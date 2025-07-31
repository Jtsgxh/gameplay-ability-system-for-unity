using System;
using GAS.Runtime;
using UnityEngine;

namespace GAS.Runtime
{
    /// <summary>
    /// 区域目标捕获器基类，为基于物理检测的范围目标捕获提供通用功能
    /// </summary>
    /// <remarks>
    /// CatchAreaBase扩展了TargetCatcherBase，添加了物理层检测功能。
    /// 它为所有基于区域的目标捕获器提供了统一的层级过滤机制。
    /// 
    /// 主要特性：
    /// - 支持Unity LayerMask进行目标过滤
    /// - 为子类提供物理检测的基础设施
    /// - 统一的区域检测初始化接口
    /// 
    /// 子类实现通常包括：
    /// - 圆形区域检测（CatchAreaCircle2D）
    /// - 矩形区域检测（CatchAreaBox2D）
    /// - 其他自定义几何形状检测
    /// 
    /// 使用LayerMask的好处：
    /// - 性能优化：只检测指定层级的对象
    /// - 逻辑分离：区分敌人、友军、环境等
    /// - 灵活配置：可在编辑器中轻松调整检测目标
    /// </remarks>
    [Serializable]
    public abstract class CatchAreaBase : TargetCatcherBase
    {
        /// <summary>
        /// 物理检测层级掩码，用于过滤检测目标
        /// </summary>
        /// <remarks>
        /// LayerMask确定了物理检测时会考虑哪些层级的对象：
        /// - 通常设置为包含敌人、友军或所有单位的层级
        /// - 可以排除环境、UI等不相关的层级
        /// - 提高物理查询的性能和准确性
        /// 
        /// 常见配置示例：
        /// - 攻击技能：只检测敌人层级
        /// - 治疗技能：只检测友军层级
        /// - 范围效果：检测所有单位层级
        /// </remarks>
        public LayerMask checkLayer;

        /// <summary>
        /// 初始化区域目标捕获器，设置拥有者和检测层级
        /// </summary>
        /// <param name="owner">拥有此捕获器的AbilitySystemComponent</param>
        /// <param name="checkLayer">用于物理检测的层级掩码</param>
        /// <remarks>
        /// 这个重载版本的Init方法专门为区域捕获器设计，
        /// 除了设置拥有者外，还配置了物理检测所需的层级信息。
        /// 
        /// 调用顺序：
        /// 1. 调用基类的Init方法设置Owner
        /// 2. 设置checkLayer用于后续的物理检测
        /// 
        /// 使用示例：
        /// <code>
        /// var areaCatcher = new CatchAreaCircle2D();
        /// areaCatcher.Init(ownerComponent, LayerMask.GetMask("Enemy"));
        /// </code>
        /// </remarks>
        public void Init(AbilitySystemComponent owner, LayerMask checkLayer) 
        {
            base.Init(owner);
            this.checkLayer = checkLayer;
        }
    }
}