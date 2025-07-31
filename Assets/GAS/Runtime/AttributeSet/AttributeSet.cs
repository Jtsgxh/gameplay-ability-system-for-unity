namespace GAS.Runtime
{
    /// <summary>
    /// 属性集抽象基类，定义一组相关属性的集合和基本操作接口
    /// </summary>
    /// <remarks>
    /// AttributeSet是EX-GAS属性系统的核心组织单元，主要功能：
    /// 
    /// 1. **属性分组管理**：
    ///    - 将语义相关的属性组织在一起（如：战斗属性、移动属性、资源属性）
    ///    - 提供统一的访问接口和命名空间
    ///    - 支持运行时动态查询和操作
    /// 
    /// 2. **代码生成支持**：
    ///    - 配合编辑器工具自动生成具体的AttributeSet子类
    ///    - 避免手动编写重复的属性声明和访问代码
    ///    - 确保类型安全和编译时检查
    /// 
    /// 3. **所有权管理**：
    ///    - 每个AttributeSet实例都属于一个特定的AbilitySystemComponent
    ///    - 支持属性的生命周期管理和事件处理
    /// 
    /// 4. **游戏设计灵活性**：
    ///    - 不同角色类型可以拥有不同的AttributeSet组合
    ///    - 支持模块化的属性系统设计
    ///    - 便于平衡调整和功能扩展
    /// 
    /// 使用模式：
    /// ```csharp
    /// // 自动生成的AttributeSet子类
    /// public class CharacterAttributeSet : AttributeSet
    /// {
    ///     public AttributeBase Health { get; private set; }
    ///     public AttributeBase Mana { get; private set; }
    ///     // ... 其他属性
    /// }
    /// 
    /// // 在AbilitySystemComponent中使用
    /// var charAttrSet = new CharacterAttributeSet();
    /// abilitySystemComponent.AttributeSetContainer.AddAttributeSet(charAttrSet);
    /// ```
    /// 
    /// 设计原则：
    /// - 抽象基类提供通用接口，具体实现由子类完成
    /// - 通过索引器支持字符串键的属性访问
    /// - 保持轻量级，避免不必要的运行时开销
    /// </remarks>
    public abstract class AttributeSet
    {
        /// <summary>
        /// 属性集的拥有者组件
        /// </summary>
        /// <remarks>
        /// 指向拥有此属性集的AbilitySystemComponent实例。
        /// 用于：
        /// - 属性变化事件的上下文传递
        /// - 跨属性集的依赖查询
        /// - 生命周期管理和清理
        /// </remarks>
        protected AbilitySystemComponent _owner;
        
        /// <summary>
        /// 通过属性名称获取属性实例的索引器
        /// </summary>
        /// <param name="key">属性的短名称（不包含AttributeSet前缀）</param>
        /// <returns>对应的AttributeBase实例，如果不存在则返回null</returns>
        /// <remarks>
        /// 提供基于字符串键的属性访问方式，支持：
        /// - 运行时动态属性查询
        /// - GameplayEffect修饰符的目标属性定位
        /// - 编辑器工具的属性枚举和操作
        /// - 脚本化的属性访问
        /// 
        /// 性能考虑：
        /// - 具体实现应该优化查询性能（如使用Dictionary缓存）
        /// - 避免在热路径中频繁使用字符串查询
        /// - 推荐在性能敏感场景下缓存AttributeBase引用
        /// 
        /// 示例：
        /// ```csharp
        /// var healthAttr = characterAttributeSet["Health"];
        /// if (healthAttr != null)
        /// {
        ///     healthAttr.SetBaseValue(100f);
        /// }
        /// ```
        /// </remarks>
        public abstract AttributeBase this[string key] { get; }
        
        /// <summary>
        /// 获取此属性集中所有属性的名称数组
        /// </summary>
        /// <value>包含所有属性短名称的字符串数组</value>
        /// <remarks>
        /// 用于：
        /// - 编辑器工具中的属性列表显示
        /// - 运行时的属性枚举和验证
        /// - 调试和诊断工具
        /// - 属性集的完整性检查
        /// 
        /// 实现要求：
        /// - 返回的数组应该是只读的或防御性拷贝
        /// - 属性名称顺序应该保持一致
        /// - 包含所有可访问的属性名称
        /// 
        /// 示例：
        /// ```csharp
        /// foreach (string attrName in attributeSet.AttributeNames)
        /// {
        ///     var attr = attributeSet[attrName];
        ///     Debug.Log($"{attrName}: {attr.CurrentValue}");
        /// }
        /// ```
        /// </remarks>
        public abstract string[] AttributeNames { get; }
        
        /// <summary>
        /// 设置属性集的拥有者组件
        /// </summary>
        /// <param name="owner">拥有此属性集的AbilitySystemComponent实例</param>
        /// <remarks>
        /// 此方法在AttributeSet被添加到AttributeSetContainer时调用。
        /// 
        /// 子类实现时应该：
        /// 1. 保存owner引用
        /// 2. 为所有属性设置owner
        /// 3. 初始化属性的聚合器和事件系统
        /// 4. 执行必要的初始化逻辑
        /// 
        /// 生命周期：
        /// - 在AttributeSetContainer.AddAttributeSet()时调用
        /// - 确保在任何属性操作之前完成设置
        /// - 支持运行时的所有权转移（但不推荐）
        /// 
        /// 示例实现：
        /// ```csharp
        /// public override void SetOwner(AbilitySystemComponent owner)
        /// {
        ///     _owner = owner;
        ///     Health.SetOwner(owner);
        ///     Mana.SetOwner(owner);
        ///     // ... 为所有属性设置owner
        /// }
        /// ```
        /// </remarks>
        public abstract void SetOwner(AbilitySystemComponent owner);
        
        /// <summary>
        /// 修改指定属性的基础值
        /// </summary>
        /// <param name="attributeShortName">属性的短名称</param>
        /// <param name="value">新的基础值</param>
        /// <remarks>
        /// 这是修改属性基础值的便捷方法，等价于：
        /// ```csharp
        /// this[attributeShortName]?.SetBaseValue(value);
        /// ```
        /// 
        /// 基础值修改的影响：
        /// - 触发AttributeAggregator重新计算CurrentValue
        /// - 影响所有依赖此属性的AttributeBased修饰符
        /// - 触发属性变化事件链
        /// 
        /// 使用场景：
        /// - Instant类型的GameplayEffect永久修改属性
        /// - 角色升级、装备穿戴等游戏事件
        /// - 调试和测试工具
        /// - 存档数据的加载和恢复
        /// 
        /// 安全性：
        /// - 自动处理属性不存在的情况（null检查）
        /// - 不会抛出异常，失败时静默忽略
        /// - 建议在调用前验证属性名称的有效性
        /// 
        /// 性能提示：
        /// - 如果需要批量修改多个属性，考虑缓存AttributeBase引用
        /// - 避免在循环中频繁调用此方法
        /// </remarks>
        public void ChangeAttributeBase(string attributeShortName, float value)
        {
            if (this[attributeShortName] != null)
            {
                this[attributeShortName].SetBaseValue(value);
            }
        }
    }
}