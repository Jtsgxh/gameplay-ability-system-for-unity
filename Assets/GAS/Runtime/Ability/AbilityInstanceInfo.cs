using System;

namespace GAS.Runtime
{
    /// <summary>
    /// 技能实例信息结构体
    /// 用于存储技能资产和其对应的运行时类型信息
    /// </summary>
    /// <remarks>
    /// 此结构体主要用于：
    /// - 技能系统的初始化和配置
    /// - 关联技能资产数据与具体的技能实现类型
    /// - 支持技能的反射创建机制
    /// </remarks>
    public struct AbilityInstanceInfo
    {
        /// <summary>
        /// 技能资产引用
        /// 包含技能的配置数据和参数设定
        /// </summary>
        public AbilityAsset abilityAsset;
        
        /// <summary>
        /// 技能的运行时类型信息
        /// 用于通过反射创建具体的技能实例
        /// </summary>
        public Type abilityType;
    }
}