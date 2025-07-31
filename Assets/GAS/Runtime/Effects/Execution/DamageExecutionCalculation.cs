using UnityEngine;

namespace GAS.Runtime
{
    /// <summary>
    /// 伤害执行计算示例
    /// </summary>
    /// <remarks>
    /// 这是一个复杂伤害计算的示例，展示了如何使用 GameplayEffectExecutionCalculation：
    /// - 获取攻击者的攻击力、暴击率、暴击伤害
    /// - 获取目标的防御力、抗性
    /// - 计算减伤、暴击、最终伤害
    /// - 应用伤害到目标的生命值
    /// </remarks>
    public class DamageExecutionCalculation : GameplayEffectExecutionCalculation
    {
        // 属性集和属性名称常量
        private const string CHARACTER_ATTR_SET = "AS_Character";
        private const string HEALTH = "Health";
        private const string ATTACK_POWER = "AttackPower";
        private const string DEFENSE = "Defense";
        private const string CRITICAL_CHANCE = "CriticalChance";
        private const string CRITICAL_DAMAGE = "CriticalDamage";
        private const string DAMAGE_RESISTANCE = "DamageResistance";
        
        public override void Execute(GameplayEffectCustomExecutionParameters context, GameplayEffectCustomExecutionOutput output)
        {
            // 1. 获取源（攻击者）属性
            float attackPower = context.GetSourceAttribute(CHARACTER_ATTR_SET, ATTACK_POWER);
            float critChance = context.GetSourceAttribute(CHARACTER_ATTR_SET, CRITICAL_CHANCE);
            float critDamage = context.GetSourceAttribute(CHARACTER_ATTR_SET, CRITICAL_DAMAGE);
            
            // 2. 获取目标属性
            float defense = context.GetTargetAttribute(CHARACTER_ATTR_SET, DEFENSE);
            float damageResistance = context.GetTargetAttribute(CHARACTER_ATTR_SET, DAMAGE_RESISTANCE);
            
            // 3. 获取效果等级和SetByCaller值
            float damageMultiplier = context.Level; // 使用等级作为伤害倍率
            
            // 尝试从SetByCaller获取额外伤害
            float bonusDamage = 0f;
            var bonusDamageTag = new GameplayTag("Damage.Bonus");
            if (context.EffectSpec.HasSetByCallerMagnitude(bonusDamageTag))
            {
                bonusDamage = context.GetSetByCallerMagnitude(bonusDamageTag);
            }
            
            // 4. 计算基础伤害
            float baseDamage = (attackPower * damageMultiplier) + bonusDamage;
            
            // 5. 计算防御减伤
            // 使用经典的防御公式：减伤百分比 = 防御 / (防御 + 100)
            float defenseMitigation = defense / (defense + 100f);
            float damageAfterDefense = baseDamage * (1f - defenseMitigation);
            
            // 6. 计算抗性减伤
            // 抗性直接作为百分比减伤（0-1）
            float damageAfterResistance = damageAfterDefense * (1f - Mathf.Clamp01(damageResistance));
            
            // 7. 暴击判定
            bool isCritical = Random.value < critChance;
            float finalDamage = damageAfterResistance;
            
            if (isCritical)
            {
                // 暴击伤害倍率通常是 2.0 = 200%
                finalDamage *= critDamage;
                
                // 可以在这里触发暴击相关的GameplayCue
                // context.TriggerCue("GameplayCue.Combat.CriticalHit");
            }
            
            // 8. 确保最小伤害为1
            finalDamage = Mathf.Max(1f, finalDamage);
            
            // 9. 应用伤害（减少目标生命值）
            output.AddModification(CHARACTER_ATTR_SET, HEALTH, GEOperation.Minus, finalDamage);
            
            // 10. 记录伤害信息（可用于UI显示）
            if (isCritical)
            {
                Debug.Log($"Critical Hit! Damage: {finalDamage:F1}");
            }
            else
            {
                Debug.Log($"Normal Hit. Damage: {finalDamage:F1}");
            }
        }
    }
    
    /// <summary>
    /// 治疗执行计算示例
    /// </summary>
    public class HealingExecutionCalculation : GameplayEffectExecutionCalculation
    {
        private const string CHARACTER_ATTR_SET = "AS_Character";
        private const string HEALTH = "Health";
        private const string MAX_HEALTH = "MaxHealth";
        private const string HEALING_POWER = "HealingPower";
        private const string HEALING_RECEIVED = "HealingReceived";
        
        public override void Execute(GameplayEffectCustomExecutionParameters context, GameplayEffectCustomExecutionOutput output)
        {
            // 获取源的治疗强度
            float healingPower = context.GetSourceAttribute(CHARACTER_ATTR_SET, HEALING_POWER);
            
            // 获取目标的治疗接收加成
            float healingReceived = context.GetTargetAttribute(CHARACTER_ATTR_SET, HEALING_RECEIVED);
            float currentHealth = context.GetTargetAttribute(CHARACTER_ATTR_SET, HEALTH);
            float maxHealth = context.GetTargetAttribute(CHARACTER_ATTR_SET, MAX_HEALTH);
            
            // 计算治疗量
            float baseHealing = healingPower * context.Level;
            float finalHealing = baseHealing * (1f + healingReceived);
            
            // 防止过量治疗
            float actualHealing = Mathf.Min(finalHealing, maxHealth - currentHealth);
            
            // 应用治疗
            if (actualHealing > 0)
            {
                output.AddModification(CHARACTER_ATTR_SET, HEALTH, GEOperation.Add, actualHealing);
                Debug.Log($"Healed for {actualHealing:F1} HP");
            }
        }
    }
}