using System;
using System.Collections;
using System.Linq;
using GAS.General;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace GAS.Runtime
{
    /// <summary>
    /// 技能资产基类，定义技能的配置数据和编辑器界面
    /// </summary>
    /// <remarks>
    /// AbilityAsset是所有技能配置的基础类，包含：
    /// - 技能的基础信息（名称、描述、类型等）
    /// - 消耗和冷却配置
    /// - 完整的标签系统配置
    /// - Unity编辑器的可视化界面配置
    /// 
    /// 每个具体的技能资产都应该继承此类并实现AbilityType()方法，
    /// 以指定该资产对应的具体技能实现类型。
    /// </remarks>
    public abstract class AbilityAsset : ScriptableObject
    {
        /// <summary>
        /// 编辑器界面标签宽度常量
        /// </summary>
        protected const int WIDTH_LABEL = 70;

        /// <summary>
        /// 技能类选择下拉菜单数据源（编辑器用）
        /// </summary>
        private static IEnumerable AbilityClassChoice = new ValueDropdownList<string>();

        /// <summary>
        /// 获取此技能资产对应的具体技能类型
        /// </summary>
        /// <returns>技能的运行时类型</returns>
        /// <remarks>
        /// 此抽象方法必须由子类实现，用于指定该资产对应的具体技能实现类。
        /// 返回的类型将用于运行时创建技能实例。
        /// </remarks>
        public abstract Type AbilityType();

        /// <summary>
        /// 技能描述信息
        /// 用于在编辑器中显示技能的详细说明和用途
        /// </summary>
#if UNITY_EDITOR
        [TitleGroup("Base")]
        [HorizontalGroup("Base/H1")]
        [TabGroup("Base/H1/V1", "Summary", SdfIconType.InfoSquareFill, TextColor = "#0BFFC5", Order = 1)]
        [HideLabel]
        [MultiLineProperty(10)]
#endif
        public string Description;

        /// <summary>
        /// 获取技能实例类的完整名称
        /// 用于编辑器显示和调试信息
        /// </summary>
        /// <value>如果AbilityType()返回有效类型则返回类型全名，否则返回null</value>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V2", "General", SdfIconType.AwardFill, TextColor = "#FF7F00", Order = 2)]
        [LabelText("所属能力", SdfIconType.FileCodeFill)]
        [LabelWidth(WIDTH_LABEL)]
        [ShowInInspector]
        [InfoBox("Ability Class is NULL!!! Please check.", InfoMessageType.Error, VisibleIf = "@AbilityType() == null")]
        [PropertyOrder(-1)]
#endif
        public string InstanceAbilityClassFullName => AbilityType() != null ? AbilityType().FullName : null;

        /// <summary>
        /// 获取当前资产类的类型名称（编辑器显示用）
        /// </summary>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V2", "General")]
        [TabGroup("Base/H1/V2", "Detail", SdfIconType.TicketDetailedFill, TextColor = "#BC2FDE")]
        [LabelText("类型名称", SdfIconType.FileCodeFill)]
        [LabelWidth(WIDTH_LABEL)]
        [ShowInInspector]
        [PropertyOrder(-1)]
        public string TypeName => GetType().Name;

        /// <summary>
        /// 获取当前资产类的类型全名（编辑器显示用）
        /// </summary>
        [TabGroup("Base/H1/V2", "Detail")]
        [LabelText("类型全名", SdfIconType.FileCodeFill)]
        [LabelWidth(WIDTH_LABEL)]
        [ShowInInspector]
        [PropertyOrder(-1)]
        public string TypeFullName => GetType().FullName;

        /// <summary>
        /// 获取当前资产类的继承链（编辑器显示用）
        /// </summary>
        [TabGroup("Base/H1/V2", "Detail")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false, ShowPaging = false)]
        [ShowInInspector]
        [LabelText("继承关系")]
        [LabelWidth(WIDTH_LABEL)]
        [PropertyOrder(-1)]
        public string[] InheritanceChain => GetType().GetInheritanceChain().Reverse().ToArray();
#endif

        /// <summary>
        /// 技能的唯一标识名称
        /// 用于在代码中引用和识别技能，必须符合C#标识符命名规则
        /// </summary>
        /// <remarks>
        /// 唯一名称用于：
        /// - 技能的激活和查找
        /// - 技能容器中的键值标识
        /// - 配置文件和数据绑定
        /// 
        /// 建议使用有意义的名称，如"SwordAttack"、"Fireball"等。
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V2", "General", SdfIconType.AwardFill)]
        [InfoBox(GASTextDefine.TIP_UNAME, InfoMessageType.None)]
        [LabelText("U-Name", SdfIconType.Fingerprint)]
        [LabelWidth(WIDTH_LABEL)]
        [InfoBox("无效的名字 - 不符合C#标识符命名规则", InfoMessageType.Error,
            "@GAS.General.Validation.Validations.IsValidVariableName($value) == false")]
        [InlineButton("@UniqueName = name", "Auto", Icon = SdfIconType.Hammer)]
#endif
        public string UniqueName;

        /// <summary>
        /// 技能消耗效果资产
        /// 定义技能使用时消耗的资源（如魔法值、体力值等）
        /// </summary>
        /// <remarks>
        /// 消耗效果通常是瞬时效果，在技能激活时立即应用。
        /// 可以包含多种资源的消耗，如同时消耗魔法值和体力值。
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V2", "General")]
        [Title("消耗&冷却", bold: true)]
        [LabelWidth(WIDTH_LABEL)]
        [AssetSelector]
        [LabelText(SdfIconType.HeartHalf, Text = GASTextDefine.ABILITY_EFFECT_COST)]
#endif
        public GameplayEffectAsset Cost;

        /// <summary>
        /// 技能冷却效果资产
        /// 定义技能使用后的冷却时间机制
        /// </summary>
        /// <remarks>
        /// 冷却效果通常是持续时间效果，持续时间由CooldownTime决定。
        /// 冷却期间，技能无法再次激活。
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V2", "General")]
        [LabelWidth(WIDTH_LABEL)]
        [AssetSelector]
        [LabelText(SdfIconType.StopwatchFill, Text = GASTextDefine.ABILITY_EFFECT_CD)]
#endif
        public GameplayEffectAsset Cooldown;

        /// <summary>
        /// 技能冷却时间（秒）
        /// 技能使用后需要等待的时间才能再次使用
        /// </summary>
        /// <remarks>
        /// 冷却时间影响技能的使用频率和战斗节奏。
        /// 可以通过其他效果或属性来修改实际的冷却时间。
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V2", "General")]
        [LabelWidth(WIDTH_LABEL)]
        [LabelText(SdfIconType.ClockFill, Text = GASTextDefine.ABILITY_CD_TIME)]
        [Unit(Units.Second)]
#endif
        public float CooldownTime;

        /// <summary>
        /// 技能资产标签数组
        /// 用于描述技能的特性表现，如伤害、治疗、控制等
        /// </summary>
        /// <remarks>
        /// 资产标签用于：
        /// - 技能分类和查找
        /// - 效果和其他技能的匹配逻辑
        /// - 技能系统的条件判断
        /// 
        /// 例如：["Ability.Attack.Melee", "Damage.Physical"]
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V3", "Tags", SdfIconType.TagsFill, TextColor = "#45B1FF", Order = 3)]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Tooltip("描述性质的标签，用来描述Ability的特性表现，比如伤害、治疗、控制等。")]
        [FormerlySerializedAs("AssetTag")]
#endif
        public GameplayTag[] AssetTags;

        /// <summary>
        /// 取消技能标签数组
        /// 此技能激活时，拥有任意这些标签的其他技能会被取消
        /// </summary>
        /// <remarks>
        /// 用于实现技能间的互斥逻辑，如：
        /// - 攻击技能激活时取消移动技能
        /// - 施法技能激活时取消其他施法技能
        /// 
        /// 例如：["Ability.Movement"] - 取消所有移动类技能
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V3", "Tags")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [LabelText("CancelAbility With Tags ")]
        [Space]
        [Tooltip("Ability激活时，Ability持有者当前持有的所有Ability中，拥有【任意】这些标签的Ability会被取消。")]
#endif
        public GameplayTag[] CancelAbilityTags;

        /// <summary>
        /// 阻止技能标签数组
        /// 当拥有任意这些标签的技能处于激活状态时，此技能无法激活
        /// </summary>
        /// <remarks>
        /// 用于实现技能的阻止逻辑，如：
        /// - 在眩晕状态下无法激活任何技能
        /// - 在施法状态下无法激活移动技能
        /// 
        /// 例如：["State.Stunned"] - 眩晕时无法激活
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V3", "Tags")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [LabelText("BlockAbility With Tags ")]
        [Space]
        [Tooltip("Ability激活时，Ability持有者当前持有的所有Ability中，拥有【任意】这些标签的Ability会被阻塞激活。")]
#endif
        public GameplayTag[] BlockAbilityTags;

        /// <summary>
        /// 激活期间拥有的标签数组
        /// 技能激活时临时添加到持有者的标签，技能结束时移除
        /// </summary>
        /// <remarks>
        /// 用于标识技能的激活状态，如：
        /// - 攻击技能激活期间添加"State.Attacking"标签
        /// - 移动技能激活期间添加"State.Moving"标签
        /// 
        /// 这些标签可以被其他系统用于判断和响应。
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V3", "Tags")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Space]
        [Tooltip("Ability激活时，持有者会获得这些标签，Ability被失活时，这些标签也会被移除。")]
        [FormerlySerializedAs("ActivationOwnedTag")]
#endif
        public GameplayTag[] ActivationOwnedTags;

        /// <summary>
        /// 激活所需标签数组
        /// 技能只有在其拥有者拥有所有这些标签时才可激活
        /// </summary>
        /// <remarks>
        /// 用于定义技能的前置条件，如：
        /// - 魔法技能需要"Resource.Mana.Sufficient"标签
        /// - 武器技能需要"Equipment.Weapon.Equipped"标签
        /// 
        /// 必须同时满足所有条件才能激活技能。
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V3", "Tags")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Space]
        [Tooltip("Ability只有在其拥有者拥有【所有】这些标签时才可激活。")]
#endif
        public GameplayTag[] ActivationRequiredTags;

        /// <summary>
        /// 激活阻止标签数组
        /// 技能在其拥有者拥有任意这些标签时不能被激活
        /// </summary>
        /// <remarks>
        /// 用于定义技能的禁用条件，如：
        /// - 沉默状态下无法使用魔法技能："State.Silenced"
        /// - 定身状态下无法使用移动技能："State.Rooted"
        /// 
        /// 只要拥有任意一个阻止标签就无法激活技能。
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V3", "Tags")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Space]
        [Tooltip("Ability在其拥有者拥有【任意】这些标签时不能被激活。")]
#endif
        public GameplayTag[] ActivationBlockedTags;
        
        /// <summary>
        /// 施法者（源）所需标签数组
        /// 施法者必须拥有所有这些标签才能激活技能
        /// </summary>
        /// <remarks>
        /// 用于限制谁可以使用此技能，如：
        /// - 只有法师职业才能使用火球术："Class.Mage"
        /// - 只有持有法杖才能施法："Equipment.Staff"
        /// 
        /// 与ActivationRequiredTags的区别是更加明确地表示施法者的条件。
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V3", "Tags")]
        [FoldoutGroup("Base/H1/V3/Tags/Source/Target Tags", false)]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Tooltip("源（施法者）必须拥有【所有】这些标签才能激活技能。")]
#endif
        public GameplayTag[] SourceRequiredTags;
        
        /// <summary>
        /// 施法者（源）阻止标签数组
        /// 施法者拥有任意这些标签时不能激活技能
        /// </summary>
        /// <remarks>
        /// 用于限制在特定状态下不能施法，如：
        /// - 被诅咒的角色无法使用治疗技能："Debuff.Cursed"
        /// - 变形状态下无法使用人形技能："State.Transformed"
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V3", "Tags")]
        [FoldoutGroup("Base/H1/V3/Tags/Source/Target Tags")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Tooltip("源（施法者）拥有【任意】这些标签时不能激活技能。")]
#endif
        public GameplayTag[] SourceBlockedTags;
        
        /// <summary>
        /// 目标所需标签数组
        /// 目标必须拥有所有这些标签才能成为有效目标
        /// </summary>
        /// <remarks>
        /// 用于限制技能可以作用的目标，如：
        /// - 治疗技能只能对友军使用："Team.Ally"
        /// - 复活技能只能对死亡单位使用："State.Dead"
        /// 
        /// 在技能选择目标时会进行此检查。
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V3", "Tags")]
        [FoldoutGroup("Base/H1/V3/Tags/Source/Target Tags")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Tooltip("目标必须拥有【所有】这些标签才能成为有效目标。")]
#endif
        public GameplayTag[] TargetRequiredTags;
        
        /// <summary>
        /// 目标阻止标签数组
        /// 拥有任意这些标签的目标不能成为有效目标
        /// </summary>
        /// <remarks>
        /// 用于排除特定目标，如：
        /// - 攻击技能无法对无敌目标使用："State.Invincible"
        /// - 魅惑技能无法对免疫魅惑的目标使用："Immunity.Charm"
        /// 
        /// 在技能选择目标时会进行此检查。
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V3", "Tags")]
        [FoldoutGroup("Base/H1/V3/Tags/Source/Target Tags")]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Tooltip("目标拥有【任意】这些标签时不能成为有效目标。")]
#endif
        public GameplayTag[] TargetBlockedTags;
    }

    /// <summary>
    /// 泛型技能资产基类，自动关联指定的技能实现类型
    /// </summary>
    /// <typeparam name="T">技能实现类类型</typeparam>
    /// <remarks>
    /// 这个泛型版本简化了技能资产的创建，自动处理类型关联。
    /// 建议所有具体的技能资产都继承此泛型基类而不是直接继承AbilityAsset。
    /// 
    /// 例如：
    /// public class FireballAbilityAsset : AbilityAssetT&lt;FireballAbility&gt; { }
    /// </remarks>
    public abstract class AbilityAssetT<T> : AbilityAsset where T : class
    {
        /// <summary>
        /// 返回泛型参数指定的技能类型
        /// </summary>
        /// <returns>T类型的Type对象</returns>
        /// <remarks>
        /// 此方法被密封，子类无需重写。类型由泛型参数T自动确定。
        /// </remarks>
        public sealed override Type AbilityType()
        {
            return typeof(T);
        }
    }
}