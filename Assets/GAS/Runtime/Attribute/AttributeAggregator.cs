using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GAS.Runtime
{
    /// <summary>
    /// 属性聚合器，负责管理单个属性的修饰符聚合和数值计算
    /// </summary>
    /// <remarks>
    /// AttributeAggregator是属性系统的核心计算引擎，主要功能：
    /// - 收集和管理影响特定属性的所有GameplayEffect修饰符
    /// - 根据不同计算模式（叠加、最小值、最大值）计算最终属性值
    /// - 实时响应基础值变化、效果变化和依赖属性变化
    /// - 支持多种运算类型（加减乘除、覆盖）
    /// - 处理属性间的依赖关系和追踪机制
    /// 
    /// 性能特点：
    /// - 使用缓存机制避免重复计算
    /// - 事件驱动的响应式更新
    /// - 智能的依赖管理和监听
    /// </remarks>
    public class AttributeAggregator
    {
        #region Private Fields
        
        private readonly AttributeBase _processedAttribute;
        private readonly AbilitySystemComponent _owner;
        
        /// <summary>
        /// 修饰符缓存结构，包含效果实例和修饰符
        /// </summary>
        private readonly struct ModifierCacheEntry
        {
            public readonly GameplayEffectSpec effectSpec;
            public readonly GameplayEffectModifier modifier;
            
            public ModifierCacheEntry(GameplayEffectSpec spec, GameplayEffectModifier mod)
            {
                effectSpec = spec;
                modifier = mod;
            }
        }
        
        /// <summary>
        /// 修饰符缓存列表，顺序很重要因为修饰符按顺序执行
        /// </summary>
        /// <remarks>
        /// 使用自定义结构体代替Tuple以减少GC分配和提高性能
        /// </remarks>
        private readonly List<ModifierCacheEntry> _modifierCache = new List<ModifierCacheEntry>();
        
        /// <summary>
        /// 缓存当前计算的数值，避免重复计算
        /// </summary>
        private float _cachedValue;
        private bool _isDirty = true;
        
        #endregion

        #region Constructor and Lifecycle
        
        /// <summary>
        /// 初始化属性聚合器
        /// </summary>
        /// <param name="attribute">要处理的属性</param>
        /// <param name="owner">属性的拥有者组件</param>
        /// <exception cref="ArgumentNullException">当attribute或owner为null时抛出</exception>
        public AttributeAggregator(AttributeBase attribute, AbilitySystemComponent owner)
        {
            _processedAttribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            
            // 初始时标记为脏，确保首次计算
            _isDirty = true;
        }

        /// <summary>
        /// 启用聚合器，注册必要的事件监听
        /// </summary>
        /// <remarks>
        /// 注册以下事件监听：
        /// - 属性基础值变化事件
        /// - GameplayEffect容器变化事件
        /// - 标签变化事件（用于Source/Target标签检查）
        /// </remarks>
        public void OnEnable()
        {
            if (_processedAttribute == null || _owner?.GameplayEffectContainer == null)
                return;
                
            // 通过EventBus订阅属性基础值变化事件
            if (_owner?.EventBus != null)
            {
                _owner.EventBus.Subscribe(GameplayEvents.OnAttributePostChange, OnAttributeBaseValueChanged);
            }
            
            // 通过EventBus订阅容器变化事件
            if (_owner?.EventBus != null)
            {
                _owner.EventBus.Subscribe(GameplayEvents.OnGameplayEffectContainerChanged, OnGameplayEffectContainerChanged);
            }
            
            // 监听标签变化，当标签变化时重新刷新Modifier缓存
            // 因为Source/Target标签的变化可能会影响Modifier的生效状态
            if (_owner?.EventBus != null)
            {
                _owner.EventBus.Subscribe(GameplayEvents.OnTagCountChanged, OnTagChanged);
            }
            
            // 启用时刷新一次缓存
            RefreshModifierCache();
        }

        /// <summary>
        /// 禁用聚合器，注销所有事件监听
        /// </summary>
        /// <remarks>
        /// 清理所有注册的事件监听，防止内存泄漏
        /// </remarks>
        public void OnDisable()
        {
            // 通过EventBus取消订阅属性基础值变化事件
            if (_owner?.EventBus != null)
            {
                _owner.EventBus.Unsubscribe(GameplayEvents.OnAttributePostChange, OnAttributeBaseValueChanged);
            }
                
            // 通过EventBus取消订阅容器变化事件
            if (_owner?.EventBus != null)
            {
                _owner.EventBus.Unsubscribe(GameplayEvents.OnGameplayEffectContainerChanged, OnGameplayEffectContainerChanged);
            }
                
            // 通过EventBus取消订阅标签变化事件
            if (_owner?.EventBus != null)
            {
                _owner.EventBus.Unsubscribe(GameplayEvents.OnTagCountChanged, OnTagChanged);
            }
                
            // 清理依赖属性监听
            UnregisterAttributeChangedListen();
            _modifierCache.Clear();
        }
        
        #endregion

        #region Cache Management
        
        /// <summary>
        /// 刷新修饰符缓存
        /// </summary>
        /// <remarks>
        /// 当GameplayEffect容器发生变化时触发，重新收集所有影响当前属性的修饰符。
        /// 优化点：
        /// - 添加null检查和提前返回
        /// - 使用结构体代替Tuple减少GC
        /// - 添加性能监控采样
        /// </remarks>
        private void RefreshModifierCache()
        {
            Profiler.BeginSample($"{nameof(AttributeAggregator)}::RefreshModifierCache");
            
            try
            {
                // 清理旧的监听
                UnregisterAttributeChangedListen();
                _modifierCache.Clear();
                
                var gameplayEffects = _owner?.GameplayEffectContainer?.GameplayEffects();
                if (gameplayEffects == null || gameplayEffects.Count == 0)
                {
                    _isDirty = true;
                    UpdateCurrentValueWhenModifierIsDirty();
                    return;
                }
                
                var attributeName = _processedAttribute.Name;
                
                // 收集相关的修饰符
                for (int i = 0; i < gameplayEffects.Count; i++)
                {
                    var geSpec = gameplayEffects[i];
                    if (geSpec?.IsActive != true || geSpec.Modifiers == null)
                        continue;
                        
                    for (int j = 0; j < geSpec.Modifiers.Length; j++)
                    {
                        var modifier = geSpec.Modifiers[j];
                        if (string.Equals(modifier.AttributeName, attributeName, StringComparison.Ordinal))
                        {
                            // 检查标签要求，只有满足条件的修饰符才会被加入缓存
                            if (GameplayEffectModifier.ShouldApplyModifier(modifier, geSpec.Source, _owner))
                            {
                                _modifierCache.Add(new ModifierCacheEntry(geSpec, modifier));
                                TryRegisterAttributeChangedListen(geSpec, modifier);
                            }
                        }
                    }
                }
                
                _isDirty = true;
                UpdateCurrentValueWhenModifierIsDirty();
            }
            finally
            {
                Profiler.EndSample();
            }
        }
        
        #endregion

        #region Value Calculation
        
        /// <summary>
        /// 计算属性的新值
        /// </summary>
        /// <returns>计算后的新属性值</returns>
        /// <remarks>
        /// 计算触发时机：
        /// 1. 修饰符缓存变化时
        /// 2. 属性基础值变化时  
        /// 3. 依赖属性值变化时（AttributeBased类型的MMC）
        /// 
        /// 优化改进：
        /// - 添加缓存机制避免重复计算
        /// - 优化循环性能
        /// - 更好的错误处理和验证
        /// - 添加性能监控
        /// </remarks>
        private float CalculateNewValue()
        {
            if (!_isDirty)
                return _cachedValue;
                
            Profiler.BeginSample($"{nameof(AttributeAggregator)}::CalculateNewValue");
            
            try
            {
                var calculateMode = _processedAttribute.CalculateMode;
                var result = calculateMode switch
                {
                    CalculateMode.Stacking => CalculateStackingValue(),
                    CalculateMode.MinValueOnly => CalculateMinValue(),
                    CalculateMode.MaxValueOnly => CalculateMaxValue(),
                    _ => throw new ArgumentOutOfRangeException(nameof(calculateMode), calculateMode, "不支持的计算模式")
                };
                
                _cachedValue = result;
                _isDirty = false;
                return result;
            }
            finally
            {
                Profiler.EndSample();
            }
        }
        
        /// <summary>
        /// 计算叠加模式的属性值
        /// </summary>
        private float CalculateStackingValue()
        {
            var newValue = _processedAttribute.BaseValue;
            
            for (int i = 0; i < _modifierCache.Count; i++)
            {
                var entry = _modifierCache[i];
                var magnitude = entry.modifier.CalculateMagnitude(entry.effectSpec, entry.modifier.ModiferMagnitude);
                
                if (!_processedAttribute.IsSupportOperation(entry.modifier.Operation))
                {
                    Debug.LogError($"属性 {_processedAttribute.Name} 不支持操作 {entry.modifier.Operation}");
                    continue;
                }
                
                newValue = ApplyOperation(newValue, magnitude, entry.modifier.Operation);
            }
            
            return newValue;
        }
        
        /// <summary>
        /// 计算最小值模式的属性值
        /// </summary>
        private float CalculateMinValue()
        {
            if (_modifierCache.Count == 0)
                return _processedAttribute.BaseValue;
                
            var min = float.MaxValue;
            var hasValidOverride = false;
            
            for (int i = 0; i < _modifierCache.Count; i++)
            {
                var entry = _modifierCache[i];
                
                if (entry.modifier.Operation != GEOperation.Override)
                {
                    Debug.LogError($"MinValueOnly模式只支持Override操作，当前操作: {entry.modifier.Operation}");
                    continue;
                }
                
                if (!_processedAttribute.IsSupportOperation(entry.modifier.Operation))
                    continue;
                    
                var magnitude = entry.modifier.CalculateMagnitude(entry.effectSpec, entry.modifier.ModiferMagnitude);
                min = Mathf.Min(min, magnitude);
                hasValidOverride = true;
            }
            
            return hasValidOverride ? min : _processedAttribute.BaseValue;
        }
        
        /// <summary>
        /// 计算最大值模式的属性值
        /// </summary>
        private float CalculateMaxValue()
        {
            if (_modifierCache.Count == 0)
                return _processedAttribute.BaseValue;
                
            var max = float.MinValue;
            var hasValidOverride = false;
            
            for (int i = 0; i < _modifierCache.Count; i++)
            {
                var entry = _modifierCache[i];
                
                if (entry.modifier.Operation != GEOperation.Override)
                {
                    Debug.LogError($"MaxValueOnly模式只支持Override操作，当前操作: {entry.modifier.Operation}");
                    continue;
                }
                
                if (!_processedAttribute.IsSupportOperation(entry.modifier.Operation))
                    continue;
                    
                var magnitude = entry.modifier.CalculateMagnitude(entry.effectSpec, entry.modifier.ModiferMagnitude);
                max = Mathf.Max(max, magnitude);
                hasValidOverride = true;
            }
            
            return hasValidOverride ? max : _processedAttribute.BaseValue;
        }
        
        /// <summary>
        /// 应用数学运算
        /// </summary>
        private static float ApplyOperation(float currentValue, float magnitude, GEOperation operation)
        {
            return operation switch
            {
                GEOperation.Add => currentValue + magnitude,
                GEOperation.Minus => currentValue - magnitude,
                GEOperation.Multiply => currentValue * magnitude,
                GEOperation.Divide => magnitude != 0f ? currentValue / magnitude : currentValue,
                GEOperation.Override => magnitude,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "不支持的运算操作")
            };
        }
        
        #endregion

        #region Update Methods
        
        /// <summary>
        /// 当基础值发生变化时更新当前值
        /// </summary>
        /// <param name="attribute">发生变化的属性</param>
        /// <param name="oldBaseValue">旧基础值</param>
        /// <param name="newBaseValue">新基础值</param>
        private void UpdateCurrentValueWhenBaseValueIsDirty(AttributeBase attribute, float oldBaseValue, float newBaseValue)
        {
            if (Mathf.Approximately(oldBaseValue, newBaseValue))
                return;
                
            _isDirty = true;
            var newValue = CalculateNewValue();
            _processedAttribute.SetCurrentValue(newValue);
        }
        
        /// <summary>
        /// 当修饰符发生变化时更新当前值
        /// </summary>
        private void UpdateCurrentValueWhenModifierIsDirty()
        {
            _isDirty = true;
            var newValue = CalculateNewValue();
            _processedAttribute.SetCurrentValue(newValue);
        }
        
        #endregion

        #region Dependency Tracking
        
        /// <summary>
        /// 注销所有属性变化监听
        /// </summary>
        /// <remarks>
        /// 清理所有注册的依赖属性监听，防止内存泄漏
        /// </remarks>
        private void UnregisterAttributeChangedListen()
        {
            for (int i = 0; i < _modifierCache.Count; i++)
            {
                var entry = _modifierCache[i];
                TryUnregisterAttributeChangedListen(entry.effectSpec, entry.modifier);
            }
        }

        /// <summary>
        /// 尝试注销特定修饰符的属性变化监听
        /// </summary>
        /// <param name="ge">游戏效果实例</param>
        /// <param name="modifier">修饰符</param>
        private void TryUnregisterAttributeChangedListen(GameplayEffectSpec ge, GameplayEffectModifier modifier)
        {
            if (modifier.MMC == null || !(modifier.MMC is AttributeBasedModCalculation mmc) || 
                mmc.captureType != AttributeBasedModCalculation.GEAttributeCaptureType.Track)
                return;
                
            var targetComponent = mmc.attributeFromType == AttributeBasedModCalculation.AttributeFrom.Target 
                ? ge?.Owner 
                : ge?.Source;
                
            if (targetComponent?.AttributeSetContainer?.Sets == null)
                return;
                
            if (targetComponent.AttributeSetContainer.Sets.TryGetValue(mmc.attributeSetName, out var attributeSet) &&
                attributeSet != null && attributeSet.AttributeNames != null)
            {
                bool containsAttribute = false;
                foreach (var attrName in attributeSet.AttributeNames)
                {
                    if (string.Equals(attrName, mmc.attributeShortName, StringComparison.Ordinal))
                    {
                        containsAttribute = true;
                        break;
                    }
                }
                
                if (containsAttribute)
                {
                    // 通过EventBus取消订阅属性变化事件
                    if (targetComponent?.EventBus != null)
                    {
                        targetComponent.EventBus.Unsubscribe(GameplayEvents.OnAttributeChanged, OnAttributeChangedEventBus);
                    }
                }
            }
        }

        /// <summary>
        /// 尝试注册特定修饰符的属性变化监听
        /// </summary>
        /// <param name="ge">游戏效果实例</param>
        /// <param name="modifier">修饰符</param>
        private void TryRegisterAttributeChangedListen(GameplayEffectSpec ge, GameplayEffectModifier modifier)
        {
            if (modifier.MMC == null || !(modifier.MMC is AttributeBasedModCalculation mmc) || 
                mmc.captureType != AttributeBasedModCalculation.GEAttributeCaptureType.Track)
                return;
                
            var targetComponent = mmc.attributeFromType == AttributeBasedModCalculation.AttributeFrom.Target 
                ? ge?.Owner 
                : ge?.Source;
                
            if (targetComponent?.AttributeSetContainer?.Sets == null)
                return;
                
            if (targetComponent.AttributeSetContainer.Sets.TryGetValue(mmc.attributeSetName, out var attributeSet) &&
                attributeSet != null && attributeSet.AttributeNames != null)
            {
                bool containsAttribute = false;
                foreach (var attrName in attributeSet.AttributeNames)
                {
                    if (string.Equals(attrName, mmc.attributeShortName, StringComparison.Ordinal))
                    {
                        containsAttribute = true;
                        break;
                    }
                }
                
                if (containsAttribute)
                {
                    // 通过EventBus订阅属性变化事件
                    if (targetComponent?.EventBus != null)
                    {
                        targetComponent.EventBus.Subscribe(GameplayEvents.OnAttributeChanged, OnAttributeChangedEventBus);
                    }
                }
            }
        }

        /// <summary>
        /// 处理依赖属性变化事件 (EventBus版本)
        /// </summary>
        /// <param name="eventData">事件数据</param>
        private void OnAttributeChangedEventBus(GameplayEventData eventData)
        {
            if (_modifierCache.Count == 0 || eventData?.Parameters == null)
                return;
                
            var attributeName = eventData.Parameters.TryGetValue("attributeName", out var attrNameObj) ? attrNameObj as string : null;
            if (string.IsNullOrEmpty(attributeName))
                return;
                
            // 检查是否有修饰符依赖于这个属性
            for (int i = 0; i < _modifierCache.Count; i++)
            {
                var entry = _modifierCache[i];
                if (IsModifierDependentOnAttributeName(entry, attributeName))
                {
                    UpdateCurrentValueWhenModifierIsDirty();
                    break; // 只需要触发一次更新
                }
            }
        }
        
        /// <summary>
        /// 处理依赖属性变化事件 (旧版本兼容)
        /// </summary>
        /// <param name="attribute">发生变化的属性</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        private void OnAttributeChanged(AttributeBase attribute, float oldValue, float newValue)
        {
            if (_modifierCache.Count == 0 || attribute?.Name == null)
                return;
                
            // 检查是否有修饰符依赖于这个属性
            for (int i = 0; i < _modifierCache.Count; i++)
            {
                var entry = _modifierCache[i];
                if (IsModifierDependentOnAttribute(entry, attribute))
                {
                    UpdateCurrentValueWhenModifierIsDirty();
                    break; // 只需要触发一次更新
                }
            }
        }
        
        /// <summary>
        /// 检查修饰符是否依赖于指定属性名称
        /// </summary>
        /// <param name="entry">修饰符缓存项</param>
        /// <param name="attributeName">属性名称</param>
        /// <returns>是否依赖</returns>
        private static bool IsModifierDependentOnAttributeName(ModifierCacheEntry entry, string attributeName)
        {
            if (entry.modifier.MMC == null || !(entry.modifier.MMC is AttributeBasedModCalculation mmc) ||
                mmc.captureType != AttributeBasedModCalculation.GEAttributeCaptureType.Track ||
                !string.Equals(attributeName, mmc.attributeName, StringComparison.Ordinal))
                return false;
                
            return true;
        }
        
        /// <summary>
        /// 检查修饰符是否依赖于指定属性
        /// </summary>
        /// <param name="entry">修饰符缓存项</param>
        /// <param name="attribute">属性</param>
        /// <returns>是否依赖</returns>
        private static bool IsModifierDependentOnAttribute(ModifierCacheEntry entry, AttributeBase attribute)
        {
            if (entry.modifier.MMC == null || !(entry.modifier.MMC is AttributeBasedModCalculation mmc) ||
                mmc.captureType != AttributeBasedModCalculation.GEAttributeCaptureType.Track ||
                !string.Equals(attribute.Name, mmc.attributeName, StringComparison.Ordinal))
                return false;
                
            var expectedOwner = mmc.attributeFromType == AttributeBasedModCalculation.AttributeFrom.Target 
                ? entry.effectSpec?.Owner 
                : entry.effectSpec?.Source;
                
            return attribute.Owner == expectedOwner;
        }
        
        /// <summary>
        /// 处理GameplayEffect容器变化事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        private void OnGameplayEffectContainerChanged(GameplayEventData eventData)
        {
            RefreshModifierCache();
        }
        
        /// <summary>
        /// 处理属性基础值变化事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        private void OnAttributeBaseValueChanged(GameplayEventData eventData)
        {
            if (eventData?.Parameters == null)
                return;
                
            var attributeName = eventData.Parameters.TryGetValue("attributeName", out var attrNameObj) ? attrNameObj as string : null;
            if (string.Equals(attributeName, _processedAttribute.Name, StringComparison.Ordinal))
            {
                var oldValue = eventData.Parameters.TryGetValue("oldValue", out var oldValueObj) ? oldValueObj : null;
                var newValue = eventData.Parameters.TryGetValue("newValue", out var newValueObj) ? newValueObj : null;
                
                if (oldValue is float oldVal && newValue is float newVal)
                {
                    UpdateCurrentValueWhenBaseValueIsDirty(_processedAttribute, oldVal, newVal);
                }
            }
        }
        
        /// <summary>
        /// 处理标签变化事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        private void OnTagChanged(GameplayEventData eventData)
        {
            RefreshModifierCache();
        }
        
        #endregion
    }
}