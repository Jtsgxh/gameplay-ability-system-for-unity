namespace GAS.Runtime
{
    /// <summary>
    /// 技能激活结果枚举
    /// </summary>
    public enum AbilityActivateResult
    {
        /// <summary>
        /// 激活成功
        /// </summary>
        Success,
        
        /// <summary>
        /// 失败：技能已激活
        /// </summary>
        FailHasActivated,
        
        /// <summary>
        /// 失败：不满足激活标签要求
        /// </summary>
        FailTagRequirement,
        
        /// <summary>
        /// 失败：不满足源（施法者）标签要求
        /// </summary>
        FailSourceTagRequirement,
        
        /// <summary>
        /// 失败：不满足目标标签要求
        /// </summary>
        FailTargetTagRequirement,
        
        /// <summary>
        /// 失败：资源不足
        /// </summary>
        FailCost,
        
        /// <summary>
        /// 失败：技能冷却中
        /// </summary>
        FailCooldown,
        
        /// <summary>
        /// 失败：其他原因
        /// </summary>
        FailOtherReason
    }
}