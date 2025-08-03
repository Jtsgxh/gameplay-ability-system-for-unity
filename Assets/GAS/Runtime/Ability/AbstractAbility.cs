using System.Collections.Generic;
using GAS.Runtime;

namespace GAS.Runtime
{
    /// <summary>
    /// 技能的抽象基类，定义了所有技能的通用属性和行为
    /// </summary>
    /// <remarks>
    /// AbstractAbility是GAS系统中所有技能的基类，包含：
    /// - 技能名称和数据引用
    /// - 标签系统（激活条件、阻挡条件等）
    /// - 冷却和消耗机制
    /// - 技能实例创建逻辑
    /// 
    /// 子类需要实现CreateSpec方法来创建具体的技能实例。
    /// </remarks>
    public abstract class AbstractAbility
    {
        /// <summary>
        /// 技能的唯一标识名称
        /// 从AbilityAsset的UniqueName字段获取，用于技能的查找和管理
        /// </summary>
        public readonly string Name;
        
        /// <summary>
        /// 技能资产数据引用
        /// 包含技能的所有配置信息，如标签、消耗、冷却等
        /// </summary>
        public readonly AbilityAsset DataReference;

        // TODO : AbilityTask
        // public List<OngoingAbilityTask> OngoingAbilityTasks=new List<OngoingAbilityTask>();
        // public List<AsyncAbilityTask> AsyncAbilityTasks = new List<AsyncAbilityTask>();

        /// <summary>
        /// 技能标签容器
        /// 包含技能的所有标签配置，如激活条件、阻止条件、资产标签等
        /// </summary>
        public AbilityTagContainer Tag { get; protected set; }

        /// <summary>
        /// 技能冷却效果
        /// 技能使用后应用的冷却GameplayEffect，为null表示无冷却
        /// </summary>
        public GameplayEffect Cooldown { get; protected set; }

        /// <summary>
        /// 技能冷却时间（秒）
        /// 技能使用后需要等待的时间，影响冷却效果的持续时间
        /// </summary>
        public float CooldownTime { get; protected set; }

        /// <summary>
        /// 技能消耗效果
        /// 技能激活时应用的消耗GameplayEffect，为null表示无消耗
        /// </summary>
        public GameplayEffect Cost { get; protected set; }

        public AbstractAbility(AbilityAsset abilityAsset)
        {
            DataReference = abilityAsset;

            Name = DataReference.UniqueName;
            Tag = new AbilityTagContainer(
                DataReference.AssetTags, DataReference.CancelAbilityTags, DataReference.BlockAbilityTags,
                DataReference.ActivationOwnedTags, DataReference.ActivationRequiredTags, DataReference.ActivationBlockedTags,
                DataReference.SourceRequiredTags, DataReference.SourceBlockedTags,
                DataReference.TargetRequiredTags, DataReference.TargetBlockedTags);
            Cooldown = DataReference.Cooldown ? new GameplayEffect(DataReference.Cooldown) : default;
            Cost = DataReference.Cost ? new GameplayEffect(DataReference.Cost) : default;

            CooldownTime = DataReference.CooldownTime;
        }

        /// <summary>
        /// 创建技能实例规格（抽象方法）
        /// </summary>
        /// <param name="owner">技能的拥有者组件</param>
        /// <returns>创建的技能实例规格</returns>
        /// <remarks>
        /// 此方法必须由子类实现，用于创建具体的技能实例。
        /// AbilitySpec包含了技能的运行时状态和行为逻辑。
        /// </remarks>
        public abstract AbilitySpec CreateSpec(AbilitySystemComponent owner);

        /// <summary>
        /// 设置技能的冷却效果
        /// </summary>
        /// <param name="coolDown">冷却效果，必须是持续时间类型</param>
        /// <remarks>
        /// 冷却效果必须是 Duration 类型，因为冷却需要持续一段时间。
        /// 在技能激活时，这个效果会被应用到技能拥有者上。
        /// 冷却期间，技能无法再次激活。
        /// </remarks>
        /// <example>
        /// // 设置一个5秒的冷却
        /// var cooldownEffect = new GameplayEffect(cooldownEffectAsset);
        /// ability.SetCooldown(cooldownEffect);
        /// </example>
        public void SetCooldown(GameplayEffect coolDown)
        {
            if (coolDown.DurationPolicy == EffectsDurationPolicy.Duration)
            {
                Cooldown = coolDown;
            }
#if UNITY_EDITOR
            else
            {
                UnityEngine.Debug.LogError("[EX] Cooldown must be duration policy!");
            }
#endif
        }

        /// <summary>
        /// 设置技能的消耗效果
        /// </summary>
        /// <param name="cost">消耗效果，必须是瞬时类型</param>
        /// <remarks>
        /// 消耗效果必须是 Instant 类型，因为消耗是在激活时立即执行的。
        /// 在技能激活时，这个效果会被应用到技能拥有者上。
        /// 常用于消耗魔法值、体力值或其他资源。
        /// </remarks>
        /// <example>
        /// // 设置一个消耗魔法值的效果
        /// var costEffect = new GameplayEffect(manaCostEffectAsset);
        /// ability.SetCost(costEffect);
        /// </example>
        public void SetCost(GameplayEffect cost)
        {
            if (cost.DurationPolicy == EffectsDurationPolicy.Instant)
            {
                Cost = cost;
            }
#if UNITY_EDITOR
            else
            {
                UnityEngine.Debug.LogError("[EX] Cost must be instant policy!");
            }
#endif
        }
    }

    /// <summary>
    /// 技能的泛型抽象基类，提供对特定类型技能资产的强类型访问
    /// </summary>
    /// <typeparam name="T">技能资产类型</typeparam>
    /// <remarks>
    /// 这个泛型版本提供了强类型的AbilityAsset访问，避免了类型转换。
    /// 建议子类继承此泛型版本而不是直接继承AbstractAbility。
    /// </remarks>
    /// <example>
    /// // 创建一个攻击技能类
    /// public class SwordAttackAbility : AbstractAbility<SwordAttackAbilityAsset>
    /// {
    ///     public SwordAttackAbility(SwordAttackAbilityAsset asset) : base(asset) { }
    ///     
    ///     public override AbilitySpec CreateSpec(AbilitySystemComponent owner)
    ///     {
    ///         return new SwordAttackAbilitySpec(this, owner);
    ///     }
    /// }
    /// </example>
    public abstract class AbstractAbility<T> : AbstractAbility where T : AbilityAsset
    {
        /// <summary>
        /// 获取强类型的技能资产引用
        /// </summary>
        public T AbilityAsset => DataReference as T;

        /// <summary>
        /// 初始化泛型技能实例
        /// </summary>
        /// <param name="abilityAsset">技能资产</param>
        protected AbstractAbility(T abilityAsset) : base(abilityAsset)
        {
        }
    }
}