using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS.Runtime
{
    /// <summary>
    /// 目标捕获器基类，定义技能目标获取的通用接口
    /// </summary>
    /// <remarks>
    /// TargetCatcherBase是GAS系统中目标选择机制的核心抽象类。
    /// 它提供了统一的目标获取接口，支持各种目标选择策略：
    /// 
    /// - 单体目标选择
    /// - 范围目标选择（圆形、矩形等）
    /// - 自身目标选择
    /// - 复杂的组合目标选择
    /// 
    /// 所有具体的目标捕获器都应继承此类并实现CatchTargetsNonAlloc方法。
    /// 为了避免GC压力，推荐使用NonAlloc版本的方法。
    /// </remarks>
    public abstract class TargetCatcherBase
    {
        /// <summary>
        /// 目标捕获器的拥有者组件
        /// 用于确定捕获的相对位置、朝向和过滤条件
        /// </summary>
        public AbilitySystemComponent Owner;

        /// <summary>
        /// 初始化目标捕获器的基础构造函数
        /// </summary>
        protected TargetCatcherBase()
        {
        }

        /// <summary>
        /// 初始化目标捕获器，设置拥有者
        /// </summary>
        /// <param name="owner">拥有此目标捕获器的AbilitySystemComponent</param>
        /// <remarks>
        /// 在使用目标捕获器之前必须调用此方法进行初始化。
        /// 拥有者组件提供了目标捕获的参考点和上下文信息。
        /// </remarks>
        public virtual void Init(AbilitySystemComponent owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// 捕获目标并返回结果列表（已过时，会产生GC）
        /// </summary>
        /// <param name="mainTarget">主要目标，可能为null</param>
        /// <returns>捕获到的目标列表</returns>
        /// <remarks>
        /// 此方法已标记为过时，因为它会创建新的List对象导致GC压力。
        /// 推荐使用CatchTargetsNonAlloc方法以获得更好的性能。
        /// </remarks>
        [Obsolete("请使用CatchTargetsNonAlloc方法来避免产生垃圾收集（GC）。")]
        public List<AbilitySystemComponent> CatchTargets(AbilitySystemComponent mainTarget)
        {
            var result = new List<AbilitySystemComponent>();

            CatchTargetsNonAlloc(mainTarget, result);

            return result;
        }

        /// <summary>
        /// 安全地捕获目标，使用预分配的列表避免GC
        /// </summary>
        /// <param name="mainTarget">主要目标，可能为null</param>
        /// <param name="results">用于存储结果的预分配列表</param>
        /// <remarks>
        /// 这是推荐的目标捕获方法，具有以下特点：
        /// - 自动清空结果列表
        /// - 无GC压力
        /// - 线程安全的Clear操作
        /// 
        /// 使用方式：
        /// <code>
        /// var targetList = new List&lt;AbilitySystemComponent&gt;();
        /// targetCatcher.CatchTargetsNonAllocSafe(mainTarget, targetList);
        /// // 使用targetList中的结果
        /// </code>
        /// </remarks>
        public void CatchTargetsNonAllocSafe(AbilitySystemComponent mainTarget, List<AbilitySystemComponent> results)
        {
            results.Clear();

            CatchTargetsNonAlloc(mainTarget, results);
        }

        /// <summary>
        /// 捕获目标的核心实现方法（抽象方法）
        /// </summary>
        /// <param name="mainTarget">主要目标，某些捕获器需要此参数作为参考点</param>
        /// <param name="results">用于存储捕获结果的列表</param>
        /// <remarks>
        /// 子类必须实现此方法来定义具体的目标捕获逻辑。
        /// 
        /// 实现要点：
        /// - 不要清空results列表，调用者负责清空
        /// - 只添加符合条件的有效目标
        /// - 考虑性能优化，避免不必要的计算
        /// - 处理边界情况，如目标为null或不存在
        /// 
        /// 常见实现模式：
        /// - 范围检测：使用物理查询（如OverlapSphere）
        /// - 单体目标：直接返回mainTarget
        /// - 自身目标：返回Owner
        /// - 复合条件：结合多种筛选条件
        /// </remarks>
        protected abstract void CatchTargetsNonAlloc(AbilitySystemComponent mainTarget, List<AbilitySystemComponent> results);

        /// <summary>
        /// 编辑器预览功能，用于可视化目标捕获范围
        /// </summary>
        /// <param name="obj">预览参考的游戏对象</param>
        /// <remarks>
        /// 仅在Unity编辑器中可用，用于：
        /// - 在Scene视图中绘制捕获范围
        /// - 调试目标选择逻辑
        /// - 可视化技能影响区域
        /// 
        /// 子类可重写此方法来提供自定义的可视化效果。
        /// </remarks>
#if UNITY_EDITOR
        public virtual void OnEditorPreview(GameObject obj)
        {
        }
#endif
    }
}