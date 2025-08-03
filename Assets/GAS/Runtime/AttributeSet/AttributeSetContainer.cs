using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// 属性集容器，管理单个AbilitySystemComponent上的所有属性集和属性聚合器
    /// </summary>
    /// <remarks>
    /// AttributeSetContainer是EX-GAS属性系统的核心管理组件，负责：
    /// 
    /// 1. **属性集生命周期管理**：
    ///    - 动态添加和移除不同类型的属性集
    ///    - 确保属性集的正确初始化和清理
    ///    - 维护属性集的所有权关系
    /// 
    /// 2. **属性聚合器管理**：
    ///    - 为每个属性创建对应的AttributeAggregator
    ///    - 协调多个GameplayEffect对同一属性的修饰
    ///    - 处理属性间的依赖关系和更新传播
    /// 
    /// 3. **统一访问接口**：
    ///    - 提供跨属性集的属性查询和操作接口
    ///    - 支持基于字符串的动态属性访问
    ///    - 提供属性快照功能用于网络同步和存档
    /// 
    /// 4. **多属性集支持**：
    ///    - 同一个角色可以拥有多个不同的属性集
    ///    - 例如：CharacterAttributeSet（基础属性）+ CombatAttributeSet（战斗属性）
    ///    - 支持模块化的属性系统设计
    /// 
    /// 设计模式：
    /// - 容器模式：管理多个AttributeSet的集合
    /// - 工厂模式：创建和配置AttributeAggregator
    /// - 代理模式：为外部提供统一的属性访问接口
    /// 
    /// 性能特点：
    /// - 使用Dictionary进行快速属性集查找
    /// - 延迟创建AttributeAggregator，按需分配资源
    /// - 支持批量操作和事件管理优化
    /// </remarks>
    public class AttributeSetContainer
    {
        /// <summary>
        /// 容器的拥有者组件
        /// </summary>
        private readonly AbilitySystemComponent _owner;
        
        /// <summary>
        /// 容器是否已启用
        /// </summary>
        private bool _enabled = false;
        
        /// <summary>
        /// 属性集集合，以属性集类型名称为键
        /// </summary>
        private readonly Dictionary<string, AttributeSet> _attributeSets = new Dictionary<string, AttributeSet>();

        /// <summary>
        /// 属性聚合器集合，以AttributeBase实例为键
        /// </summary>
        /// <remarks>
        /// 每个AttributeBase实例对应一个AttributeAggregator，负责：
        /// - 收集影响该属性的所有GameplayEffect修饰符
        /// - 计算修饰符叠加后的最终属性值
        /// - 处理属性变化事件和依赖更新
        /// </remarks>
        private readonly Dictionary<AttributeBase, AttributeAggregator> _attributeAggregators =
            new Dictionary<AttributeBase, AttributeAggregator>();

        /// <summary>
        /// 获取所有属性集的只读视图
        /// </summary>
        /// <value>以属性集名称为键的属性集字典</value>
        /// <remarks>
        /// 直接暴露内部字典以支持高效的批量操作和迭代。
        /// 外部代码应该谨慎使用，避免直接修改字典内容。
        /// 
        /// 常见用途：
        /// - 遍历所有属性集进行批量操作
        /// - 编辑器工具的属性集检查和显示
        /// - 调试和诊断工具
        /// </remarks>
        public Dictionary<string, AttributeSet> Sets => _attributeSets;

        /// <summary>
        /// 初始化属性集容器
        /// </summary>
        /// <param name="owner">拥有此容器的AbilitySystemComponent</param>
        /// <exception cref="ArgumentNullException">当owner为null时抛出</exception>
        public AttributeSetContainer(AbilitySystemComponent owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// 添加指定类型的属性集（泛型版本）
        /// </summary>
        /// <typeparam name="T">属性集类型，必须继承自AttributeSet</typeparam>
        /// <remarks>
        /// 这是AddAttributeSet(Type)的泛型版本，提供编译时类型安全。
        /// 
        /// 使用示例：
        /// ```csharp
        /// // 为角色添加基础属性集
        /// container.AddAttributeSet<CharacterAttributeSet>();
        /// 
        /// // 为战斗单位添加战斗属性集
        /// container.AddAttributeSet<CombatAttributeSet>();
        /// ```
        /// 
        /// 特性：
        /// - 如果同类型的属性集已存在，操作会被忽略（不会重复添加）
        /// - 自动创建所有属性的AttributeAggregator
        /// - 如果容器已启用，会立即激活新的聚合器
        /// </remarks>
        public void AddAttributeSet<T>() where T : AttributeSet
        {
            AddAttributeSet(typeof(T));
        }

        /// <summary>
        /// 添加指定类型的属性集
        /// </summary>
        /// <param name="attrSetType">属性集类型，必须继承自AttributeSet</param>
        /// <exception cref="ArgumentNullException">当attrSetType为null时抛出</exception>
        /// <exception cref="ArgumentException">当attrSetType不是AttributeSet的子类时抛出</exception>
        /// <remarks>
        /// 添加属性集的完整流程：
        /// 
        /// 1. **重复检查**：检查同类型属性集是否已存在，避免重复添加
        /// 2. **实例创建**：使用Activator.CreateInstance创建属性集实例
        /// 3. **聚合器创建**：为属性集中的每个属性创建AttributeAggregator
        /// 4. **生命周期管理**：如果容器已启用，立即激活新聚合器
        /// 5. **所有权设置**：设置属性集的拥有者为当前AbilitySystemComponent
        /// 
        /// AttributeAggregator的作用：
        /// - 收集和管理影响特定属性的所有GameplayEffect修饰符
        /// - 根据属性的计算模式（叠加/最小值/最大值）计算最终值
        /// - 处理属性间的依赖关系和级联更新
        /// 
        /// 性能考虑：
        /// - 属性集的创建是一次性开销，运行时性能影响很小
        /// - AttributeAggregator使用事件驱动模式，只在必要时进行计算
        /// - 支持延迟激活，在容器启用时才开始监听事件
        /// 
        /// 使用场景：
        /// - 角色初始化时添加基础属性集
        /// - 职业系统中添加职业特定的属性集
        /// - 装备系统中添加临时属性集
        /// - 技能系统中添加技能相关的属性集
        /// </remarks>
        public void AddAttributeSet(Type attrSetType)
        {
            if (attrSetType == null)
                throw new ArgumentNullException(nameof(attrSetType));
                
            if (!typeof(AttributeSet).IsAssignableFrom(attrSetType))
                throw new ArgumentException($"Type {attrSetType.Name} must inherit from AttributeSet", nameof(attrSetType));
                
            if (TryGetAttributeSet(attrSetType, out _)) return;
            
            var setName = AttributeSetUtil.AttributeSetName(attrSetType);
            var attributeSetInstance = Activator.CreateInstance(attrSetType) as AttributeSet;
            _attributeSets.Add(setName, attributeSetInstance);

            var attrSet = _attributeSets[setName];
            foreach (var attr in attrSet.AttributeNames)
            {
                var attributeBase = attrSet[attr];
                if (attributeBase != null && !_attributeAggregators.ContainsKey(attributeBase))
                {
                    var attrAggt = new AttributeAggregator(attributeBase, _owner);
                    // 只有当容器已启用时，才启用新的属性聚合器
                    if (_enabled)
                    {
                        attrAggt.OnEnable();
                    }
                    _attributeAggregators.Add(attributeBase, attrAggt);
                }
            }

            attrSet.SetOwner(_owner);
        }

        /// <summary>
        /// 移除指定类型的属性集
        /// </summary>
        /// <typeparam name="T">要移除的属性集类型</typeparam>
        /// <remarks>
        /// ⚠️ **警告：谨慎使用此方法！**
        /// 
        /// 移除属性集可能导致的问题：
        /// - 正在生效的GameplayEffect可能失去目标属性，导致错误
        /// - 网络同步时可能出现属性不匹配的问题
        /// - 其他系统对属性的引用可能变为无效
        /// - 可能破坏游戏逻辑的完整性
        /// 
        /// 移除流程：
        /// 1. 查找并获取要移除的属性集
        /// 2. 禁用并移除所有属性的AttributeAggregator
        /// 3. 从容器中删除属性集实例
        /// 4. 清理相关的事件监听和引用
        /// 
        /// 建议的使用场景（限制性）：
        /// - 角色转职或重置时的完全重构
        /// - 游戏模式切换时的属性系统变更
        /// - 测试和调试环境中的动态配置
        /// - 确保没有正在生效的GameplayEffect依赖被移除的属性
        /// 
        /// 更安全的替代方案：
        /// - 使用GameplayTag禁用属性集的功能
        /// - 设置属性为0或默认值而不移除
        /// - 使用条件性的属性访问逻辑
        /// </remarks>
        public void RemoveAttributeSet<T>() where T : AttributeSet
        {
            var setName = AttributeSetUtil.AttributeSetName(typeof(T));
            if (!_attributeSets.TryGetValue(setName, out var attrSet)) 
                return;
                
            // 禁用并移除所有属性的聚合器
            foreach (var attr in attrSet.AttributeNames)
            {
                var attributeBase = attrSet[attr];
                if (attributeBase != null && _attributeAggregators.TryGetValue(attributeBase, out var aggregator))
                {
                    aggregator.OnDisable();
                    _attributeAggregators.Remove(attributeBase);
                }
            }

            _attributeSets.Remove(setName);
        }

        /// <summary>
        /// 尝试获取指定类型的属性集
        /// </summary>
        /// <typeparam name="T">属性集类型</typeparam>
        /// <param name="attributeSet">输出的属性集实例，如果不存在则为null</param>
        /// <returns>如果找到属性集返回true，否则返回false</returns>
        /// <remarks>
        /// 这是获取属性集的推荐方法，使用TryGet模式避免异常抛出。
        /// 
        /// 使用示例：
        /// ```csharp
        /// // 安全获取属性集
        /// if (container.TryGetAttributeSet<CharacterAttributeSet>(out var charAttrSet))
        /// {
        ///     var currentHealth = charAttrSet.Health.CurrentValue;
        ///     Debug.Log($"当前生命值: {currentHealth}");
        /// }
        /// else
        /// {
        ///     Debug.LogWarning("角色属性集未找到，可能尚未初始化");
        /// }
        /// ```
        /// 
        /// 性能特点：
        /// - O(1)时间复杂度的字典查找
        /// - 不会创建不必要的对象或抛出异常
        /// - 类型安全的强类型返回值
        /// 
        /// 使用场景：
        /// - 技能系统查询目标属性
        /// - UI系统获取显示数据
        /// - 游戏逻辑中的条件判断
        /// - 网络同步时的属性访问
        /// </remarks>
        public bool TryGetAttributeSet<T>(out T attributeSet) where T : AttributeSet
        {
            if (_attributeSets.TryGetValue(AttributeSetUtil.AttributeSetName(typeof(T)), out var set))
            {
                attributeSet = (T)set;
                return true;
            }

            attributeSet = null;
            return false;
        }

        /// <summary>
        /// 尝试获取指定类型的属性集（内部使用的非泛型版本）
        /// </summary>
        /// <param name="attrSetType">属性集类型</param>
        /// <param name="attributeSet">输出的属性集实例</param>
        /// <returns>如果找到属性集返回true，否则返回false</returns>
        private bool TryGetAttributeSet(Type attrSetType, out AttributeSet attributeSet)
        {
            if (_attributeSets.TryGetValue(AttributeSetUtil.AttributeSetName(attrSetType), out var set))
            {
                attributeSet = set;
                return true;
            }

            attributeSet = null;
            return false;
        }

        /// <summary>
        /// 获取指定属性的AttributeValue结构
        /// </summary>
        /// <param name="attrSetName">属性集名称</param>
        /// <param name="attrShortName">属性短名称</param>
        /// <returns>AttributeValue结构，如果属性不存在则返回null</returns>
        /// <remarks>
        /// 返回完整的AttributeValue结构，包含基础值、当前值、计算模式等所有信息。
        /// 用于需要访问属性完整信息的场景。
        /// </remarks>
        public AttributeValue? GetAttributeAttributeValue(string attrSetName, string attrShortName)
        {
            return _attributeSets.TryGetValue(attrSetName, out var set) && set[attrShortName] != null
                ? set[attrShortName].Value
                : (AttributeValue?)null;
        }

        /// <summary>
        /// 获取指定属性的计算模式
        /// </summary>
        /// <param name="attrSetName">属性集名称</param>
        /// <param name="attrShortName">属性短名称</param>
        /// <returns>计算模式，如果属性不存在则返回null</returns>
        /// <remarks>
        /// 用于GameplayEffect系统查询属性如何处理修饰符叠加。
        /// </remarks>
        public CalculateMode? GetAttributeCalculateMode(string attrSetName, string attrShortName)
        {
            return _attributeSets.TryGetValue(attrSetName, out var set) && set[attrShortName] != null
                ? set[attrShortName].CalculateMode
                : (CalculateMode?)null;
        }

        /// <summary>
        /// 获取指定属性的基础值
        /// </summary>
        /// <param name="attrSetName">属性集名称</param>
        /// <param name="attrShortName">属性短名称</param>
        /// <returns>基础值，如果属性不存在则返回null</returns>
        /// <remarks>
        /// 基础值是不受GameplayEffect修饰符影响的原始属性值。
        /// 常用于存档保存、网络同步的基础数据传输。
        /// </remarks>
        public float? GetAttributeBaseValue(string attrSetName, string attrShortName)
        {
            return _attributeSets.TryGetValue(attrSetName, out var set) && set[attrShortName] != null 
                ? set[attrShortName].BaseValue 
                : (float?)null;
        }

        /// <summary>
        /// 获取指定属性的当前值
        /// </summary>
        /// <param name="attrSetName">属性集名称</param>
        /// <param name="attrShortName">属性短名称</param>
        /// <returns>当前值，如果属性不存在则返回null</returns>
        /// <remarks>
        /// 当前值是经过所有生效的GameplayEffect修饰符计算后的最终值。
        /// 这是游戏逻辑中最常用的属性值访问方法。
        /// 
        /// 使用场景：
        /// - 伤害计算中获取攻击力
        /// - 移动系统中获取移动速度
        /// - UI显示中获取当前生命值
        /// - 技能释放条件检查
        /// </remarks>
        public float? GetAttributeCurrentValue(string attrSetName, string attrShortName)
        {
            return _attributeSets.TryGetValue(attrSetName, out var set) && set[attrShortName] != null
                ? set[attrShortName].CurrentValue
                : (float?)null;
        }

        /// <summary>
        /// 创建所有属性当前值的快照
        /// </summary>
        /// <returns>包含所有属性名称和当前值的字典</returns>
        /// <remarks>
        /// 快照功能的主要用途：
        /// 
        /// 1. **网络同步**：
        ///    - 将属性状态序列化发送给客户端
        ///    - 确保多人游戏中的状态一致性
        ///    - 减少网络带宽消耗（只传输变化的属性）
        /// 
        /// 2. **存档系统**：
        ///    - 保存角色的完整属性状态
        ///    - 支持游戏进度的保存和加载
        ///    - 便于数据格式的版本兼容
        /// 
        /// 3. **调试和分析**：
        ///    - 记录特定时刻的属性状态
        ///    - 分析属性变化趋势
        ///    - 问题排查和性能分析
        /// 
        /// 4. **回滚机制**：
        ///    - 支持游戏状态的回滚操作
        ///    - 实现"撤销"功能
        ///    - 处理网络延迟和预测错误
        /// 
        /// 快照格式：
        /// - 键：完全限定的属性名称（"AttributeSetName.AttributeName"）
        /// - 值：属性的当前值（考虑所有修饰符）
        /// 
        /// 性能考虑：
        /// - 快照创建是O(n)操作，n为属性总数
        /// - 返回新的字典实例，避免外部修改影响内部状态
        /// - 建议在合适的时机调用，避免频繁创建快照
        /// </remarks>
        public Dictionary<string, float> Snapshot()
        {
            var snapshot = new Dictionary<string, float>();
            foreach (var attributeSet in _attributeSets)
            {
                foreach (var name in attributeSet.Value.AttributeNames)
                {
                    var attr = attributeSet.Value[name];
                    if (attr != null)
                    {
                        snapshot.Add(attr.Name, attr.CurrentValue);
                    }
                }
            }

            return snapshot;
        }

        /// <summary>
        /// 禁用容器，停止所有属性聚合器的工作
        /// </summary>
        /// <remarks>
        /// 当AbilitySystemComponent被禁用时调用，确保：
        /// - 停止所有AttributeAggregator的事件监听
        /// - 暂停属性值的自动计算和更新
        /// - 释放不必要的计算资源
        /// - 避免在组件禁用状态下的错误操作
        /// 
        /// 禁用是可逆的，可以通过OnEnable()重新激活。
        /// </remarks>
        public void OnDisable()
        {
            if (!_enabled) return;
            
            _enabled = false;
            foreach (var aggregator in _attributeAggregators)
                aggregator.Value.OnDisable();
        }

        /// <summary>
        /// 启用容器，激活所有属性聚合器
        /// </summary>
        /// <remarks>
        /// 当AbilitySystemComponent被启用时调用，确保：
        /// - 启动所有AttributeAggregator的事件监听
        /// - 恢复属性值的自动计算和更新
        /// - 重新建立属性间的依赖关系
        /// - 触发必要的初始化计算
        /// 
        /// 启用后会立即刷新所有属性的当前值。
        /// </remarks>
        public void OnEnable()
        {
            if (_enabled) return;
            
            _enabled = true;
            foreach (var aggregator in _attributeAggregators)
                aggregator.Value.OnEnable();
        }
    }
}