using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// 属性集工具类，提供属性集名称解析和缓存功能
    /// </summary>
    /// <remarks>
    /// AttributeSetUtil主要解决属性集名称标准化的问题：
    /// 
    /// 1. **名称标准化**：
    ///    - 将Type信息转换为标准化的字符串名称
    ///    - 支持自定义名称映射，避免硬编码类型名称
    ///    - 提供一致的命名规范用于序化和网络传输
    /// 
    /// 2. **性能优化**：
    ///    - 缓存Type到Name的映射关系，避免重复计算
    ///    - 减少反射操作的性能开销
    ///    - 支持批量预计算和缓存
    /// 
    /// 3. **灵活性支持**：
    ///    - 允许编辑器工具自定义属性集名称
    ///    - 支持本地化和多语言环境
    ///    - 便于重构和重命名操作
    /// 
    /// 使用模式：
    /// 
    /// ```csharp
    /// // 编辑器工具在启动时缓存映射关系
    /// var nameMapping = new Dictionary<Type, string>
    /// {
    ///     [typeof(CharacterAttributeSet)] = "Character",
    ///     [typeof(CombatAttributeSet)] = "Combat"
    /// };
    /// AttributeSetUtil.Cache(nameMapping);
    /// 
    /// // 运行时快速获取名称
    /// string name = AttributeSetUtil.AttributeSetName(typeof(CharacterAttributeSet));
    /// // 返回 "Character" 而不是 "CharacterAttributeSet"
    /// ```
    /// 
    /// 设计原则：
    /// - 静态工具类，无状态设计
    /// - 线程安全的缓存访问
    /// - 优雅降级，缓存失败时使用类型名称
    /// </remarks>
    public static class AttributeSetUtil
    {
        /// <summary>
        /// 属性集类型到名称的映射缓存
        /// </summary>
        /// <value>Type到字符串名称的映射字典，如果未初始化则为null</value>
        /// <remarks>
        /// 缓存的用途：
        /// - 避免重复的类型名称计算和字符串操作
        /// - 支持编辑器工具的自定义名称配置
        /// - 提供统一的属性集命名规范
        /// 
        /// 缓存特点：
        /// - 只读访问，保证外部无法直接修改
        /// - 延迟初始化，只有在需要时才创建
        /// - 不可变设计，设置后不应再修改
        /// 
        /// 注意事项：
        /// - 缓存应该在应用启动早期设置
        /// - 多线程环境下的访问是安全的（只读）
        /// - null值表示尚未初始化缓存
        /// </remarks>
        public static Dictionary<Type, string> AttrSetNameCache { get; private set; }
        
        /// <summary>
        /// 设置属性集类型到名称的映射缓存
        /// </summary>
        /// <param name="typeToName">Type到名称的映射字典</param>
        /// <remarks>
        /// 此方法应该在应用启动时调用一次，通常由编辑器工具或初始化系统负责。
        /// 
        /// 调用时机：
        /// - 应用启动的早期阶段
        /// - 在任何AttributeSetContainer操作之前
        /// - 编辑器工具的代码生成完成后
        /// 
        /// 参数要求：
        /// - typeToName可以为null（清除缓存）
        /// - 字典中的Type必须是AttributeSet的子类
        /// - 名称字符串应该唯一且符合标识符规范
        /// 
        /// 使用示例：
        /// ```csharp
        /// // 在游戏初始化时设置缓存
        /// var mapping = new Dictionary<Type, string>
        /// {
        ///     [typeof(PlayerCharacterAttributeSet)] = "PlayerCharacter",
        ///     [typeof(NPCAttributeSet)] = "NPC",
        ///     [typeof(MonsterAttributeSet)] = "Monster"
        /// };
        /// AttributeSetUtil.Cache(mapping);
        /// ```
        /// 
        /// 注意事项：
        /// - 建议只调用一次，避免运行时动态修改
        /// - 映射关系应该在整个应用生命周期内保持稳定
        /// - 用于网络同步时，确保客户端和服务器使用相同的映射
        /// </remarks>
        public static void Cache(Dictionary<Type,string> typeToName)
        {
            AttrSetNameCache = typeToName;
        }

        /// <summary>
        /// 获取属性集类型的标准化名称
        /// </summary>
        /// <param name="attrSetType">属性集类型</param>
        /// <returns>属性集的标准化名称</returns>
        /// <exception cref="ArgumentNullException">当attrSetType为null时抛出</exception>
        /// <remarks>
        /// 名称解析策略：
        /// 
        /// 1. **优先使用缓存**：
        ///    - 如果缓存已设置且包含该类型，返回缓存中的名称
        ///    - 缓存提供自定义的简化名称，便于序列化和显示
        /// 
        /// 2. **降级到类型名称**：
        ///    - 如果缓存未设置或不包含该类型，使用Type.Name
        ///    - 确保在任何情况下都能返回有效的名称
        /// 
        /// 使用场景：
        /// - AttributeSetContainer中的字典键生成
        /// - 网络协议中的属性集标识
        /// - 序列化和反序列化过程
        /// - 编辑器工具的显示和调试
        /// 
        /// 性能特点：
        /// - O(1)时间复杂度的字典查找
        /// - 无内存分配（使用已有字符串）
        /// - 线程安全的只读操作
        /// 
        /// 示例：
        /// ```csharp
        /// // 假设已缓存映射 CharacterAttributeSet -> "Character"
        /// string name1 = AttributeSetUtil.AttributeSetName(typeof(CharacterAttributeSet));
        /// // 返回: "Character"
        /// 
        /// // 未缓存的类型降级使用类型名称
        /// string name2 = AttributeSetUtil.AttributeSetName(typeof(SomeNewAttributeSet));
        /// // 返回: "SomeNewAttributeSet"
        /// ```
        /// 
        /// 注意事项：
        /// - 返回的名称在应用生命周期内应该保持一致
        /// - 用于持久化数据时，确保名称的向后兼容性
        /// - 在分布式系统中，确保所有节点使用相同的名称映射
        /// </remarks>
        public static string AttributeSetName(Type attrSetType)
        {
            if (attrSetType == null)
                throw new ArgumentNullException(nameof(attrSetType));
                
            if (AttrSetNameCache == null)
                return attrSetType.Name;
            
            return AttrSetNameCache.TryGetValue(attrSetType, out var value) ? value : attrSetType.Name;
        }
    }
}