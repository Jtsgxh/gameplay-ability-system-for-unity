using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// 单体目标捕获器，返回指定的主要目标
    /// </summary>
    /// <remarks>
    /// CatchTarget是用于单体目标技能的捕获器实现，特点：
    /// 
    /// - 直接返回传入的mainTarget作为唯一目标
    /// - 适用于所有单体指向性技能
    /// - 不进行范围检测或多目标处理
    /// - 支持空目标处理（如果mainTarget为null）
    /// 
    /// 典型使用场景：
    /// - 单体攻击技能（如火球术、冰箭）
    /// - 单体治疗技能（如治疗术）
    /// - 单体debuff技能（如减速、毒素）
    /// - 定向技能效果应用
    /// 
    /// 注意：如果mainTarget为null，将添加null到结果列表中，
    /// 调用方需要处理这种情况。
    /// </remarks>
    public sealed class CatchTarget : TargetCatcherBase
    {
        /// <summary>
        /// 捕获目标实现：直接返回主要目标
        /// </summary>
        /// <param name="mainTarget">主要目标，将作为结果返回</param>
        /// <param name="results">结果列表，将添加mainTarget到此列表中</param>
        /// <remarks>
        /// 实现逻辑：
        /// 1. 直接将mainTarget添加到结果列表
        /// 2. 不进行任何条件检查或有效性验证
        /// 3. 不使用Owner信息（但Owner仍然可能被其他系统使用）
        /// 
        /// 空目标处理：
        /// - 如果mainTarget为null，将添加null到结果列表
        /// - 调用方应该检查并处理null目标的情况
        /// - 某些技能可能允许空目标（如范围技能的备用目标选择）
        /// 
        /// 性能特点：
        /// - 最轻量级的目标捕获器
        /// - 无计算开销
        /// - 无内存分配
        /// </remarks>
        protected override void CatchTargetsNonAlloc(AbilitySystemComponent mainTarget, List<AbilitySystemComponent> results)
        {
            results.Add(mainTarget);
        }
    }
}