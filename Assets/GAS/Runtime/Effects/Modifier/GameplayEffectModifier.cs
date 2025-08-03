using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS.Runtime
{
    public enum GEOperation
    {
#if UNITY_EDITOR
        [LabelText(SdfIconType.PlusLg, Text = "加")]
#endif
        Add = 0,

#if UNITY_EDITOR
        [LabelText(SdfIconType.DashLg, Text = "减")]
#endif
        Minus = 3,

#if UNITY_EDITOR
        [LabelText(SdfIconType.XLg, Text = "乘")]
#endif
        Multiply = 1,

#if UNITY_EDITOR
        [LabelText(SdfIconType.SlashLg, Text = "除")]
#endif
        Divide = 4,

#if UNITY_EDITOR
        [LabelText(SdfIconType.Pencil, Text = "替")]
#endif
        Override = 2,
    }

    [Flags]
    public enum SupportedOperation : byte
    {
        None = 0,

#if UNITY_EDITOR
        [LabelText(SdfIconType.PlusLg, Text = "加")]
#endif
        Add = 1 << GEOperation.Add,

#if UNITY_EDITOR
        [LabelText(SdfIconType.DashLg, Text = "减")]
#endif
        Minus = 1 << GEOperation.Minus,

#if UNITY_EDITOR
        [LabelText(SdfIconType.XLg, Text = "乘")]
#endif
        Multiply = 1 << GEOperation.Multiply,

#if UNITY_EDITOR
        [LabelText(SdfIconType.SlashLg, Text = "除")]
#endif
        Divide = 1 << GEOperation.Divide,

#if UNITY_EDITOR
        [LabelText(SdfIconType.Pencil, Text = "替")]
#endif
        Override = 1 << GEOperation.Override,

        All = Add | Minus | Multiply | Divide | Override
    }

    [Serializable]
    public struct GameplayEffectModifier
    {
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器标签宽度常量
        /// </summary>
        private const int LABEL_WIDTH = 70;

        [LabelText("修改属性", SdfIconType.Fingerprint)]
        [LabelWidth(LABEL_WIDTH)]
        [OnValueChanged("OnAttributeChanged")]
        [ValueDropdown("@ValueDropdownHelper.AttributeChoices", IsUniqueList = true)]
        [Tooltip("指的是GameplayEffect作用对象被修改的属性。")]
        [InfoBox("未选择属性", InfoMessageType.Error, VisibleIf = "@string.IsNullOrWhiteSpace($value)")]
        [SuffixLabel("@ReflectionHelper.GetAttribute($value)?.CalculateMode")]
        [PropertyOrder(1)]
#endif
        public string AttributeName;

#if UNITY_EDITOR
        [HideInInspector]
#endif
        public string AttributeSetName;

#if UNITY_EDITOR
        [HideInInspector]
#endif
        public string AttributeShortName;

#if UNITY_EDITOR
        [LabelText("运算参数", SdfIconType.Activity)]
        [LabelWidth(LABEL_WIDTH)]
        [Tooltip("修改器的基础数值。这个数值如何使用由MMC的运行逻辑决定。\nMMC未指定时直接使用这个值。")]
        [InfoBox("除数不能为零", InfoMessageType.Error,
            VisibleIf = "@Operation == GEOperation.Divide && ModiferMagnitude == 0 && MMC == null")]
        [PropertyOrder(3)]
#endif
        public float ModiferMagnitude;

#if UNITY_EDITOR
        [LabelText("运算法则", SdfIconType.PlusSlashMinus)]
        [LabelWidth(LABEL_WIDTH)]
        [EnumToggleButtons]
        [PropertyOrder(2)]
        [ValidateInput("@ReflectionHelper.GetAttribute(AttributeName).IsSupportOperation($value)", "非法运算: 该属性不支持的此运算法则")]
#endif
        public GEOperation Operation;

#if UNITY_EDITOR
        [LabelText("参数修饰", SdfIconType.CpuFill)]
        [LabelWidth(LABEL_WIDTH)]
        [AssetSelector]
        [Tooltip("ModifierMagnitudeCalculation，修改器，负责GAS中Attribute的数值计算逻辑。\n可以为空(不对\"计算参数\"做任何修改)。")]
        [PropertyOrder(4)]
#endif
        public ModifierMagnitudeCalculation MMC;

        #region Tag Requirements
        
#if UNITY_EDITOR
        [FoldoutGroup("标签要求")]
        [LabelText("源必需标签", SdfIconType.TagFill)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Tooltip("源(Source)必须拥有所有这些标签，修改器才会生效")]
        [PropertyOrder(5)]
#endif
        public GameplayTag[] SourceRequiredTags;

#if UNITY_EDITOR
        [FoldoutGroup("标签要求")]
        [LabelText("源忽略标签", SdfIconType.XCircleFill)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Tooltip("如果源(Source)拥有任何这些标签，修改器将不会生效")]
        [PropertyOrder(6)]
#endif
        public GameplayTag[] SourceIgnoredTags;

#if UNITY_EDITOR
        [FoldoutGroup("标签要求")]
        [LabelText("目标必需标签", SdfIconType.TagFill)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Tooltip("目标(Target)必须拥有所有这些标签，修改器才会生效")]
        [PropertyOrder(7)]
#endif
        public GameplayTag[] TargetRequiredTags;

#if UNITY_EDITOR
        [FoldoutGroup("标签要求")]
        [LabelText("目标忽略标签", SdfIconType.XCircleFill)]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", IsUniqueList = true, HideChildProperties = true)]
        [Tooltip("如果目标(Target)拥有任何这些标签，修改器将不会生效")]
        [PropertyOrder(8)]
#endif
        public GameplayTag[] TargetIgnoredTags;
        
        #endregion

        public GameplayEffectModifier(
            string attributeName,
            float modiferMagnitude,
            GEOperation operation,
            ModifierMagnitudeCalculation mmc = null,
            GameplayTag[] sourceRequiredTags = null,
            GameplayTag[] sourceIgnoredTags = null,
            GameplayTag[] targetRequiredTags = null,
            GameplayTag[] targetIgnoredTags = null)
        {
            AttributeName = attributeName;
            var splits = attributeName.Split('.');
            AttributeSetName = splits[0];
            AttributeShortName = splits[1];
            ModiferMagnitude = modiferMagnitude;
            Operation = operation;
            MMC = mmc;
            SourceRequiredTags = sourceRequiredTags;
            SourceIgnoredTags = sourceIgnoredTags;
            TargetRequiredTags = targetRequiredTags;
            TargetIgnoredTags = targetIgnoredTags;
        }

        public float CalculateMagnitude(GameplayEffectSpec spec, float modifierMagnitude)
        {
            return MMC == null ? ModiferMagnitude : MMC.CalculateMagnitude(spec, modifierMagnitude);
        }

        public void SetModiferMagnitude(float value)
        {
            ModiferMagnitude = value;
        }

#if UNITY_EDITOR
        void OnAttributeChanged()
        {
            var split = AttributeName.Split('.');
            AttributeSetName = split[0];
            AttributeShortName = split[1];

            if (ReflectionHelper.GetAttribute(AttributeName)?.CalculateMode !=
                CalculateMode.Stacking)
            {
                Operation = GEOperation.Override;
            }
        }
#endif

        /// <summary>
        /// 检查修改器是否应该被应用（基于源和目标的标签要求）
        /// </summary>
        /// <param name="modifier">要检查的修改器</param>
        /// <param name="source">效果源组件</param>
        /// <param name="target">效果目标组件</param>
        /// <returns>如果应该应用返回true，否则返回false</returns>
        public static bool ShouldApplyModifier(GameplayEffectModifier modifier, AbilitySystemComponent source, AbilitySystemComponent target)
        {
            // 性能优化：如果没有任何标签要求，直接通过
            if ((modifier.SourceRequiredTags == null || modifier.SourceRequiredTags.Length == 0) &&
                (modifier.SourceIgnoredTags == null || modifier.SourceIgnoredTags.Length == 0) &&
                (modifier.TargetRequiredTags == null || modifier.TargetRequiredTags.Length == 0) &&
                (modifier.TargetIgnoredTags == null || modifier.TargetIgnoredTags.Length == 0))
            {
                return true;
            }

            // 检查源的必需标签
            if (modifier.SourceRequiredTags != null && modifier.SourceRequiredTags.Length > 0)
            {
                var sourceRequiredTagSet = new GameplayTagSet(modifier.SourceRequiredTags);
                if (!source.HasAllTags(sourceRequiredTagSet))
                {
                    return false;
                }
            }

            // 检查源的忽略标签
            if (modifier.SourceIgnoredTags != null && modifier.SourceIgnoredTags.Length > 0)
            {
                var sourceIgnoredTagSet = new GameplayTagSet(modifier.SourceIgnoredTags);
                if (source.HasAnyTags(sourceIgnoredTagSet))
                {
                    return false;
                }
            }

            // 检查目标的必需标签
            if (modifier.TargetRequiredTags != null && modifier.TargetRequiredTags.Length > 0)
            {
                var targetRequiredTagSet = new GameplayTagSet(modifier.TargetRequiredTags);
                if (!target.HasAllTags(targetRequiredTagSet))
                {
                    return false;
                }
            }

            // 检查目标的忽略标签
            if (modifier.TargetIgnoredTags != null && modifier.TargetIgnoredTags.Length > 0)
            {
                var targetIgnoredTagSet = new GameplayTagSet(modifier.TargetIgnoredTags);
                if (target.HasAnyTags(targetIgnoredTagSet))
                {
                    return false;
                }
            }

            return true;
        }
    }
}