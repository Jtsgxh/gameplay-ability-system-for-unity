using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// 属性集容器，管理一个组件上的所有属性集
    /// </summary>
    /// <remarks>
    /// 这个容器负责：
    /// - 属性集的添加和移除
    /// - 属性值的计算和聚合（通过AttributeAggregator）
    /// - 属性值的查询和修改
    /// - 属性事件的管理和转发
    /// 
    /// 支持多种属性集同时存在，例如生命值属性集和战斗属性集。
    /// </remarks>
    public class AttributeSetContainer
    {
        private readonly AbilitySystemComponent _owner;
        private readonly Dictionary<string, AttributeSet> _attributeSets = new Dictionary<string, AttributeSet>();

        private readonly Dictionary<AttributeBase, AttributeAggregator> _attributeAggregators =
            new Dictionary<AttributeBase, AttributeAggregator>();

        public Dictionary<string, AttributeSet> Sets => _attributeSets;

        public AttributeSetContainer(AbilitySystemComponent owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 添加指定类型的属性集（泛型版本）
        /// </summary>
        /// <typeparam name="T">属性集类型</typeparam>
        /// <remarks>
        /// 这是AddAttributeSet(Type)的泛型版本，使用更加方便。
        /// 如果同类型的属性集已存在，此操作会被忽略。
        /// </remarks>
        /// <example>
        /// // 添加生命值属性集
        /// container.AddAttributeSet<HealthAttributeSet>();
        /// </example>
        public void AddAttributeSet<T>() where T : AttributeSet
        {
            AddAttributeSet(typeof(T));
        }

        /// <summary>
        /// 添加指定类型的属性集
        /// </summary>
        /// <param name="attrSetType">属性集类型</param>
        /// <remarks>
        /// 添加属性集的流程：
        /// 1. 检查同类型属性集是否已存在
        /// 2. 创建属性集实例
        /// 3. 为属性集中的每个属性创建聚合器（AttributeAggregator）
        /// 4. 设置属性集的拥有者
        /// 
        /// 属性聚合器负责处理来自不同源的属性修饰符。
        /// </remarks>
        /// <example>
        /// // 添加战斗属性集
        /// container.AddAttributeSet(typeof(CombatAttributeSet));
        /// </example>
        public void AddAttributeSet(Type attrSetType)
        {
            if (TryGetAttributeSet(attrSetType, out _)) return;
            var setName = AttributeSetUtil.AttributeSetName(attrSetType);
            _attributeSets.Add(setName, Activator.CreateInstance(attrSetType) as AttributeSet);

            var attrSet = _attributeSets[setName];
            foreach (var attr in attrSet.AttributeNames)
            {
                if (!_attributeAggregators.ContainsKey(attrSet[attr]))
                {
                    var attrAggt = new AttributeAggregator(attrSet[attr], _owner);
                    if (_owner.enabled) attrAggt.OnEnable();
                    _attributeAggregators.Add(attrSet[attr], attrAggt);
                }
            }

            attrSet.SetOwner(_owner);
        }

        /// <summary>
        /// 移除指定类型的属性集
        /// </summary>
        /// <typeparam name="T">属性集类型</typeparam>
        /// <remarks>
        /// 警告：谨慎使用此方法，可能导致意外错误（特别是在使用网络同步时）。
        /// 
        /// 移除属性集会：
        /// 1. 移除所有属性的聚合器
        /// 2. 从容器中删除属性集
        /// 3. 可能导致与此属性集相关的GameplayEffect失效
        /// 
        /// 建议仅在特殊情况下使用，比如角色转职或游戏模式切换。
        /// </remarks>
        /// <example>
        /// // 移除临时属性集（警告：谨慎使用）
        /// container.RemoveAttributeSet<TemporaryAttributeSet>();
        /// </example>
        public void RemoveAttributeSet<T>() where T : AttributeSet
        {
            var setName = AttributeSetUtil.AttributeSetName(typeof(T));
            var attrSet = _attributeSets[setName];
            foreach (var attr in attrSet.AttributeNames)
            {
                _attributeAggregators.Remove(attrSet[attr]);
            }

            _attributeSets.Remove(setName);
        }

        /// <summary>
        /// 尝试获取指定类型的属性集
        /// </summary>
        /// <typeparam name="T">属性集类型</typeparam>
        /// <param name="attributeSet">输出的属性集实例</param>
        /// <returns>如果找到属性集则返回true</returns>
        /// <remarks>
        /// 这是获取属性集的推荐方法，使用TryGet模式避免异常。
        /// 如果属性集不存在，attributeSet将为null。
        /// </remarks>
        /// <example>
        /// // 安全获取属性集
        /// if (container.TryGetAttributeSet<HealthAttributeSet>(out var healthSet))
        /// {
        ///     var currentHealth = healthSet.CurrentHealth.Value.CurrentValue;
        ///     Debug.Log($"当前生命值: {currentHealth}");
        /// }
        /// </example>
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

        bool TryGetAttributeSet(Type attrSetType, out AttributeSet attributeSet)
        {
            if (_attributeSets.TryGetValue(AttributeSetUtil.AttributeSetName(attrSetType), out var set))
            {
                attributeSet = set;
                return true;
            }

            attributeSet = null;
            return false;
        }

        public AttributeValue? GetAttributeAttributeValue(string attrSetName, string attrShortName)
        {
            return _attributeSets.TryGetValue(attrSetName, out var set)
                ? set[attrShortName].Value
                : (AttributeValue?)null;
        }

        public CalculateMode? GetAttributeCalculateMode(string attrSetName, string attrShortName)
        {
            return _attributeSets.TryGetValue(attrSetName, out var set)
                ? set[attrShortName].CalculateMode
                : (CalculateMode?)null;
        }

        public float? GetAttributeBaseValue(string attrSetName, string attrShortName)
        {
            return _attributeSets.TryGetValue(attrSetName, out var set) ? set[attrShortName].BaseValue : (float?)null;
        }

        public float? GetAttributeCurrentValue(string attrSetName, string attrShortName)
        {
            return _attributeSets.TryGetValue(attrSetName, out var set)
                ? set[attrShortName].CurrentValue
                : (float?)null;
        }

        public Dictionary<string, float> Snapshot()
        {
            Dictionary<string, float> snapshot = new Dictionary<string, float>();
            foreach (var attributeSet in _attributeSets)
            {
                foreach (var name in attributeSet.Value.AttributeNames)
                {
                    var attr = attributeSet.Value[name];
                    snapshot.Add(attr.Name, attr.CurrentValue);
                }
            }

            return snapshot;
        }

        public void OnDisable()
        {
            foreach (var aggregator in _attributeAggregators)
                aggregator.Value.OnDisable();
        }

        public void OnEnable()
        {
            foreach (var aggregator in _attributeAggregators)
                aggregator.Value.OnEnable();
        }
    }
}