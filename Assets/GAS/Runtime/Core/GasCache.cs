using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// GAS系统的缓存管理器
    /// 提供系统级别的缓存操作，用于优化性能和减少重复计算
    /// </summary>
    /// <remarks>
    /// 缓存管理器负责：
    /// - 缓存AttributeSet类型到名称的映射关系
    /// - 优化频繁使用的类型查找操作
    /// - 提供统一的缓存接口
    /// </remarks>
    public class GasCache
    {
        /// <summary>
        /// 缓存AttributeSet类型到名称的映射关系
        /// </summary>
        /// <param name="attrSetTypeToName">AttributeSet类型到名称的字典映射</param>
        /// <remarks>
        /// 此方法将AttributeSet的类型信息缓存到AttributeSetUtil中，
        /// 用于优化运行时的类型名称查找性能，避免重复的反射操作
        /// </remarks>
        public static void CacheAttributeSetName(Dictionary<Type, string> attrSetTypeToName)
        {
            AttributeSetUtil.Cache(attrSetTypeToName);
        }
    }
}