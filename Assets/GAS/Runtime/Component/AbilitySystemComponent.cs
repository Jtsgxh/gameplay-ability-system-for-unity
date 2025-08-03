using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS.Runtime
{
    public class AbilitySystemComponent : MonoBehaviour, IAbilitySystemComponent
    {
        [SerializeField]
        private AbilitySystemComponentPreset preset;

        public AbilitySystemComponentPreset Preset => preset;

        public int Level { get; protected set; }

        public GameplayEffectContainer GameplayEffectContainer { get; private set; }

        public GameplayTagAggregator GameplayTagAggregator { get; private set; }

        public AbilityContainer AbilityContainer { get; private set; }

        public AttributeSetContainer AttributeSetContainer { get; private set; }

        /// <summary>
        /// 标记组件是否已经准备完毕
        /// </summary>
        private bool _ready;

        private void Prepare()
        {
            if (_ready) return;
            AbilityContainer = new AbilityContainer(this);
            GameplayEffectContainer = new GameplayEffectContainer(this);
            AttributeSetContainer = new AttributeSetContainer(this);
            GameplayTagAggregator = new GameplayTagAggregator(this);
            _ready = true;
        }

        public void Enable()
        {
            AttributeSetContainer.OnEnable();
        }

        public void Disable()
        {
            AttributeSetContainer.OnDisable();
            DisableAllAbilities();
            ClearGameplayEffects();
            GameplayTagAggregator?.OnDisable();
        }

        private void Awake()
        {
            Prepare();
        }

        private void OnEnable()
        {
            Prepare();
            GameplayAbilitySystem.GAS.Register(this);
            GameplayTagAggregator?.OnEnable();
            Enable();
        }

        private void OnDisable()
        {
            Disable();
            GameplayAbilitySystem.GAS.Unregister(this);
        }

        public void SetPreset(AbilitySystemComponentPreset ascPreset)
        {
            preset = ascPreset;
        }

        /// <summary>
        /// 初始化技能系统组件，设置基础数据和能力
        /// </summary>
        /// <param name="baseTags">基础固定标签数组，这些标签将永久存在于该组件上</param>
        /// <param name="attrSetTypes">属性集类型数组，指定该组件拥有的属性集类型</param>
        /// <param name="baseAbilities">基础技能数组，这些技能将被自动授予给该组件</param>
        /// <param name="level">组件的初始等级</param>
        /// <remarks>
        /// 这是技能系统组件的主要初始化方法，通常在游戏对象创建后立即调用。
        /// 所有参数都可以为null，在这种情况下将跳过相应的初始化步骤。
        /// </remarks>
        /// <example>
        /// // 初始化一个攻击型角色
        /// var tags = new GameplayTag[] { new GameplayTag("Character.Warrior") };
        /// var attrTypes = new Type[] { typeof(HealthAttributeSet), typeof(CombatAttributeSet) };
        /// var abilities = new AbilityAsset[] { swordAttackAbility, shieldBlockAbility };
        /// 
        /// abilitySystemComponent.Init(tags, attrTypes, abilities, 1);
        /// </example>
        public void Init(GameplayTag[] baseTags, Type[] attrSetTypes, AbilityAsset[] baseAbilities, int level)
        {
            Prepare();
            SetLevel(level);
            if (baseTags != null) GameplayTagAggregator.Init(baseTags);

            if (attrSetTypes != null)
            {
                foreach (var attrSetType in attrSetTypes)
                    AttributeSetContainer.AddAttributeSet(attrSetType);
            }

            if (baseAbilities != null)
            {
                foreach (var info in baseAbilities)
                    GrantAbility(info);
            }
        }

        private void GrantAbility(AbilityAsset info)
        {
            if (info == null)
            {
                Debug.LogWarning($"[EX] Try To Grant a NULL Ability!");
                return;
            }

            try
            {
                var ability = Activator.CreateInstance(info.AbilityType(), args: info) as AbstractAbility;
                AbilityContainer.GrantAbility(ability);
            }
#pragma warning disable CS0168 // 声明了变量，但从未使用过
            catch (MissingMethodException e)
#pragma warning restore CS0168 // 声明了变量，但从未使用过
            {
                // 踩坑日志:
                //   复制了某个AbilityAsset实现类的代码，但忘记更新AbilityType()方法的返回值。
                //   一般来说AbilityAsset和Ability应该是配套的, 比如在"GAA_xxx"中返回"GA_xxx"的类型.
                Debug.LogError($"[EX] 创建能力失败: " +
                               $"请检查AbilityAsset实现类'{info.GetType().FullName}'中的AbilityType()方法" +
                               $"是否正确返回了能力类型(当前为'{info.AbilityType()?.FullName ?? "null"}')。");
                throw;
            }
        }

        public void SetLevel(int level)
        {
            Level = level;
        }

        public bool HasTag(GameplayTag gameplayTag)
        {
            return GameplayTagAggregator.HasTag(gameplayTag);
        }

        public bool HasAllTags(GameplayTagSet tags)
        {
            return GameplayTagAggregator.HasAllTags(tags);
        }

        public bool HasAnyTags(GameplayTagSet tags)
        {
            return GameplayTagAggregator.HasAnyTags(tags);
        }

        public void AddFixedTags(GameplayTagSet tags)
        {
            GameplayTagAggregator.AddFixedTag(tags);
        }

        public void RemoveFixedTags(GameplayTagSet tags)
        {
            GameplayTagAggregator.RemoveFixedTag(tags);
        }

        public void AddFixedTag(GameplayTag gameplayTag)
        {
            GameplayTagAggregator.AddFixedTag(gameplayTag);
        }

        public void RemoveFixedTag(GameplayTag gameplayTag)
        {
            GameplayTagAggregator.RemoveFixedTag(gameplayTag);
        }

        public void RemoveGameplayEffect(GameplayEffectSpec spec)
        {
            GameplayEffectContainer.RemoveGameplayEffectSpec(spec);
        }

        public void RemoveGameplayEffectWithAnyTags(GameplayTagSet tags)
        {
            GameplayEffectContainer.RemoveGameplayEffectWithAnyTags(tags);
        }

        public GameplayEffectSpec ApplyGameplayEffectTo(GameplayEffectSpec gameplayEffectSpec,
            AbilitySystemComponent target)
        {
            return target.AddGameplayEffect(this, gameplayEffectSpec);
        }

        /// <summary>
        /// 对目标应用游戏效果
        /// </summary>
        /// <param name="gameplayEffect">要应用的游戏效果</param>
        /// <param name="target">效果作用的目标组件</param>
        /// <returns>创建的效果实例，如果应用失败则返回null</returns>
        /// <remarks>
        /// 这是应用效果的主要方法。效果将从当前组件发出，作用于目标组件。
        /// 效果的等级将默认为1，如需指定等级请使用重载方法。
        /// </remarks>
        /// <example>
        /// // 对敌人应用伤害效果
        /// var damageEffect = damageGameplayEffect;
        /// var effectSpec = attacker.ApplyGameplayEffectTo(damageEffect, enemy);
        /// 
        /// if (effectSpec != null)
        /// {
        ///     Debug.Log($"伤害效果已应用: {effectSpec.GameplayEffect.GameplayEffectName}");
        /// }
        /// </example>
        public GameplayEffectSpec ApplyGameplayEffectTo(GameplayEffect gameplayEffect, AbilitySystemComponent target)
        {
            if (gameplayEffect == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"[EX] Try To Apply a NULL GameplayEffect From {name} To {target.name}!");
#endif
                return null;
            }

            var spec = gameplayEffect.CreateSpec();
            return ApplyGameplayEffectTo(spec, target);
        }

        public GameplayEffectSpec ApplyGameplayEffectTo(GameplayEffect gameplayEffect, AbilitySystemComponent target,
            int effectLevel)
        {
            if (gameplayEffect == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"[EX] Try To Apply a NULL GameplayEffect From {name} To {target.name}!");
#endif
                return null;
            }

            var spec = gameplayEffect.CreateSpec();
            spec.SetLevel(effectLevel);
            return ApplyGameplayEffectTo(spec, target);
        }

        public GameplayEffectSpec ApplyGameplayEffectToSelf(GameplayEffectSpec gameplayEffectSpec)
        {
            return ApplyGameplayEffectTo(gameplayEffectSpec, this);
        }

        /// <summary>
        /// 对自身应用游戏效果
        /// </summary>
        /// <param name="gameplayEffect">要应用的游戏效果</param>
        /// <returns>创建的效果实例，如果应用失败则返回null</returns>
        /// <remarks>
        /// 这是ApplyGameplayEffectTo的便捷方法，目标为自身。
        /// 常用于自我增益、恢复或状态改变效果。
        /// </remarks>
        /// <example>
        /// // 使用恢复药水
        /// var healingPotion = healingGameplayEffect;
        /// var effectSpec = player.ApplyGameplayEffectToSelf(healingPotion);
        /// </example>
        public GameplayEffectSpec ApplyGameplayEffectToSelf(GameplayEffect gameplayEffect)
        {
            return ApplyGameplayEffectTo(gameplayEffect, this);
        }

        public void RemoveGameplayEffectSpec(GameplayEffectSpec gameplayEffectSpec)
        {
            GameplayEffectContainer.RemoveGameplayEffectSpec(gameplayEffectSpec);
        }

        /// <summary>
        /// 授予组件一个新技能
        /// </summary>
        /// <param name="ability">要授予的技能实例</param>
        /// <returns>创建的技能规格实例</returns>
        /// <remarks>
        /// 授予技能后，该技能将可以被激活和使用。
        /// 如果同名技能已存在，则不会重复授予。
        /// </remarks>
        /// <example>
        /// // 授予攻击技能
        /// var attackAbility = new SwordAttackAbility(attackAbilityAsset);
        /// var abilitySpec = component.GrantAbility(attackAbility);
        /// 
        /// // 现在可以激活这个技能
        /// component.TryActivateAbility("SwordAttack");
        /// </example>
        public AbilitySpec GrantAbility(AbstractAbility ability)
        {
            AbilityContainer.GrantAbility(ability);
            return AbilityContainer.AbilitySpecs()[ability.Name];
        }

        /// <summary>
        /// 从组件中移除指定名称的技能
        /// </summary>
        /// <param name="abilityName">要移除的技能名称</param>
        /// <remarks>
        /// 移除技能会立即结束该技能的所有实例，并从技能列表中删除。
        /// 如果技能不存在，此操作不会产生任何效果。
        /// </remarks>
        /// <example>
        /// // 移除攻击技能
        /// component.RemoveAbility("SwordAttack");
        /// 
        /// // 现在无法再激活这个技能
        /// bool success = component.TryActivateAbility("SwordAttack"); // 返回false
        /// </example>
        public void RemoveAbility(string abilityName)
        {
            AbilityContainer.RemoveAbility(abilityName);
        }

        public AttributeValue? GetAttributeAttributeValue(string attrSetName, string attrShortName)
        {
            var value = AttributeSetContainer.GetAttributeAttributeValue(attrSetName, attrShortName);
            return value;
        }

        public CalculateMode? GetAttributeCalculateMode(string attrSetName, string attrShortName)
        {
            var value = AttributeSetContainer.GetAttributeCalculateMode(attrSetName, attrShortName);
            return value;
        }

        /// <summary>
        /// 获取指定属性的当前值（包含所有修饰符效果）
        /// </summary>
        /// <param name="setName">属性集名称</param>
        /// <param name="attributeShortName">属性短名称</param>
        /// <returns>属性的当前值，如果属性不存在则返回null</returns>
        /// <remarks>
        /// 当前值是经过所有GameplayEffect修饰符计算后的最终值。
        /// 这是游戏中实际使用的数值，包括临时增益、减益等效果。
        /// </remarks>
        /// <example>
        /// // 获取角色当前生命值
        /// float? currentHealth = component.GetAttributeCurrentValue("Health", "CurrentHealth");
        /// if (currentHealth.HasValue)
        /// {
        ///     Debug.Log($"当前生命值: {currentHealth.Value}");
        /// }
        /// </example>
        public float? GetAttributeCurrentValue(string setName, string attributeShortName)
        {
            var value = AttributeSetContainer.GetAttributeCurrentValue(setName, attributeShortName);
            return value;
        }

        /// <summary>
        /// 获取指定属性的基础值（不包含任何修饰符效果）
        /// </summary>
        /// <param name="setName">属性集名称</param>
        /// <param name="attributeShortName">属性短名称</param>
        /// <returns>属性的基础值，如果属性不存在则返回null</returns>
        /// <remarks>
        /// 基础值是属性的原始值，不受任何临时效果影响。
        /// 通常用于显示角色的基础属性或计算百分比修饰符。
        /// </remarks>
        /// <example>
        /// // 获取角色基础攻击力
        /// float? baseAttack = component.GetAttributeBaseValue("Combat", "Attack");
        /// if (baseAttack.HasValue)
        /// {
        ///     Debug.Log($"基础攻击力: {baseAttack.Value}");
        /// }
        /// </example>
        public float? GetAttributeBaseValue(string setName, string attributeShortName)
        {
            var value = AttributeSetContainer.GetAttributeBaseValue(setName, attributeShortName);
            return value;
        }

        public void Tick()
        {
            AbilityContainer.Tick();
            GameplayEffectContainer.Tick();
        }

        /// <summary>
        /// 获取所有属性的当前值快照
        /// </summary>
        /// <returns>包含所有属性当前值的字典，键为"属性集名.属性名"格式</returns>
        /// <remarks>
        /// 返回的字典包含了组件上所有属性集中所有属性的当前值。
        /// 常用于调试、数据保存或UI显示。
        /// </remarks>
        /// <example>
        /// // 获取所有属性快照
        /// var snapshot = component.DataSnapshot();
        /// foreach (var kvp in snapshot)
        /// {
        ///     Debug.Log($"{kvp.Key}: {kvp.Value}");
        /// }
        /// // 输出示例: "Health.CurrentHealth: 85.5"
        /// </example>
        public Dictionary<string, float> DataSnapshot()
        {
            return AttributeSetContainer.Snapshot();
        }

        /// <summary>
        /// 尝试激活指定名称的技能
        /// </summary>
        /// <param name="abilityName">要激活的技能名称</param>
        /// <param name="args">传递给技能的参数</param>
        /// <returns>如果激活成功返回true，否则返回false</returns>
        /// <remarks>
        /// 激活可能失败的原因包括：
        /// - 技能不存在
        /// - 技能正在冷却中
        /// - 不满足激活条件（标签、资源等）
        /// - 技能已经在激活状态且不允许多重激活
        /// </remarks>
        /// <example>
        /// // 攻击指定目标
        /// bool success = attacker.TryActivateAbility("SwordAttack", enemy);
        /// if (success)
        /// {
        ///     Debug.Log("攻击技能激活成功");
        /// }
        /// else
        /// {
        ///     Debug.Log("攻击技能激活失败");
        /// }
        /// </example>
        public bool TryActivateAbility(string abilityName, params object[] args)
        {
            return AbilityContainer.TryActivateAbility(abilityName, args);
        }

        /// <summary>
        /// 尝试正常结束指定名称的技能
        /// </summary>
        /// <param name="abilityName">要结束的技能名称</param>
        /// <remarks>
        /// 正常结束技能会触发技能的结束逻辑，并清理相关状态。
        /// 如果技能不存在或没有激活，此操作不会产生效果。
        /// </remarks>
        /// <example>
        /// // 手动结束技能
        /// component.TryEndAbility("ChannelingSpell");
        /// </example>
        public void TryEndAbility(string abilityName)
        {
            AbilityContainer.EndAbility(abilityName);
        }

        /// <summary>
        /// 尝试取消指定名称的技能
        /// </summary>
        /// <param name="abilityName">要取消的技能名称</param>
        /// <remarks>
        /// 取消技能与结束技能不同，取消是强制中断，可能不会触发正常的结束逻辑。
        /// 常用于打断、眩眤等情况下强制停止技能。
        /// </remarks>
        /// <example>
        /// // 当角色被眩眤时取消正在释放的技能
        /// if (IsStunned)
        /// {
        ///     component.TryCancelAbility("ChannelingSpell");
        /// }
        /// </example>
        public void TryCancelAbility(string abilityName)
        {
            AbilityContainer.CancelAbility(abilityName);
        }

        public void ApplyModFromInstantGameplayEffect(GameplayEffectSpec spec)
        {
            foreach (var modifier in spec.Modifiers)
            {
                var attributeValue = GetAttributeAttributeValue(modifier.AttributeSetName, modifier.AttributeShortName);
                if (attributeValue == null) continue;
                if (attributeValue.Value.IsSupportOperation(modifier.Operation) == false)
                {
                    throw new InvalidOperationException("Unsupported operation.");
                }

                if (attributeValue.Value.CalculateMode != CalculateMode.Stacking)
                {
                    throw new InvalidOperationException(
                        $"[EX] Instant GameplayEffect Can Only Modify Stacking Mode Attribute! " +
                        $"But {modifier.AttributeSetName}.{modifier.AttributeShortName} is {attributeValue.Value.CalculateMode}");
                }

                var magnitude = modifier.CalculateMagnitude(spec, modifier.ModiferMagnitude);
                var baseValue = attributeValue.Value.BaseValue;
                switch (modifier.Operation)
                {
                    case GEOperation.Add:
                        baseValue += magnitude;
                        break;
                    case GEOperation.Minus:
                        baseValue -= magnitude;
                        break;
                    case GEOperation.Multiply:
                        baseValue *= magnitude;
                        break;
                    case GEOperation.Divide:
                        baseValue /= magnitude;
                        break;
                    case GEOperation.Override:
                        baseValue = magnitude;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                AttributeSetContainer.Sets[modifier.AttributeSetName]
                    .ChangeAttributeBase(modifier.AttributeShortName, baseValue);
            }
        }

        public CooldownTimer CheckCooldownFromTags(GameplayTagSet tags)
        {
            return GameplayEffectContainer.CheckCooldownFromTags(tags);
        }

        public T AttrSet<T>() where T : AttributeSet
        {
            AttributeSetContainer.TryGetAttributeSet<T>(out var attrSet);
            return attrSet;
        }

        public void ClearGameplayEffect()
        {
            // _abilityContainer = new AbilityContainer(this);
            // GameplayEffectContainer = new GameplayEffectContainer(this);
            // _attributeSetContainer = new AttributeSetContainer(this);
            // tagAggregator = new GameplayTagAggregator(this);
            GameplayEffectContainer.ClearGameplayEffect();
        }

        private GameplayEffectSpec AddGameplayEffect(AbilitySystemComponent source, GameplayEffectSpec effectSpec)
        {
            return GameplayEffectContainer.AddGameplayEffectSpec(source, effectSpec);
        }

        private GameplayEffectSpec AddGameplayEffect(AbilitySystemComponent source, GameplayEffectSpec effectSpec,
            int effectLevel)
        {
            return GameplayEffectContainer.AddGameplayEffectSpec(source, effectSpec, true, effectLevel);
        }

        private void DisableAllAbilities()
        {
            AbilityContainer.CancelAllAbilities();
        }

        private void ClearGameplayEffects()
        {
            GameplayEffectContainer.ClearGameplayEffect();
        }
        
        #region EVENT PUBLISHING
        
        /// <summary>
        /// 发布游戏事件到事件总线
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="target">事件目标（可选）</param>
        /// <param name="eventTags">事件标签（可选）</param>
        /// <param name="parameters">事件参数（可选）</param>
        /// <remarks>
        /// 通过事件总线发布事件，所有订阅此事件的监听器都会收到通知。
        /// 这是触发EventExecutionCondition的主要方式。
        /// </remarks>
        /// <example>
        /// // 发布伤害事件
        /// var damageParams = new Dictionary&lt;string, object&gt;
        /// {
        ///     { "damage", 100f },
        ///     { "damageType", "Physical" }
        /// };
        /// attacker.PublishGameplayEvent(GameplayEvents.OnDamageDealt, target, null, damageParams);
        /// </example>
        public void PublishGameplayEvent(
            string eventName, 
            AbilitySystemComponent target = null,
            GameplayTag[] eventTags = null, 
            Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("Cannot publish event with null or empty name");
                return;
            }
            
            GameplayEventBus.Instance.Publish(eventName, this, target, eventTags, parameters);
        }
        
        /// <summary>
        /// 发布伤害相关事件
        /// </summary>
        /// <param name="damageAmount">伤害数值</param>
        /// <param name="target">伤害目标</param>
        /// <param name="damageType">伤害类型标签（可选）</param>
        /// <param name="isDealt">true表示造成伤害，false表示受到伤害</param>
        public void PublishDamageEvent(float damageAmount, AbilitySystemComponent target, GameplayTag? damageType = null, bool isDealt = true)
        {
            var eventName = isDealt ? GameplayEvents.OnDamageDealt : GameplayEvents.OnDamageReceived;
            var parameters = new Dictionary<string, object>
            {
                { "damage", damageAmount },
                { "target", target }
            };
            
            if (damageType.HasValue)
            {
                parameters["damageType"] = damageType.Value;
            }
            
            var eventTags = damageType.HasValue ? new[] { damageType.Value } : null;
            PublishGameplayEvent(eventName, target, eventTags, parameters);
        }
        
        /// <summary>
        /// 发布治疗相关事件
        /// </summary>
        /// <param name="healAmount">治疗数值</param>
        /// <param name="target">治疗目标</param>
        /// <param name="healType">治疗类型标签（可选）</param>
        /// <param name="isDealt">true表示提供治疗，false表示受到治疗</param>
        public void PublishHealingEvent(float healAmount, AbilitySystemComponent target, GameplayTag? healType = null, bool isDealt = true)
        {
            var eventName = isDealt ? GameplayEvents.OnHealingDealt : GameplayEvents.OnHealingReceived;
            var parameters = new Dictionary<string, object>
            {
                { "healing", healAmount },
                { "target", target }
            };
            
            if (healType.HasValue)
            {
                parameters["healType"] = healType.Value;
            }
            
            var eventTags = healType.HasValue ? new[] { healType.Value } : null;
            PublishGameplayEvent(eventName, target, eventTags, parameters);
        }
        
        /// <summary>
        /// 发布技能相关事件
        /// </summary>
        /// <param name="abilityName">技能名称</param>
        /// <param name="eventType">事件类型（激活、结束等）</param>
        /// <param name="target">技能目标（可选）</param>
        public void PublishAbilityEvent(string abilityName, string eventType, AbilitySystemComponent target = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "abilityName", abilityName }
            };
            
            if (target != null)
            {
                parameters["target"] = target;
            }
            
            PublishGameplayEvent(eventType, target, null, parameters);
        }
        
        /// <summary>
        /// 发布标签变化事件
        /// </summary>
        /// <param name="tag">变化的标签</param>
        /// <param name="isAdded">true表示添加，false表示移除</param>
        /// <param name="stackCount">堆叠数量（可选）</param>
        public void PublishTagEvent(GameplayTag tag, bool isAdded, int stackCount = 1)
        {
            var eventName = isAdded ? GameplayEvents.OnTagAdded : GameplayEvents.OnTagRemoved;
            var parameters = new Dictionary<string, object>
            {
                { "tag", tag },
                { "stackCount", stackCount }
            };
            
            var eventTags = new[] { tag };
            PublishGameplayEvent(eventName, null, eventTags, parameters);
        }
        
        /// <summary>
        /// 发布游戏效果相关事件
        /// </summary>
        /// <param name="effectSpec">游戏效果实例</param>
        /// <param name="eventType">事件类型（应用、移除等）</param>
        public void PublishGameplayEffectEvent(GameplayEffectSpec effectSpec, string eventType)
        {
            if (effectSpec == null) return;
            
            var parameters = new Dictionary<string, object>
            {
                { "effectSpec", effectSpec },
                { "effectName", effectSpec.GameplayEffect.GameplayEffectName }
            };
            
            PublishGameplayEvent(eventType, null, null, parameters);
        }
        
        /// <summary>
        /// 发布属性变化事件
        /// </summary>
        /// <param name="attributeName">属性名称</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        /// <param name="eventType">事件类型（PreChange, PostChange等）</param>
        public void PublishAttributeEvent(string attributeName, float oldValue, float newValue, string eventType = null)
        {
            if (string.IsNullOrEmpty(attributeName)) return;
            
            var finalEventType = eventType ?? GameplayEvents.OnAttributeChanged;
            var parameters = new Dictionary<string, object>
            {
                { "attributeName", attributeName },
                { "oldValue", oldValue },
                { "newValue", newValue },
                { "delta", newValue - oldValue }
            };
            
            PublishGameplayEvent(finalEventType, null, null, parameters);
        }
        
        #endregion
    }
}