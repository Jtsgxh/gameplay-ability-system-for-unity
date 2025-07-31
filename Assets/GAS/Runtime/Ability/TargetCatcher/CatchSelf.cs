using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// 自身目标捕获器，始终返回技能拥有者自身作为目标
    /// </summary>
    /// <remarks>
    /// CatchSelf是最简单的目标捕获器实现，用于以下场景：
    /// 
    /// - 自我增益技能（如治疗自己、给自己加buff）
    /// - 自我状态技能（如变身、隐身）
    /// - 以自身为中心的范围技能的起始点
    /// - 被动技能的触发点
    /// 
    /// 此捕获器忽略mainTarget参数，始终返回Owner作为唯一目标。
    /// 
    /// 典型使用场景：
    /// - 治疗药水：对自己施加治疗效果
    /// - 防御姿态：给自己添加防御增益
    /// - 魔法护盾：在自身周围创建护盾
    /// </remarks>
    public sealed class CatchSelf : TargetCatcherBase
    {
        /// <summary>
        /// 捕获目标实现：始终返回拥有者自身
        /// </summary>
        /// <param name="mainTarget">主要目标（此捕获器中被忽略）</param>
        /// <param name="results">结果列表，将添加拥有者到此列表中</param>
        /// <remarks>
        /// 实现逻辑：
        /// 1. 直接将Owner添加到结果列表
        /// 2. 忽略mainTarget参数
        /// 3. 不进行任何条件检查或过滤
        /// 
        /// 注意：此方法假设Owner已经通过Init方法正确设置。
        /// 如果Owner为null，将导致添加null到结果列表中。
        /// </remarks>
        protected override void CatchTargetsNonAlloc(AbilitySystemComponent mainTarget, List<AbilitySystemComponent> results)
        {
            results.Add(Owner);
        }
    }
}