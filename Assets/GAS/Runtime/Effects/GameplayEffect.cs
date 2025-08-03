using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace GAS.Runtime
{
    public enum EffectsDurationPolicy
    {
#if UNITY_ENGINE
        [LabelText("瞬时(Instant)", SdfIconType.LightningCharge)]
#endif
        Instant = 1,
#if UNITY_ENGINE
        [LabelText("永久(Infinite)", SdfIconType.Infinity)]
#endif
        Infinite,
#if UNITY_ENGINE
        [LabelText("限时(Duration)", SdfIconType.HourglassSplit)]
#endif
        Duration
    }

    /// <summary>
    /// 游戏效果核心类，定义了效果的所有属性和行为
    /// </summary>
    /// <remarks>
    /// GameplayEffect是GAS系统中用于改变游戏状态的核心机制，包括：
    /// - 属性修改（伤害、治疗、增益等）
    /// - 标签授予和移除
    /// - 技能授予
    /// - 视觉和音频效果触发
    /// - 堆叠和持续时间管理
    /// 
    /// 效果可以是瞬时的（Instant）、有限时间的（Duration）或永久的（Infinite）。
    /// </remarks>
    public class GameplayEffect
    {
        /// <summary>
        /// 游戏效果名称
        /// </summary>
        public readonly string GameplayEffectName;
        
        /// <summary>
        /// 效果持续时间策略
        /// </summary>
        public readonly EffectsDurationPolicy DurationPolicy;
        
        /// <summary>
        /// 效果持续时间（-1表示无限持续）
        /// </summary>
        public readonly float Duration; // -1 represents infinite duration
        
        /// <summary>
        /// 周期执行间隔时间
        /// </summary>
        public readonly float Period;
        
        /// <summary>
        /// 周期执行的游戏效果
        /// </summary>
        public readonly GameplayEffect PeriodExecution;
        
        /// <summary>
        /// 标签容器，管理效果相关的所有标签
        /// </summary>
        public readonly GameplayEffectTagContainer TagContainer;

        // Cues
        /// <summary>
        /// 执行时触发的瞬时Cue数组
        /// </summary>
        public readonly GameplayCueInstant[] CueOnExecute;
        
        /// <summary>
        /// 移除时触发的瞬时Cue数组
        /// </summary>
        public readonly GameplayCueInstant[] CueOnRemove;
        
        /// <summary>
        /// 添加时触发的瞬时Cue数组
        /// </summary>
        public readonly GameplayCueInstant[] CueOnAdd;
        
        /// <summary>
        /// 激活时触发的瞬时Cue数组
        /// </summary>
        public readonly GameplayCueInstant[] CueOnActivate;
        
        /// <summary>
        /// 失活时触发的瞬时Cue数组
        /// </summary>
        public readonly GameplayCueInstant[] CueOnDeactivate;
        
        /// <summary>
        /// 持续期间的Cue数组
        /// </summary>
        public readonly GameplayCueDurational[] CueDurational;

        // Modifiers
        /// <summary>
        /// 属性修饰符数组
        /// </summary>
        public readonly GameplayEffectModifier[] Modifiers;
        
        /// <summary>
        /// 执行计算数组
        /// </summary>
        public readonly GameplayEffectExecutionCalculation[] Executions;

        // Granted Ability
        /// <summary>
        /// 授予的技能数组
        /// </summary>
        public readonly GrantedAbilityFromEffect[] GrantedAbilities;

        //Stacking
        /// <summary>
        /// 堆叠策略配置
        /// </summary>
        public readonly GameplayEffectStacking Stacking;

        // Expiration Effects - 对应UE中的过期效果
        /// <summary>
        /// 提前过期效果数组（被移除、驱散等情况下触发）
        /// </summary>
        public readonly GameplayEffect[] PrematureExpirationEffects; // 提前过期（被移除、驱散等）
        
        /// <summary>
        /// 正常过期效果数组（持续时间结束时触发）
        /// </summary>
        public readonly GameplayEffect[] RoutineExpirationEffects;    // 正常过期（持续时间结束）

        /// <summary>
        /// 创建并初始化游戏效果实例
        /// </summary>
        /// <param name="creator">效果创建者（通常是技能或道具的拥有者）</param>
        /// <param name="owner">效果目标（受效果影响的对象）</param>
        /// <param name="level">效果等级，默认为1</param>
        /// <returns>完全初始化的效果实例</returns>
        /// <remarks>
        /// 这是创建效果实例的推荐方法，会同时完成实例化和初始化。
        /// creator和owner可以是同一个对象（自我效果），也可以是不同对象。
        /// </remarks>
        /// <example>
        /// // 创建一个伤害效果
        /// var damageSpec = damageEffect.CreateSpec(attacker, target, attackerLevel);
        /// target.AddGameplayEffect(attacker, damageSpec);
        /// </example>
        public GameplayEffectSpec CreateSpec(
            AbilitySystemComponent creator,
            AbilitySystemComponent owner,
            float level = 1)
        {
            var spec = new GameplayEffectSpec(this);
            spec.Init(creator, owner, level);
            return spec;
        }

        /// <summary>
        /// 分离GameplayEffectSpec的实例化过程为：实例 + 数据初始化
        /// </summary>
        /// <returns></returns>
        public GameplayEffectSpec CreateSpec()
        {
            var spec = new GameplayEffectSpec(this);
            return spec;
        }

        public GameplayEffect(IGameplayEffectData data)
        {
            if (data is null)
            {
                throw new System.Exception($"GE data can't be null!");
            }

            GameplayEffectName = data.GetDisplayName();
            DurationPolicy = data.GetDurationPolicy();
            Duration = data.GetDuration();
            Period = data.GetPeriod();
            TagContainer = new GameplayEffectTagContainer(data);
            var periodExecutionGe = data.GetPeriodExecution();
#if UNITY_EDITOR
            if (periodExecutionGe != null && periodExecutionGe.GetDurationPolicy() != EffectsDurationPolicy.Instant)
            {
                UnityEngine.Debug.LogError($"PeriodExecution of {GameplayEffectName} should be Instant type.");
            }
#endif
            PeriodExecution = periodExecutionGe != null ? new GameplayEffect(periodExecutionGe) : null;
            CueOnExecute = data.GetCueOnExecute();
            CueOnRemove = data.GetCueOnRemove();
            CueOnAdd = data.GetCueOnAdd();
            CueOnActivate = data.GetCueOnActivate();
            CueOnDeactivate = data.GetCueOnDeactivate();
            CueDurational = data.GetCueDurational();
            Modifiers = data.GetModifiers();
            Executions = data.GetExecutions();
            GrantedAbilities = GetGrantedAbilities(data.GetGrantedAbilities());
            Stacking = data.GetStacking();
            
            // Initialize expiration effects
            PrematureExpirationEffects = InitializeEffectArray(data.GetPrematureExpirationEffects());
            RoutineExpirationEffects = InitializeEffectArray(data.GetRoutineExpirationEffects());
        }

        private static GrantedAbilityFromEffect[] GetGrantedAbilities(IEnumerable<GrantedAbilityConfig> grantedAbilities)
        {
            var grantedAbilityList = new List<GrantedAbilityFromEffect>();
            foreach (var grantedAbilityConfig in grantedAbilities)
            {
                if (grantedAbilityConfig.AbilityAsset == null) continue;
                grantedAbilityList.Add(new GrantedAbilityFromEffect(grantedAbilityConfig));
            }

            return grantedAbilityList.ToArray();
        }
        
        private static GameplayEffect[] InitializeEffectArray(GameplayEffectAsset[] effectAssets)
        {
            if (effectAssets == null || effectAssets.Length == 0)
                return null;
                
            var effectList = new List<GameplayEffect>();
            foreach (var effectAsset in effectAssets)
            {
                if (effectAsset != null)
                {
                    effectList.Add(new GameplayEffect(effectAsset));
                }
            }
            return effectList.Count > 0 ? effectList.ToArray() : null;
        }

        /// <summary>
        /// 检查效果是否可以应用到指定目标
        /// </summary>
        /// <param name="target">目标组件</param>
        /// <returns>如果目标满足所有应用要求标签则返回true</returns>
        /// <remarks>
        /// 检查目标是否具备所有必需的ApplicationRequiredTags。
        /// 如果目标不满足要求，效果将无法应用。
        /// </remarks>
        /// <example>
        /// // 检查治疗效果是否可以应用到目标（可能需要目标处于“受伤”状态）
        /// if (healingEffect.CanApplyTo(target))
        /// {
        ///     // 可以应用治疗
        /// }
        /// </example>
        public bool CanApplyTo(IAbilitySystemComponent target)
        {
            return target.HasAllTags(TagContainer.ApplicationRequiredTags);
        }

        /// <summary>
        /// 检查效果是否可以在目标上继续运行
        /// </summary>
        /// <param name="target">目标组件</param>
        /// <returns>如果目标满足所有持续要求标签则返回true</returns>
        /// <remarks>
        /// 用于检查已经应用的效果是否仍然满足运行条件。
        /// 如果不满足OngoingRequiredTags，效果将被移除。
        /// 这允许创建有条件的效果，比如只在特定状态下才能维持的增益。
        /// </remarks>
        /// <example>
        /// // 检查速度增益是否可以继续（可能需要目标保持“跑步”状态）
        /// if (!speedBoostEffect.CanRunning(target))
        /// {
        ///     // 移除速度增益效果
        /// }
        /// </example>
        public bool CanRunning(IAbilitySystemComponent target)
        {
            return target.HasAllTags(TagContainer.OngoingRequiredTags);
        }

        /// <summary>
        /// 检查目标是否对此效果免疫
        /// </summary>
        /// <param name="target">目标组件</param>
        /// <returns>如果目标具有任意一个免疫标签则返回true</returns>
        /// <remarks>
        /// 检查目标是否具有ApplicationImmunityTags中的任何标签。
        /// 如果目标免疫，效果将被完全忽略，不会产生任何影响。
        /// 常用于实现魔法免疫、状态免疫等机制。
        /// </remarks>
        /// <example>
        /// // 检查目标是否对火焰伤害免疫
        /// if (fireDamageEffect.IsImmune(target))
        /// {
        ///     Debug.Log("目标对火焰伤害免疫！");
        ///     return; // 不应用效果
        /// }
        /// </example>
        public bool IsImmune(IAbilitySystemComponent target)
        {
            return target.HasAnyTags(TagContainer.ApplicationImmunityTags);
        }

        /// <summary>
        /// 检查两个效果是否属于同一堆叠组
        /// </summary>
        /// <param name="effect">要比较的效果</param>
        /// <returns>如果两个效果属于同一堆叠组则返回true</returns>
        /// <remarks>
        /// 将根据堆叠配置来判断两个效果是否可以堆叠。
        /// 只有当两个效果都启用了堆叠并且具有相同的堆叠编码时，才会返回true。
        /// 用于实现效果堆叠逻辑，比如多个同类型的增益效果可以堆叠。
        /// </remarks>
        /// <example>
        /// // 检查两个力量增益是否可以堆叠
        /// if (strengthBuff1.StackEqual(strengthBuff2))
        /// {
        ///     // 可以堆叠，增加层数或刷新持续时间
        /// }
        /// </example>
        public bool StackEqual(GameplayEffect effect)
        {
            if (Stacking.stackingType == StackingType.None) return false;
            if (effect.Stacking.stackingType == StackingType.None) return false;
            if (string.IsNullOrEmpty(Stacking.stackingCodeName)) return false;
            if (string.IsNullOrEmpty(effect.Stacking.stackingCodeName)) return false;

            return Stacking.stackingHashCode == effect.Stacking.stackingHashCode;
        }
    }
}