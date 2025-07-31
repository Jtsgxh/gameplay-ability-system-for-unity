using System.Linq;
using GAS.General;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace GAS.Runtime
{
    /// <summary>
    /// AbilitySystemComponent的预设配置资产
    /// 用于在Unity编辑器中配置ASC的初始状态和基础数据
    /// </summary>
    /// <remarks>
    /// 预设配置包含：
    /// - 属性集列表：定义实体拥有的属性集合
    /// - 基础标签：实体的初始标签状态
    /// - 基础技能：实体默认拥有的技能列表
    /// - 描述信息：用于编辑器显示的说明文本
    /// </remarks>
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "AbilitySystemComponentPreset", menuName = "GAS/AbilitySystemComponentPreset")]
#endif
    public class AbilitySystemComponentPreset : ScriptableObject
    {
        /// <summary>
        /// 编辑器界面标签宽度常量
        /// </summary>
        private const int WIDTH_LABEL = 70;
        
        /// <summary>
        /// 技能为空时的错误提示信息
        /// </summary>
        private const string ERROR_ABILITY = "Ability can't be NONE!!";

        /// <summary>
        /// 预设描述信息
        /// 用于在编辑器中显示此预设的用途和说明
        /// </summary>
#if UNITY_EDITOR
        [TitleGroup("Base")]
        [HorizontalGroup("Base/H1", Width = 1 / 3f)]
        [TabGroup("Base/H1/V1", "Summary", SdfIconType.InfoSquareFill, TextColor = "#0BFFC5", Order = 1)]
        [HideLabel]
        [MultiLineProperty(10)]
#endif
        public string Description;

        /// <summary>
        /// 属性集名称数组
        /// 定义此预设将包含的所有属性集类型
        /// </summary>
        /// <remarks>
        /// 属性集决定了实体拥有哪些数值属性（如生命值、魔法值等）
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V2", "Attribute Sets", SdfIconType.PersonLinesFill, TextColor = "#FF7F00", Order = 2)]
        [LabelText(GASTextDefine.ASC_AttributeSet)]
        [LabelWidth(WIDTH_LABEL)]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false, OnTitleBarGUI = "DrawAttributeSetsButtons")]
        [ValueDropdown("@ValueDropdownHelper.AttributeSetChoices", IsUniqueList = true)]
#endif
        public string[] AttributeSets;

        /// <summary>
        /// 绘制属性集编辑器工具栏按钮
        /// </summary>
        /// <remarks>
        /// 提供排序功能，方便在编辑器中管理属性集列表
        /// </remarks>
        private void DrawAttributeSetsButtons()
        {
#if UNITY_EDITOR
            if (SirenixEditorGUI.ToolbarButton(SdfIconType.SortAlphaDown))
            {
                AttributeSets = AttributeSets.OrderBy(x => x).ToArray();
            }
#endif
        }

        /// <summary>
        /// 基础标签数组
        /// 定义实体在初始化时将拥有的所有固定标签
        /// </summary>
        /// <remarks>
        /// 基础标签用于：
        /// - 标识实体类型和属性
        /// - 影响技能激活条件
        /// - 控制效果应用逻辑
        /// </remarks>
#if UNITY_EDITOR
        [TabGroup("Base/H1/V3", "Tags", SdfIconType.TagsFill, TextColor = "#45B1FF", Order = 3)]
        [LabelText(GASTextDefine.ASC_BASE_TAG)]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false, OnTitleBarGUI = "DrawBaseTagsButtons")]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
#endif
        public GameplayTag[] BaseTags;

        /// <summary>
        /// 绘制基础标签编辑器工具栏按钮
        /// </summary>
        /// <remarks>
        /// 提供排序功能，方便在编辑器中管理标签列表
        /// </remarks>
        private void DrawBaseTagsButtons()
        {
#if UNITY_EDITOR
            if (SirenixEditorGUI.ToolbarButton(SdfIconType.SortAlphaDown))
            {
                BaseTags = BaseTags.OrderBy(x => x.Name).ToArray();
            }
#endif
        }

        /// <summary>
        /// 基础技能资产数组
        /// 定义实体在初始化时将拥有的所有技能
        /// </summary>
        /// <remarks>
        /// 基础技能是实体的核心能力，包括：
        /// - 主动技能（需要玩家触发）
        /// - 被动技能（自动生效）
        /// - 系统技能（内部逻辑使用）
        /// </remarks>
#if UNITY_EDITOR
        [HorizontalGroup("Base/H2")]
        [TabGroup("Base/H2/V1", "Abilities", SdfIconType.YinYang, TextColor = "#D6626E", Order = 1)]
        [LabelText(GASTextDefine.ASC_BASE_ABILITY)]
        [ListDrawerSettings(ShowFoldout = true, ShowItemCount = false, OnTitleBarGUI = "DrawBaseAbilitiesButtons")]
        [AssetSelector]
        [InfoBox(ERROR_ABILITY, InfoMessageType.Error, VisibleIf = "@IsAbilityNone()")]
#endif
        public AbilityAsset[] BaseAbilities;

        /// <summary>
        /// 绘制基础技能编辑器工具栏按钮
        /// </summary>
        /// <remarks>
        /// 提供排序功能，方便在编辑器中管理技能列表
        /// </remarks>
        private void DrawBaseAbilitiesButtons()
        {
#if UNITY_EDITOR
            if (SirenixEditorGUI.ToolbarButton(SdfIconType.SortAlphaDown))
            {
                BaseAbilities = BaseAbilities.OrderBy(x => x.name).ToArray();
            }
#endif
        }

        /// <summary>
        /// 检查技能数组中是否存在空引用
        /// </summary>
        /// <returns>如果存在空引用返回true，否则返回false</returns>
        /// <remarks>
        /// 用于编辑器验证，确保所有技能配置都是有效的
        /// </remarks>
        bool IsAbilityNone()
        {
            return BaseAbilities != null && BaseAbilities.Any(ability => ability == null);
        }
    }
}