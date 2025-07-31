using System.Collections.Generic;
using GAS.General;
using UnityEngine;

namespace GAS.Runtime
{
    /// <summary>
    /// 2D圆形区域目标捕获器，在指定的圆形区域内检测目标
    /// </summary>
    /// <remarks>
    /// CatchAreaCircle2D实现了基于2D物理系统的圆形范围目标检测。
    /// 它支持多种中心点类型和灵活的配置选项。
    /// 
    /// 主要特性：
    /// - 使用Unity 2D物理系统进行检测
    /// - 支持多种圆心定位方式
    /// - 可配置的半径和偏移量
    /// - 高性能的NonAlloc检测
    /// - 编辑器可视化预览
    /// 
    /// 适用场景：
    /// - 范围攻击技能（如火球爆炸、冰霜新星）
    /// - 治疗光环效果
    /// - 范围buff/debuff应用
    /// - 区域控制技能
    /// 
    /// 性能优化：
    /// - 使用静态Collider2D数组避免GC
    /// - 限制最大检测数量为32个对象
    /// - LayerMask过滤减少不必要的检测
    /// </remarks>
    public sealed class CatchAreaCircle2D : CatchAreaBase
    {
        /// <summary>
        /// 圆形检测区域的半径
        /// </summary>
        /// <remarks>
        /// 半径决定了技能的影响范围：
        /// - 较小半径适用于精确的近距离技能
        /// - 较大半径适用于范围控制或支援技能
        /// - 可以通过技能等级或属性动态调整
        /// </remarks>
        public float radius;
        
        /// <summary>
        /// 相对于中心点的偏移量
        /// </summary>
        /// <remarks>
        /// 偏移量允许调整检测区域的实际位置：
        /// - Vector2.zero表示以中心点为圆心
        /// - 正值偏移可以调整攻击范围的位置
        /// - 配合不同的centerType实现灵活的定位
        /// </remarks>
        public Vector2 offset;
        
        /// <summary>
        /// 效果中心点类型，决定圆形区域的定位方式
        /// </summary>
        /// <remarks>
        /// 中心类型影响圆形检测区域的基准位置：
        /// - SelfOffset: 以施法者位置为基准，加上偏移量
        /// - WorldSpace: 使用世界坐标系中的绝对位置
        /// - TargetOffset: 以主要目标位置为基准，加上偏移量
        /// </remarks>
        public EffectCenterType centerType;

        /// <summary>
        /// 初始化圆形区域捕获器的完整配置
        /// </summary>
        /// <param name="owner">拥有者组件</param>
        /// <param name="tCheckLayer">检测层级掩码</param>
        /// <param name="offset">相对偏移量</param>
        /// <param name="radius">检测半径</param>
        /// <remarks>
        /// 这个重载方法提供了最完整的初始化选项，
        /// 适用于需要精确控制检测参数的场景。
        /// 
        /// 通常在技能初始化时调用，参数可能来自：
        /// - 技能配置文件
        /// - 技能等级计算
        /// - 运行时状态修改
        /// </remarks>
        public void Init(AbilitySystemComponent owner, LayerMask tCheckLayer, Vector2 offset, float radius)
        {
            base.Init(owner, tCheckLayer);
            this.offset = offset;
            this.radius = radius;
        }

        /// <summary>
        /// 静态Collider2D数组，用于NonAlloc检测避免GC
        /// </summary>
        /// <remarks>
        /// 静态数组的设计考虑：
        /// - 避免每次检测时创建新数组
        /// - 32个元素的容量适合大多数游戏场景
        /// - 如果需要检测更多目标，可以调整数组大小
        /// - 线程安全性：假设游戏逻辑在主线程执行
        /// </remarks>
        private static readonly Collider2D[] Collider2Ds = new Collider2D[32];
        
        /// <summary>
        /// 执行圆形区域目标检测的核心实现
        /// </summary>
        /// <param name="mainTarget">主要目标，用于TargetOffset中心类型</param>
        /// <param name="results">检测结果列表</param>
        /// <remarks>
        /// 检测流程：
        /// 1. 根据centerType确定检测中心点
        /// 2. 使用相应的物理检测方法
        /// 3. 遍历检测到的Collider2D
        /// 4. 过滤出包含AbilitySystemComponent的对象
        /// 5. 添加到结果列表
        /// 
        /// 中心点计算逻辑：
        /// - SelfOffset: 使用Owner的扩展方法，支持缩放和旋转
        /// - WorldSpace: 直接使用Physics2D的静态方法
        /// - TargetOffset: 使用mainTarget的扩展方法
        /// 
        /// 注意事项：
        /// - 如果centerType为TargetOffset但mainTarget为null，不会检测到任何目标
        /// - 检测结果不包含没有AbilitySystemComponent的对象
        /// - 最多返回32个目标（受静态数组大小限制）
        /// </remarks>
        protected override void CatchTargetsNonAlloc(AbilitySystemComponent mainTarget, List<AbilitySystemComponent> results)
        {
            int count = centerType switch
            {
                EffectCenterType.SelfOffset => Owner.OverlapCircle2DNonAlloc(offset, radius, Collider2Ds, checkLayer),
                EffectCenterType.WorldSpace => Physics2D.OverlapCircleNonAlloc(offset, radius, Collider2Ds, checkLayer),
                EffectCenterType.TargetOffset => mainTarget.OverlapCircle2DNonAlloc(offset, radius, Collider2Ds, checkLayer),
                _ => 0
            };

            for (var i = 0; i < count; ++i)
            {
                var targetUnit = Collider2Ds[i].GetComponent<AbilitySystemComponent>();
                if (targetUnit != null)
                {
                    results.Add(targetUnit);
                }
            }
        }
        
        /// <summary>
        /// 在Unity编辑器中绘制圆形检测区域的预览
        /// </summary>
        /// <param name="previewObject">用于预览的游戏对象</param>
        /// <remarks>
        /// 编辑器预览功能：
        /// - 在Scene视图中绘制绿色圆形轮廓
        /// - 根据centerType显示正确的位置
        /// - 考虑对象的缩放因子
        /// - 预览持续1秒钟
        /// 
        /// 中心点计算：
        /// - SelfOffset: 考虑previewObject的位置和缩放
        /// - WorldSpace: 使用offset的世界坐标
        /// - TargetOffset: 当前未实现（需要目标引用）
        /// 
        /// 调试用途：
        /// - 验证技能范围设置
        /// - 调整半径和偏移参数
        /// - 确认centerType的效果
        /// </remarks>
#if UNITY_EDITOR
        public override void OnEditorPreview(GameObject previewObject)
        {
            // 使用Debug绘制圆形预览
            float showTime = 1;
            Color color = Color.green;
            var relativeTransform = previewObject.transform;
            var center = offset;
            switch (centerType)
            {
                case EffectCenterType.SelfOffset:
                    center = relativeTransform.position;
                    center.y += relativeTransform.lossyScale.y > 0 ? offset.y : -offset.y;
                    center.x += relativeTransform.lossyScale.x > 0 ? offset.x : -offset.x;
                    break;
                case EffectCenterType.WorldSpace:
                    center = offset;
                    break;
                case EffectCenterType.TargetOffset:
                    // 注意：这里需要目标引用才能正确预览
                    // center = mainTarget.transform.position + offset;
                    break;
            }

            DebugExtension.DebugDrawCircle(center, radius, color, showTime);
        }
#endif
    }
}