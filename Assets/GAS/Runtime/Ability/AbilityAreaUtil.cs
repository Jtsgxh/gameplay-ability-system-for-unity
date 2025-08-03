using System;

namespace GAS.Runtime
{
    public enum EffectCenterType
    {
        SelfOffset,
        WorldSpace,
        TargetOffset
    }

    /// <summary>
    /// 技能区域检测工具类
    /// </summary>
    /// <remarks>
    /// 注意：在纯.NET环境中，Unity的物理系统不可用。
    /// 如需在.NET环境中使用区域检测功能，请使用第三方物理引擎或自定义实现。
    /// 
    /// 这个类提供了接口兼容性，但所有物理检测方法都会抛出NotSupportedException。
    /// 在实际使用中，您需要：
    /// 1. 使用第三方.NET物理引擎（如Box2D.NET）
    /// 2. 实现自定义的2D/3D几何计算
    /// 3. 或者在Unity环境中使用原版实现
    /// </remarks>
    public static class AbilityAreaUtil
    {
        /// <summary>
        /// 2D矩形重叠检测（占位符实现）
        /// </summary>
        [Obsolete("此方法在纯.NET环境中不可用，需要Unity物理系统支持")]
        public static object[] OverlapBox2D(this AbilitySystemComponent asc, object offset, object size,
            float angle, int layerMask, object relativeTransform = null)
        {
            throw new NotSupportedException("Unity物理系统在纯.NET环境中不可用。请使用第三方物理引擎或自定义实现。");
        }

        /// <summary>
        /// 2D矩形重叠检测（非分配版本，占位符实现）
        /// </summary>
        [Obsolete("此方法在纯.NET环境中不可用，需要Unity物理系统支持")]
        public static int OverlapBox2DNonAlloc(this AbilitySystemComponent asc, object offset, object size,
            float angle, object[] results, int layerMask, object relativeTransform = null)
        {
            throw new NotSupportedException("Unity物理系统在纯.NET环境中不可用。请使用第三方物理引擎或自定义实现。");
        }

        /// <summary>
        /// 2D圆形重叠检测（占位符实现）
        /// </summary>
        [Obsolete("此方法在纯.NET环境中不可用，需要Unity物理系统支持")]
        public static object[] OverlapCircle2D(this AbilitySystemComponent asc, object offset, float radius,
            int layerMask, object relativeTransform = null)
        {
            throw new NotSupportedException("Unity物理系统在纯.NET环境中不可用。请使用第三方物理引擎或自定义实现。");
        }

        /// <summary>
        /// 2D圆形重叠检测（非分配版本，占位符实现）
        /// </summary>
        [Obsolete("此方法在纯.NET环境中不可用，需要Unity物理系统支持")]
        public static int OverlapCircle2DNonAlloc(this AbilitySystemComponent asc, object offset, float radius,
            object[] results, int layerMask, object relativeTransform = null)
        {
            throw new NotSupportedException("Unity物理系统在纯.NET环境中不可用。请使用第三方物理引擎或自定义实现。");
        }

        /// <summary>
        /// 时间轴技能的2D矩形重叠检测（占位符实现）
        /// </summary>
        [Obsolete("此方法在纯.NET环境中不可用，需要Unity物理系统支持")]
        public static int TimelineAbilityOverlapBox2D(this object spec,
            object offset, object size, float angle, int layerMask, object[] results,
            EffectCenterType centerType, object relativeTransform = null)
        {
            throw new NotSupportedException("Unity物理系统在纯.NET环境中不可用。请使用第三方物理引擎或自定义实现。");
        }
    }
}