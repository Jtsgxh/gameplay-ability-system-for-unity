namespace GAS.Runtime
{
    /// <summary>
    /// 冷却计时器结构体，用于表示技能或效果的冷却状态
    /// </summary>
    /// <remarks>
    /// CooldownTimer提供了简单而有效的冷却时间管理：
    /// 
    /// 主要用途：
    /// - 技能冷却状态查询
    /// - UI冷却显示
    /// - 冷却条件检查
    /// - 冷却时间计算
    /// 
    /// 使用场景：
    /// - 检查技能是否在冷却中
    /// - 显示冷却剩余时间
    /// - 计算冷却完成时间
    /// - 冷却相关的游戏逻辑判断
    /// 
    /// 时间单位：
    /// - 所有时间值都以秒为单位
    /// - TimeRemaining为0表示没有冷却
    /// - TimeRemaining为-1通常表示无限冷却（如被永久禁用）
    /// </remarks>
    public struct CooldownTimer
    {
        /// <summary>
        /// 冷却剩余时间（秒）
        /// </summary>
        /// <remarks>
        /// 表示距离冷却结束还需要多长时间：
        /// - 0：没有冷却，可以立即使用
        /// - 正值：剩余的冷却时间
        /// - -1：无限冷却（特殊情况，如被永久禁用）
        /// 
        /// 通常通过以下方式计算：
        /// TimeRemaining = Duration - (当前时间 - 冷却开始时间)
        /// </remarks>
        public float TimeRemaining;
        
        /// <summary>
        /// 冷却总持续时间（秒）
        /// </summary>
        /// <remarks>
        /// 表示完整的冷却周期长度：
        /// - 用于UI显示冷却进度
        /// - 计算冷却完成百分比
        /// - 提供冷却时间的上下文信息
        /// 
        /// 冷却完成百分比计算：
        /// Progress = 1.0f - (TimeRemaining / Duration)
        /// </remarks>
        public float Duration;
    }
}