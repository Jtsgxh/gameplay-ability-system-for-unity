using UnityEngine;

namespace GAS.Runtime
{
    /// <summary>
    /// 伤害执行计算 - 展示UE式的AttributeCapture和自定义执行能力
    /// </summary>
    /// <remarks>
    /// 这个示例展示了GameplayEffectExecutionCalculation的真正威力：
    /// - 使用UE式的AttributeCapture机制（Snapshot vs No-Snapshot）
    /// - 直接修改属性，无需通过预定义操作
    /// - 根据条件执行不同的逻辑
    /// - 触发其他GameplayEffect
    /// - 实现复杂的伤害计算公式
    /// - 处理特殊情况（如生命值归零）
    /// </remarks>
    public class DamageExecutionCalculation : GameplayEffectExecutionCalculation
    {
        // 属性集和属性名称常量
        private const string CHARACTER_ATTR_SET = "AS_Character";
        private const string HEALTH = "Health";
        private const string MAX_HEALTH = "MaxHealth";
        private const string ATTACK_POWER = "AttackPower";
        private const string DEFENSE = "Defense";
        private const string CRITICAL_CHANCE = "CriticalChance";
        private const string CRITICAL_DAMAGE = "CriticalDamage";
        private const string DAMAGE_RESISTANCE = "DamageResistance";
        
        /// <summary>
        /// 定义需要捕获的属性 - 这是UE式的AttributeCapture机制
        /// </summary>
        public override GameplayEffectAttributeCaptureDefinition[] GetAttributeCaptureDefinitions()
        {
            return new[]
            {
                // 源属性 - 使用Snapshot，火球发射后不受后续伤害加成影响
                new GameplayEffectAttributeCaptureDefinition(CHARACTER_ATTR_SET, ATTACK_POWER, AttributeCaptureSource.Source, true),
                new GameplayEffectAttributeCaptureDefinition(CHARACTER_ATTR_SET, CRITICAL_CHANCE, AttributeCaptureSource.Source, true),
                new GameplayEffectAttributeCaptureDefinition(CHARACTER_ATTR_SET, CRITICAL_DAMAGE, AttributeCaptureSource.Source, true),
                
                // 目标属性 - 使用No-Snapshot，实时获取目标当前状态
                new GameplayEffectAttributeCaptureDefinition(CHARACTER_ATTR_SET, DEFENSE, AttributeCaptureSource.Target, false),
                new GameplayEffectAttributeCaptureDefinition(CHARACTER_ATTR_SET, DAMAGE_RESISTANCE, AttributeCaptureSource.Target, false),
                new GameplayEffectAttributeCaptureDefinition(CHARACTER_ATTR_SET, HEALTH, AttributeCaptureSource.Target, false),
                new GameplayEffectAttributeCaptureDefinition(CHARACTER_ATTR_SET, MAX_HEALTH, AttributeCaptureSource.Target, false)
            };
        }
        
        public override void Execute(GameplayEffectCustomExecutionParameters executionParams)
        {
            // 1. 获取源（攻击者）属性 - 这些是Snapshot值，不会受后续变化影响
            float attackPower = executionParams.GetSourceAttribute(CHARACTER_ATTR_SET, ATTACK_POWER);
            float critChance = executionParams.GetSourceAttribute(CHARACTER_ATTR_SET, CRITICAL_CHANCE);
            float critDamage = executionParams.GetSourceAttribute(CHARACTER_ATTR_SET, CRITICAL_DAMAGE);
            
            // 2. 获取目标属性 - 这些是No-Snapshot值，获取执行时的实时值
            float defense = executionParams.GetTargetAttribute(CHARACTER_ATTR_SET, DEFENSE);
            float damageResistance = executionParams.GetTargetAttribute(CHARACTER_ATTR_SET, DAMAGE_RESISTANCE);
            float currentHealth = executionParams.GetTargetAttribute(CHARACTER_ATTR_SET, HEALTH);
            float maxHealth = executionParams.GetTargetAttribute(CHARACTER_ATTR_SET, MAX_HEALTH);
            
            // 3. 获取效果等级和SetByCaller值
            float damageMultiplier = executionParams.Level;
            
            // 尝试从SetByCaller获取额外伤害
            float bonusDamage = 0f;
            var bonusDamageTag = new GameplayTag("Damage.Bonus");
            if (executionParams.EffectSpec.HasSetByCallerMagnitude(bonusDamageTag))
            {
                bonusDamage = executionParams.GetSetByCallerMagnitude(bonusDamageTag);
            }
            
            // 4. 计算基础伤害
            float baseDamage = (attackPower * damageMultiplier) + bonusDamage;
            
            // 5. 计算防御减伤
            float defenseMitigation = defense / (defense + 100f);
            float damageAfterDefense = baseDamage * (1f - defenseMitigation);
            
            // 6. 计算抗性减伤
            float damageAfterResistance = damageAfterDefense * (1f - Mathf.Clamp01(damageResistance));
            
            // 7. 暴击判定
            bool isCritical = Random.value < critChance;
            float finalDamage = damageAfterResistance;
            
            if (isCritical)
            {
                finalDamage *= critDamage;
                
                // 触发暴击相关的GameplayEffect（比如额外的出血效果）
                // 这展示了ExecutionCalculation可以触发其他效果
                TriggerCriticalHitEffects(executionParams);
            }
            
            // 8. 确保最小伤害为1
            finalDamage = Mathf.Max(1f, finalDamage);
            
            // 9. 计算新的生命值
            float newHealth = currentHealth - finalDamage;
            
            // 10. 处理生命值归零的特殊情况
            if (newHealth <= 0)
            {
                newHealth = 0;
                // 触发死亡相关的GameplayEffect
                TriggerDeathEffects(executionParams);
            }
            
            // 11. 直接修改目标的生命值属性
            var targetAttributeSet = executionParams.Target.AttributeSetContainer.Sets[CHARACTER_ATTR_SET];
            if (targetAttributeSet != null)
            {
                targetAttributeSet.ChangeAttributeBase(HEALTH, newHealth);
            }
            
            // 12. 记录伤害信息并触发相关系统
            LogDamageInfo(finalDamage, isCritical, newHealth <= 0);
            
            // 13. 触发伤害相关的GameplayCues
            TriggerDamageGameplayCues(executionParams, finalDamage, isCritical);
        }
        
        /// <summary>
        /// 触发暴击相关的效果
        /// </summary>
        private void TriggerCriticalHitEffects(GameplayEffectCustomExecutionParameters executionParams)
        {
            // 这里可以触发出血效果、暴击音效等
            // 例如：executionParams.Target.ApplyGameplayEffectToSelf(bleedingEffect);
            Debug.Log("Critical Hit! Triggering additional effects...");
        }
        
        /// <summary>
        /// 触发死亡相关的效果
        /// </summary>
        private void TriggerDeathEffects(GameplayEffectCustomExecutionParameters executionParams)
        {
            // 这里可以触发死亡效果、掉落物品、经验奖励等
            // 例如：executionParams.Source.ApplyGameplayEffectToSelf(expGainEffect);
            Debug.Log("Target died! Triggering death effects...");
        }
        
        /// <summary>
        /// 记录伤害信息
        /// </summary>
        private void LogDamageInfo(float damage, bool isCritical, bool isDead)
        {
            string message = $"Damage: {damage:F1}";
            if (isCritical) message += " (Critical!)";
            if (isDead) message += " (Target Killed!)";
            Debug.Log(message);
        }
        
        /// <summary>
        /// 触发伤害相关的GameplayCues
        /// </summary>
        private void TriggerDamageGameplayCues(GameplayEffectCustomExecutionParameters executionParams, 
            float damage, bool isCritical)
        {
            // 这里可以触发伤害数字显示、屏幕震动等视觉效果
            // 例如：executionParams.Target.TriggerGameplayCue(damageNumberCue, damage);
        }
    }
    
    /// <summary>
    /// 治疗执行计算 - 另一个展示自定义能力的示例
    /// </summary>
    public class HealingExecutionCalculation : GameplayEffectExecutionCalculation
    {
        private const string CHARACTER_ATTR_SET = "AS_Character";
        private const string HEALTH = "Health";
        private const string MAX_HEALTH = "MaxHealth";
        private const string HEALING_POWER = "HealingPower";
        private const string HEALING_RECEIVED = "HealingReceived";
        
        public override void Execute(GameplayEffectCustomExecutionParameters executionParams)
        {
            // 获取治疗相关属性
            float healingPower = executionParams.GetSourceAttribute(CHARACTER_ATTR_SET, HEALING_POWER);
            float healingReceived = executionParams.GetTargetAttribute(CHARACTER_ATTR_SET, HEALING_RECEIVED);
            float currentHealth = executionParams.GetTargetAttribute(CHARACTER_ATTR_SET, HEALTH);
            float maxHealth = executionParams.GetTargetAttribute(CHARACTER_ATTR_SET, MAX_HEALTH);
            
            // 计算治疗量
            float baseHealing = healingPower * executionParams.Level;
            float finalHealing = baseHealing * (1f + healingReceived);
            
            // 计算新的生命值，防止过量治疗
            float newHealth = Mathf.Min(currentHealth + finalHealing, maxHealth);
            float actualHealing = newHealth - currentHealth;
            
            // 只有在实际治疗量大于0时才应用
            if (actualHealing > 0)
            {
                // 直接修改生命值
                var targetAttributeSet = executionParams.Target.AttributeSetContainer.Sets[CHARACTER_ATTR_SET];
                if (targetAttributeSet != null)
                {
                    targetAttributeSet.ChangeAttributeBase(HEALTH, newHealth);
                }
                
                // 如果治疗量达到某个阈值，触发额外的增益效果
                if (actualHealing >= maxHealth * 0.5f) // 如果治疗了50%以上的最大生命值
                {
                    TriggerMassiveHealingBonus(executionParams);
                }
                
                Debug.Log($"Healed for {actualHealing:F1} HP (New Health: {newHealth:F1}/{maxHealth:F1})");
            }
        }
        
        /// <summary>
        /// 触发大量治疗的奖励效果
        /// </summary>
        private void TriggerMassiveHealingBonus(GameplayEffectCustomExecutionParameters executionParams)
        {
            // 可以给目标一个短暂的伤害减免效果
            Debug.Log("Massive healing! Granting temporary damage reduction...");
        }
    }
}