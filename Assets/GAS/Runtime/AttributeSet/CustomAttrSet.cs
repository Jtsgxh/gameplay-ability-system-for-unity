using System.Collections.Generic;
using System.Linq;

namespace GAS.Runtime
{
    /// <summary>
    /// 自定义属性集，支持运行时动态添加和移除属性
    /// </summary>
    /// <remarks>
    /// CustomAttrSet与自动生成的静态AttributeSet不同，提供了完全动态的属性管理：
    /// 
    /// 1. **动态属性管理**：
    ///    - 运行时添加和移除属性
    ///    - 无需预定义属性结构
    ///    - 支持数据驱动的属性配置
    /// 
    /// 2. **灵活的使用场景**：
    ///    - 模组系统：动态加载的属性
    ///    - 配置驱动：从配置文件读取属性定义
    ///    - 实验功能：快速原型开发
    ///    - 用户自定义：玩家自定义属性
    /// 
    /// 3. **与静态AttributeSet的对比**：
    ///    - 静态：编译时确定，性能更优，类型安全
    ///    - 动态：运行时确定，更灵活，配置驱动
    /// 
    /// 使用示例：
    /// ```csharp
    /// var customSet = new CustomAttrSet();
    /// 
    /// // 动态添加属性
    /// customSet.AddAttribute(new AttributeBase("CustomAttrSet", "DynamicHealth", 100f));
    /// customSet.AddAttribute(new AttributeBase("CustomAttrSet", "DynamicMana", 50f));
    /// 
    /// // 添加到ASC
    /// abilitySystemComponent.AttributeSetContainer.AddAttributeSet(customSet);
    /// ```
    /// 
    /// 注意事项：
    /// - 性能略低于静态AttributeSet（动态查找开销）
    /// - 需要小心管理属性的生命周期
    /// - 确保GameplayEffect引用的属性确实存在
    /// - 网络同步时需要额外处理动态属性
    /// </remarks>
    public class CustomAttrSet : AttributeSet
    {
        /// <summary>
        /// 内部属性存储字典
        /// </summary>
        private readonly Dictionary<string, AttributeBase> _attributes = new Dictionary<string, AttributeBase>();

        /// <summary>
        /// 添加属性到属性集
        /// </summary>
        /// <param name="attribute">要添加的属性实例</param>
        /// <remarks>
        /// 添加属性的注意事项：
        /// 
        /// 1. **重复检查**：如果同名属性已存在，操作会被忽略
        /// 2. **所有权设置**：如果属性集已有拥有者，会自动为新属性设置拥有者
        /// 3. **聚合器创建**：需要在AttributeSetContainer层面处理聚合器的创建
        /// 4. **线程安全**：此方法不是线程安全的，应在主线程调用
        /// 
        /// 使用场景：
        /// - 游戏启动时从配置加载属性
        /// - 角色获得新技能时添加相关属性
        /// - 装备穿戴时添加临时属性
        /// - 模组系统动态扩展属性
        /// 
        /// 示例：
        /// ```csharp
        /// // 创建新属性
        /// var newAttr = new AttributeBase("CustomAttrSet", "CustomProperty", 42f, 
        ///     CalculateMode.Stacking, SupportedOperation.All, 0f, 100f);
        /// 
        /// // 添加到属性集
        /// customAttrSet.AddAttribute(newAttr);
        /// ```
        /// </remarks>
        public void AddAttribute(AttributeBase attribute)
        {
            if (attribute == null || _attributes.ContainsKey(attribute.Name))
                return;
                
            _attributes.Add(attribute.Name, attribute);
            
            // 如果属性集已有拥有者，为新属性设置拥有者
            if (_owner != null)
                attribute.SetOwner(_owner);
        }
        
        /// <summary>
        /// 从属性集中移除属性
        /// </summary>
        /// <param name="attribute">要移除的属性实例</param>
        /// <remarks>
        /// ⚠️ **警告：谨慎使用此方法！**
        /// 
        /// 移除属性可能导致的问题：
        /// - 正在生效的GameplayEffect失去目标属性
        /// - 其他系统的属性引用变为无效
        /// - AttributeAggregator可能出现异常
        /// - 网络同步问题
        /// 
        /// 安全的移除流程：
        /// 1. 确认没有GameplayEffect正在影响此属性
        /// 2. 清理所有对此属性的外部引用
        /// 3. 通知相关系统属性即将移除
        /// 4. 执行移除操作
        /// 5. 更新相关的缓存和索引
        /// 
        /// 建议的替代方案：
        /// - 将属性值设为0或禁用状态
        /// - 使用GameplayTag控制属性的可见性
        /// - 标记属性为"已删除"但保留在集合中
        /// </remarks>
        public void RemoveAttribute(AttributeBase attribute)
        {
            if (attribute != null)
                _attributes.Remove(attribute.Name);
        }

        /// <summary>
        /// 通过属性名称获取属性实例的索引器
        /// </summary>
        /// <param name="key">属性名称</param>
        /// <returns>对应的AttributeBase实例，如果不存在则返回null</returns>
        /// <remarks>
        /// 实现AttributeSet的抽象索引器，支持基于字符串的属性访问。
        /// 
        /// 性能特点：
        /// - O(1)时间复杂度的字典查找
        /// - 线程安全的只读操作
        /// - 自动处理不存在的键（返回null而不抛出异常）
        /// 
        /// 使用示例：
        /// ```csharp
        /// var healthAttr = customAttrSet["DynamicHealth"];
        /// if (healthAttr != null)
        /// {
        ///     float currentHealth = healthAttr.CurrentValue;
        /// }
        /// ```
        /// </remarks>
        public override AttributeBase this[string key] =>
            _attributes.TryGetValue(key, out var attribute) ? attribute : null;

        /// <summary>
        /// 获取所有属性名称的数组
        /// </summary>
        /// <value>包含所有当前属性名称的字符串数组</value>
        /// <remarks>
        /// 动态计算属性名称列表，确保返回最新的属性状态。
        /// 
        /// 实现特点：
        /// - 每次调用都重新计算（因为属性可能动态变化）
        /// - 使用LINQ的ToArray()确保返回独立的数组副本
        /// - 数组顺序可能因字典实现而变化
        /// 
        /// 性能考虑：
        /// - O(n)时间复杂度，n为属性数量
        /// - 每次调用都创建新数组
        /// - 建议在需要时缓存结果，避免频繁调用
        /// 
        /// 使用场景：
        /// - 编辑器工具显示属性列表
        /// - 序列化时枚举所有属性
        /// - 调试和诊断工具
        /// </remarks>
        public override string[] AttributeNames => _attributes.Keys.ToArray();
        
        /// <summary>
        /// 设置属性集的拥有者组件
        /// </summary>
        /// <param name="owner">拥有此属性集的AbilitySystemComponent实例</param>
        /// <remarks>
        /// 设置拥有者的完整流程：
        /// 
        /// 1. **保存拥有者引用**：存储到基类的_owner字段
        /// 2. **传播所有权**：为所有现有属性设置相同的拥有者
        /// 3. **初始化属性**：确保每个属性都正确初始化
        /// 4. **建立关联**：在属性和ASC之间建立双向关联
        /// 
        /// 所有权的重要性：
        /// - 属性需要拥有者来创建AttributeAggregator
        /// - 事件系统需要明确的所有权关系
        /// - 网络同步依赖正确的组件关联
        /// - 生命周期管理需要清晰的拥有关系
        /// 
        /// 调用时机：
        /// - AttributeSetContainer.AddAttributeSet()时自动调用
        /// - 确保在任何属性操作之前完成
        /// - 支持运行时的所有权转移（但不推荐）
        /// 
        /// 实现要求：
        /// - 必须为所有现有属性设置拥有者
        /// - 必须为后续添加的属性设置拥有者
        /// - 处理拥有者为null的情况
        /// </remarks>
        public override void SetOwner(AbilitySystemComponent owner)
        {
            _owner = owner;
            
            // 为所有现有属性设置拥有者
            foreach (var attribute in _attributes.Values)
            {
                attribute?.SetOwner(owner);
            }
        }
    }
}