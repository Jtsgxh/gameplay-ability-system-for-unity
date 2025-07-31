using System;

namespace GAS.Runtime
{
    /// <summary>
    /// 瞬时游戏效果数据实现，用于创建立即生效的游戏效果
    /// </summary>
    /// <remarks>
    /// InstantGameplayEffectData提供了瞬时效果的基础实现：
    /// 
    /// 瞬时效果特点：
    /// - 立即应用修饰符到目标属性
    /// - 不需要持续时间管理
    /// - 执行后立即清理，不占用持续效果槽位
    /// - 适用于伤害、治疗、瞬时属性修改等场景
    /// 
    /// 支持的功能：
    /// - 应用条件标签检查
    /// - 免疫标签检查
    /// - 移除其他效果的能力
    /// - 执行时的Cue反馈
    /// - 属性修饰符应用
    /// 
    /// 典型使用场景：
    /// - 伤害计算和应用
    /// - 治疗效果
    /// - 瞬时buff/debuff
    /// - 资源消耗和恢复
    /// </remarks>
    public class InstantGameplayEffectData : IGameplayEffectData
    {
        /// <summary>
        /// 效果的显示名称
        /// </summary>
        private string Name { get; }

        /// <summary>
        /// 应用此效果所需的标签条件
        /// 目标必须拥有所有这些标签才能应用效果
        /// </summary>
        public GameplayTag[] ApplicationRequiredTags { get; set; } = Array.Empty<GameplayTag>();
        
        /// <summary>
        /// 效果免疫标签
        /// 拥有任意这些标签的目标将免疫此效果
        /// </summary>
        public GameplayTag[] ApplicationImmunityTags { get; set; } = Array.Empty<GameplayTag>();
        
        /// <summary>
        /// 移除效果标签
        /// 应用此效果时，会移除目标身上拥有这些标签的其他效果
        /// </summary>
        public GameplayTag[] RemoveGameplayEffectsWithTags { get; set; } = Array.Empty<GameplayTag>();
        
        /// <summary>
        /// 执行时的瞬时Cue反馈
        /// 在效果应用时触发的视听反馈
        /// </summary>
        public GameplayCueInstant[] CueOnExecute { get; set; } = Array.Empty<GameplayCueInstant>();
        
        /// <summary>
        /// 属性修饰符数组
        /// 定义此效果对目标属性的具体修改
        /// </summary>
        public GameplayEffectModifier[] Modifiers { get; set; } = Array.Empty<GameplayEffectModifier>();

        /// <summary>
        /// 创建瞬时游戏效果数据实例
        /// </summary>
        /// <param name="name">效果的显示名称</param>
        public InstantGameplayEffectData(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 获取效果的显示名称
        /// </summary>
        /// <returns>效果名称</returns>
        public string GetDisplayName()
        {
            return Name;
        }

        /// <summary>
        /// 获取效果的持续时间策略（瞬时效果）
        /// </summary>
        /// <returns>始终返回瞬时策略</returns>
        public virtual EffectsDurationPolicy GetDurationPolicy()
        {
            return EffectsDurationPolicy.Instant;
        }

        /// <summary>
        /// 获取效果持续时间（瞬时效果不适用）
        /// </summary>
        /// <returns>始终返回-1表示不适用</returns>
        public virtual float GetDuration()
        {
            return -1;
        }

        /// <summary>
        /// 获取效果周期时间（瞬时效果不适用）
        /// </summary>
        /// <returns>始终返回0表示无周期</returns>
        public virtual float GetPeriod()
        {
            return 0;
        }

        /// <summary>
        /// 获取周期执行的效果（瞬时效果不适用）
        /// </summary>
        /// <returns>始终返回null</returns>
        public virtual IGameplayEffectData GetPeriodExecution()
        {
            return null;
        }

        /// <summary>
        /// 获取资产标签（瞬时效果默认为空）
        /// </summary>
        /// <returns>空标签数组</returns>
        public virtual GameplayTag[] GetAssetTags()
        {
            return Array.Empty<GameplayTag>();
        }

        /// <summary>
        /// 获取授予的标签（瞬时效果默认为空）
        /// </summary>
        /// <returns>空标签数组</returns>
        public virtual GameplayTag[] GetGrantedTags()
        {
            return Array.Empty<GameplayTag>();
        }

        /// <summary>
        /// 获取移除效果标签
        /// </summary>
        /// <returns>移除效果标签数组</returns>
        public GameplayTag[] GetRemoveGameplayEffectsWithTags()
        {
            return RemoveGameplayEffectsWithTags;
        }

        /// <summary>
        /// 获取应用所需标签
        /// </summary>
        /// <returns>应用条件标签数组</returns>
        public GameplayTag[] GetApplicationRequiredTags()
        {
            return ApplicationRequiredTags;
        }

        /// <summary>
        /// 获取应用免疫标签
        /// </summary>
        /// <returns>免疫标签数组</returns>
        public GameplayTag[] GetApplicationImmunityTags()
        {
            return ApplicationImmunityTags;
        }

        /// <summary>
        /// 获取持续所需标签（瞬时效果不适用）
        /// </summary>
        /// <returns>空标签数组</returns>
        public virtual GameplayTag[] GetOngoingRequiredTags()
        {
            return Array.Empty<GameplayTag>();
        }

        /// <summary>
        /// 获取执行时Cue
        /// </summary>
        /// <returns>执行时Cue数组</returns>
        public GameplayCueInstant[] GetCueOnExecute()
        {
            return CueOnExecute;
        }

        /// <summary>
        /// 获取移除时Cue（瞬时效果不适用）
        /// </summary>
        /// <returns>空Cue数组</returns>
        public virtual GameplayCueInstant[] GetCueOnRemove()
        {
            return Array.Empty<GameplayCueInstant>();
        }

        /// <summary>
        /// 获取添加时Cue（瞬时效果不适用）
        /// </summary>
        /// <returns>空Cue数组</returns>
        public virtual GameplayCueInstant[] GetCueOnAdd()
        {
            return Array.Empty<GameplayCueInstant>();
        }

        /// <summary>
        /// 获取激活时Cue（瞬时效果不适用）
        /// </summary>
        /// <returns>空Cue数组</returns>
        public virtual GameplayCueInstant[] GetCueOnActivate()
        {
            return Array.Empty<GameplayCueInstant>();
        }

        /// <summary>
        /// 获取失活时Cue（瞬时效果不适用）
        /// </summary>
        /// <returns>空Cue数组</returns>
        public virtual GameplayCueInstant[] GetCueOnDeactivate()
        {
            return Array.Empty<GameplayCueInstant>();
        }

        /// <summary>
        /// 获取持续Cue（瞬时效果不适用）
        /// </summary>
        /// <returns>空Cue数组</returns>
        public virtual GameplayCueDurational[] GetCueDurational()
        {
            return Array.Empty<GameplayCueDurational>();
        }

        /// <summary>
        /// 获取属性修饰符
        /// </summary>
        /// <returns>修饰符数组</returns>
        public GameplayEffectModifier[] GetModifiers()
        {
            return Modifiers;
        }

        /// <summary>
        /// 获取执行计算（瞬时效果默认为空）
        /// </summary>
        /// <returns>空执行计算数组</returns>
        public virtual GameplayEffectExecutionCalculation[] GetExecutions()
        {
            return Array.Empty<GameplayEffectExecutionCalculation>();
        }
        
        /// <summary>
        /// 获取提前过期效果（瞬时效果不适用）
        /// </summary>
        /// <returns>空效果数组</returns>
        public virtual GameplayEffectAsset[] GetPrematureExpirationEffects()
        {
            return Array.Empty<GameplayEffectAsset>();
        }
        
        /// <summary>
        /// 获取正常过期效果（瞬时效果不适用）
        /// </summary>
        /// <returns>空效果数组</returns>
        public virtual GameplayEffectAsset[] GetRoutineExpirationEffects()
        {
            return Array.Empty<GameplayEffectAsset>();
        }

        /// <summary>
        /// 获取授予的技能（瞬时效果默认为空）
        /// </summary>
        /// <returns>空技能配置数组</returns>
        public virtual GrantedAbilityConfig[] GetGrantedAbilities()
        {
            return Array.Empty<GrantedAbilityConfig>();
        }

        /// <summary>
        /// 获取堆叠策略（瞬时效果默认无堆叠）
        /// </summary>
        /// <returns>无堆叠策略</returns>
        public virtual GameplayEffectStacking GetStacking()
        {
            return GameplayEffectStacking.None;
        }
    }

    /// <summary>
    /// 无限持续游戏效果数据实现，用于创建永久生效的游戏效果
    /// </summary>
    /// <remarks>
    /// InfiniteGameplayEffectData扩展了瞬时效果，添加了持续效果的功能：
    /// 
    /// 无限效果特点：
    /// - 永久存在直到被显式移除
    /// - 支持周期性执行
    /// - 可以授予标签和技能
    /// - 支持持续条件检查
    /// - 完整的Cue生命周期支持
    /// 
    /// 新增功能：
    /// - 周期执行机制
    /// - 标签授予和管理
    /// - 持续条件标签检查
    /// - 完整的Cue事件支持
    /// - 执行计算支持
    /// - 过期效果处理
    /// - 技能授予功能
    /// - 效果堆叠策略
    /// 
    /// 典型使用场景：
    /// - 永久buff/debuff
    /// - 被动技能效果
    /// - 装备提供的属性加成
    /// - 持续的状态效果
    /// </remarks>
    public class InfiniteGameplayEffectData : InstantGameplayEffectData
    {
        /// <summary>
        /// 周期执行间隔时间（秒）
        /// </summary>
        public float Period { get; }

        /// <summary>
        /// 周期执行的效果数据
        /// 每个周期都会执行这个效果
        /// </summary>
        public IGameplayEffectData PeriodExecution { get; set; } = null;

        /// <summary>
        /// 资产标签，用于标识效果类型和特性
        /// </summary>
        public GameplayTag[] AssetTags { get; set; } = Array.Empty<GameplayTag>();
        
        /// <summary>
        /// 授予给目标的标签，效果存在期间目标拥有这些标签
        /// </summary>
        public GameplayTag[] GrantedTags { get; set; } = Array.Empty<GameplayTag>();
        
        /// <summary>
        /// 持续所需标签，目标必须持续拥有这些标签效果才能保持激活
        /// </summary>
        public GameplayTag[] OngoingRequiredTags { get; set; } = Array.Empty<GameplayTag>();

        /// <summary>
        /// 效果移除时的瞬时Cue反馈
        /// </summary>
        public GameplayCueInstant[] CueOnRemove { get; set; } = Array.Empty<GameplayCueInstant>();
        
        /// <summary>
        /// 效果添加时的瞬时Cue反馈
        /// </summary>
        public GameplayCueInstant[] CueOnAdd { get; set; } = Array.Empty<GameplayCueInstant>();
        
        /// <summary>
        /// 效果激活时的瞬时Cue反馈
        /// </summary>
        public GameplayCueInstant[] CueOnActivate { get; set; } = Array.Empty<GameplayCueInstant>();
        
        /// <summary>
        /// 效果失活时的瞬时Cue反馈
        /// </summary>
        public GameplayCueInstant[] CueOnDeactivate { get; set; } = Array.Empty<GameplayCueInstant>();
        
        /// <summary>
        /// 持续期间的Cue反馈
        /// </summary>
        public GameplayCueDurational[] CueDurational { get; set; } = Array.Empty<GameplayCueDurational>();
        
        /// <summary>
        /// 执行计算数组，用于复杂的效果逻辑
        /// </summary>
        public GameplayEffectExecutionCalculation[] Executions { get; set; } = Array.Empty<GameplayEffectExecutionCalculation>();
        
        /// <summary>
        /// 提前过期时触发的效果
        /// </summary>
        public GameplayEffectAsset[] PrematureExpirationEffects { get; set; } = Array.Empty<GameplayEffectAsset>();
        
        /// <summary>
        /// 正常过期时触发的效果
        /// </summary>
        public GameplayEffectAsset[] RoutineExpirationEffects { get; set; } = Array.Empty<GameplayEffectAsset>();
        
        /// <summary>
        /// 效果授予的技能配置
        /// </summary>
        public GrantedAbilityConfig[] GrantedAbilities { get; set; } = Array.Empty<GrantedAbilityConfig>();
        
        /// <summary>
        /// 效果堆叠策略
        /// </summary>
        public GameplayEffectStacking Stacking { get; set; } = GameplayEffectStacking.None;

        /// <summary>
        /// 创建无限持续游戏效果数据实例
        /// </summary>
        /// <param name="name">效果的显示名称</param>
        /// <param name="period">周期执行间隔时间</param>
        public InfiniteGameplayEffectData(string name, float period) : base(name)
        {
            Period = period;
        }

        /// <summary>
        /// 获取效果的持续时间策略（无限持续）
        /// </summary>
        /// <returns>无限持续策略</returns>
        public override EffectsDurationPolicy GetDurationPolicy()
        {
            return EffectsDurationPolicy.Infinite;
        }

        /// <summary>
        /// 获取周期执行间隔
        /// </summary>
        /// <returns>周期时间</returns>
        public override float GetPeriod()
        {
            return Period;
        }

        /// <summary>
        /// 获取周期执行的效果数据
        /// </summary>
        /// <returns>周期执行效果</returns>
        public override IGameplayEffectData GetPeriodExecution()
        {
            return PeriodExecution;
        }

        /// <summary>
        /// 获取资产标签
        /// </summary>
        /// <returns>资产标签数组</returns>
        public override GameplayTag[] GetAssetTags()
        {
            return AssetTags;
        }

        /// <summary>
        /// 获取授予的标签
        /// </summary>
        /// <returns>授予标签数组</returns>
        public override GameplayTag[] GetGrantedTags()
        {
            return GrantedTags;
        }

        /// <summary>
        /// 获取持续所需标签
        /// </summary>
        /// <returns>持续条件标签数组</returns>
        public override GameplayTag[] GetOngoingRequiredTags()
        {
            return OngoingRequiredTags;
        }

        /// <summary>
        /// 获取移除时Cue
        /// </summary>
        /// <returns>移除时Cue数组</returns>
        public override GameplayCueInstant[] GetCueOnRemove()
        {
            return CueOnRemove;
        }

        /// <summary>
        /// 获取添加时Cue
        /// </summary>
        /// <returns>添加时Cue数组</returns>
        public override GameplayCueInstant[] GetCueOnAdd()
        {
            return CueOnAdd;
        }

        /// <summary>
        /// 获取激活时Cue
        /// </summary>
        /// <returns>激活时Cue数组</returns>
        public override GameplayCueInstant[] GetCueOnActivate()
        {
            return CueOnActivate;
        }

        /// <summary>
        /// 获取失活时Cue
        /// </summary>
        /// <returns>失活时Cue数组</returns>
        public override GameplayCueInstant[] GetCueOnDeactivate()
        {
            return CueOnDeactivate;
        }

        /// <summary>
        /// 获取持续Cue
        /// </summary>
        /// <returns>持续Cue数组</returns>
        public override GameplayCueDurational[] GetCueDurational()
        {
            return CueDurational;
        }

        /// <summary>
        /// 获取执行计算
        /// </summary>
        /// <returns>执行计算数组</returns>
        public override GameplayEffectExecutionCalculation[] GetExecutions()
        {
            return Executions;
        }
        
        /// <summary>
        /// 获取提前过期效果
        /// </summary>
        /// <returns>提前过期效果数组</returns>
        public override GameplayEffectAsset[] GetPrematureExpirationEffects()
        {
            return PrematureExpirationEffects;
        }
        
        /// <summary>
        /// 获取正常过期效果
        /// </summary>
        /// <returns>正常过期效果数组</returns>
        public override GameplayEffectAsset[] GetRoutineExpirationEffects()
        {
            return RoutineExpirationEffects;
        }

        /// <summary>
        /// 获取授予的技能
        /// </summary>
        /// <returns>技能配置数组</returns>
        public override GrantedAbilityConfig[] GetGrantedAbilities()
        {
            return GrantedAbilities;
        }

        /// <summary>
        /// 获取堆叠策略
        /// </summary>
        /// <returns>堆叠策略</returns>
        public override GameplayEffectStacking GetStacking()
        {
            return Stacking;
        }
    }

    /// <summary>
    /// 持续时间游戏效果数据实现，用于创建有限时间内生效的游戏效果
    /// </summary>
    /// <remarks>
    /// DurationalGameplayEffectData继承了无限效果的所有功能，并添加了持续时间限制：
    /// 
    /// 持续效果特点：
    /// - 有明确的持续时间限制
    /// - 到期后自动移除
    /// - 支持所有无限效果的功能
    /// - 可以设置过期时的处理逻辑
    /// 
    /// 继承的功能：
    /// - 周期执行机制
    /// - 标签授予和管理
    /// - 完整的Cue支持
    /// - 执行计算
    /// - 技能授予
    /// - 效果堆叠
    /// 
    /// 新增特性：
    /// - 明确的持续时间
    /// - 自动过期处理
    /// - 过期效果触发
    /// 
    /// 典型使用场景：
    /// - 临时buff/debuff
    /// - 持续伤害/治疗效果
    /// - 限时状态效果
    /// - 技能冷却效果
    /// </remarks>
    public class DurationalGameplayEffectData : InfiniteGameplayEffectData
    {
        /// <summary>
        /// 效果持续时间（秒）
        /// </summary>
        public float Duration { get; }

        /// <summary>
        /// 创建持续时间游戏效果数据实例
        /// </summary>
        /// <param name="name">效果的显示名称</param>
        /// <param name="period">周期执行间隔时间</param>
        /// <param name="duration">效果持续时间</param>
        public DurationalGameplayEffectData(string name, float period, float duration) : base(name, period)
        {
            Duration = duration;
        }

        /// <summary>
        /// 获取效果的持续时间策略（持续时间）
        /// </summary>
        /// <returns>持续时间策略</returns>
        public override EffectsDurationPolicy GetDurationPolicy()
        {
            return EffectsDurationPolicy.Duration;
        }

        /// <summary>
        /// 获取效果持续时间
        /// </summary>
        /// <returns>持续时间（秒）</returns>
        public override float GetDuration()
        {
            return Duration;
        }
    }
}