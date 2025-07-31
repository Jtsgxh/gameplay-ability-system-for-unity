using System;

namespace GAS.Runtime
{
    /// <summary>
    /// 技能标签容器，用于管理技能相关的所有GameplayTag配置
    /// </summary>
    /// <remarks>
    /// 技能标签容器是GAS系统中技能标签管理的核心数据结构，定义了技能的各种标签行为：
    /// 
    /// <para><b>资产标签</b>：技能的身份标识标签</para>
    /// <para><b>取消标签</b>：当此技能激活时，会取消具有这些标签的其他技能</para>
    /// <para><b>阻止标签</b>：当具有这些标签的技能激活时，会阻止此技能激活</para>
    /// <para><b>激活拥有标签</b>：技能激活期间临时拥有的标签</para>
    /// <para><b>激活条件标签</b>：技能激活所需的前置条件标签</para>
    /// <para><b>源/目标标签</b>：施法者和目标必须满足的标签条件</para>
    /// 
    /// 参考文档：https://github.com/BillEliot/GASDocumentation_Chinese?tab=readme-ov-file#4610-gameplay-ability-spec
    /// </remarks>
    [Serializable]
    public struct AbilityTagContainer
    {
        /// <summary>
        /// 技能资产标签集合
        /// 用于标识技能类型和分类，常用于技能查找和匹配逻辑
        /// </summary>
        /// <example>
        /// AssetTag 可能包含：
        /// - "Ability.Attack.Melee" (近战攻击技能)
        /// - "Ability.Magic.Fire" (火系魔法技能)
        /// - "Ability.Passive.Buff" (被动增益技能)
        /// </example>
        public GameplayTagSet AssetTag;

        /// <summary>
        /// 取消技能标签集合
        /// 当此技能激活时，拥有这些标签的其他技能将被取消
        /// </summary>
        /// <example>
        /// 攻击技能激活时取消移动技能：
        /// CancelAbilitiesWithTags = {"Ability.Movement"}
        /// </example>
        public GameplayTagSet CancelAbilitiesWithTags;
        
        /// <summary>
        /// 阻止技能标签集合
        /// 当拥有这些标签的技能处于激活状态时，此技能无法激活
        /// </summary>
        /// <example>
        /// 在眩晕状态下无法激活任何技能：
        /// BlockAbilitiesWithTags = {"State.Stunned"}
        /// </example>
        public GameplayTagSet BlockAbilitiesWithTags;

        /// <summary>
        /// 激活期间拥有的标签集合
        /// 技能激活期间临时添加到拥有者的标签，技能结束后移除
        /// </summary>
        /// <example>
        /// 攻击技能激活期间添加攻击状态标签：
        /// ActivationOwnedTag = {"State.Attacking"}
        /// </example>
        public GameplayTagSet ActivationOwnedTag;
        
        /// <summary>
        /// 激活所需标签集合
        /// 拥有者必须同时拥有所有这些标签才能激活技能
        /// </summary>
        /// <example>
        /// 魔法技能需要充足魔法值：
        /// ActivationRequiredTags = {"Resource.Mana.Sufficient"}
        /// </example>
        public GameplayTagSet ActivationRequiredTags;
        
        /// <summary>
        /// 激活阻止标签集合
        /// 拥有者如果拥有任何这些标签则无法激活技能
        /// </summary>
        /// <example>
        /// 沉默状态下无法使用魔法技能：
        /// ActivationBlockedTags = {"State.Silenced"}
        /// </example>
        public GameplayTagSet ActivationBlockedTags;

        /// <summary>
        /// 施法者（源）所需标签集合
        /// 施法者必须拥有这些标签才能使用此技能
        /// </summary>
        /// <example>
        /// 只有法师职业才能使用火球术：
        /// SourceRequiredTags = {"Class.Mage"}
        /// </example>
        public GameplayTagSet SourceRequiredTags;
        
        /// <summary>
        /// 施法者（源）阻止标签集合
        /// 施法者如果拥有这些标签则无法使用此技能
        /// </summary>
        /// <example>
        /// 被诅咒的角色无法使用治疗技能：
        /// SourceBlockedTags = {"Debuff.Cursed"}
        /// </example>
        public GameplayTagSet SourceBlockedTags;
        
        /// <summary>
        /// 目标所需标签集合
        /// 目标必须拥有这些标签才能成为技能目标
        /// </summary>
        /// <example>
        /// 治疗技能只能对友军使用：
        /// TargetRequiredTags = {"Team.Ally"}
        /// </example>
        public GameplayTagSet TargetRequiredTags;
        
        /// <summary>
        /// 目标阻止标签集合
        /// 拥有这些标签的目标无法成为技能目标
        /// </summary>
        /// <example>
        /// 攻击技能无法对无敌目标使用：
        /// TargetBlockedTags = {"State.Invincible"}
        /// </example>
        public GameplayTagSet TargetBlockedTags;

        /// <summary>
        /// 初始化技能标签容器
        /// </summary>
        /// <param name="assetTags">资产标签数组</param>
        /// <param name="cancelAbilityTags">取消技能标签数组</param>
        /// <param name="blockAbilityTags">阻止技能标签数组</param>
        /// <param name="activationOwnedTag">激活拥有标签数组</param>
        /// <param name="activationRequiredTags">激活所需标签数组</param>
        /// <param name="activationBlockedTags">激活阻止标签数组</param>
        /// <param name="sourceRequiredTags">施法者所需标签数组（可选）</param>
        /// <param name="sourceBlockedTags">施法者阻止标签数组（可选）</param>
        /// <param name="targetRequiredTags">目标所需标签数组（可选）</param>
        /// <param name="targetBlockedTags">目标阻止标签数组（可选）</param>
        /// <remarks>
        /// 源和目标标签参数为可选，如果不提供则使用空数组。
        /// 这样设计是为了向后兼容，同时避免不必要的标签检查开销。
        /// </remarks>
        public AbilityTagContainer(
            GameplayTag[] assetTags, 
            GameplayTag[] cancelAbilityTags,
            GameplayTag[] blockAbilityTags, 
            GameplayTag[] activationOwnedTag, 
            GameplayTag[] activationRequiredTags,
            GameplayTag[] activationBlockedTags,
            GameplayTag[] sourceRequiredTags = null,
            GameplayTag[] sourceBlockedTags = null,
            GameplayTag[] targetRequiredTags = null,
            GameplayTag[] targetBlockedTags = null)
        {
            AssetTag = new GameplayTagSet(assetTags);
            CancelAbilitiesWithTags = new GameplayTagSet(cancelAbilityTags);
            BlockAbilitiesWithTags = new GameplayTagSet(blockAbilityTags);
            ActivationOwnedTag = new GameplayTagSet(activationOwnedTag);
            ActivationRequiredTags = new GameplayTagSet(activationRequiredTags);
            ActivationBlockedTags = new GameplayTagSet(activationBlockedTags);
            SourceRequiredTags = new GameplayTagSet(sourceRequiredTags ?? System.Array.Empty<GameplayTag>());
            SourceBlockedTags = new GameplayTagSet(sourceBlockedTags ?? System.Array.Empty<GameplayTag>());
            TargetRequiredTags = new GameplayTagSet(targetRequiredTags ?? System.Array.Empty<GameplayTag>());
            TargetBlockedTags = new GameplayTagSet(targetBlockedTags ?? System.Array.Empty<GameplayTag>());
        }
    }
}