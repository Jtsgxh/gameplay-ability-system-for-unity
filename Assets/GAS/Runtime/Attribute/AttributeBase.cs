using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GAS.Runtime
{
    /// <summary>
    /// 属性基类，代表GAS系统中的一个属性（如生命值、魔法值等）
    /// </summary>
    /// <remarks>
    /// AttributeBase是GAS属性系统的核心类，负责：
    /// - 属性值的存储和管理（基础值和当前值）
    /// - 属性值的范围限制（最小值和最大值）
    /// - 属性变化事件的管理和触发
    /// - 属性操作的支持性检查
    /// - 属性计算模式的管理
    /// 
    /// 属性的命名规则：
    /// - Name: 完整名称，格式为"属性集名.属性名"
    /// - SetName: 属性集名称
    /// - ShortName: 属性短名称
    /// 
    /// 事件系统：
    /// - Pre事件：在值变化前触发，可以修改或阻止变化
    /// - Post事件：在值变化后触发，用于响应和通知
    /// </remarks>
    public class AttributeBase
    {
        /// <summary>
        /// 属性的完整名称（属性集名.属性名）
        /// </summary>
        public readonly string Name;
        
        /// <summary>
        /// 属性所属的属性集名称
        /// </summary>
        public readonly string SetName;
        
        /// <summary>
        /// 属性的短名称（不包含属性集前缀）
        /// </summary>
        public readonly string ShortName;
        protected event Action<AttributeBase, float, float> _onPostCurrentValueChange;
        protected event Action<AttributeBase, float, float> _onPostBaseValueChange;
        protected event Action<AttributeBase, float> _onPreCurrentValueChange;
        protected event Func<AttributeBase, float, float> _onPreBaseValueChange;
        protected IEnumerable<Func<AttributeBase, float, float>> _preBaseValueChangeListeners;

        private AttributeValue _value;
        private AbilitySystemComponent _owner;
        /// <summary>
        /// 获取属性的拥有者组件
        /// </summary>
        public AbilitySystemComponent Owner => _owner;

        /// <summary>
        /// 初始化属性实例
        /// </summary>
        /// <param name="attrSetName">属性集名称</param>
        /// <param name="attrName">属性名称</param>
        /// <param name="value">初始值</param>
        /// <param name="calculateMode">计算模式</param>
        /// <param name="supportedOperation">支持的操作类型</param>
        /// <param name="minValue">最小值</param>
        /// <param name="maxValue">最大值</param>
        public AttributeBase(string attrSetName, string attrName, float value = 0,
            CalculateMode calculateMode = CalculateMode.Stacking,
            SupportedOperation supportedOperation = SupportedOperation.All,
            float minValue = float.MinValue, float maxValue = float.MaxValue)
        {
            SetName = attrSetName;
            Name = $"{attrSetName}.{attrName}";
            ShortName = attrName;
            _value = new AttributeValue(value, calculateMode, supportedOperation, minValue, maxValue);
        }


        /// <summary>
        /// 获取属性值对象
        /// </summary>
        public AttributeValue Value => _value;
        
        /// <summary>
        /// 获取属性的基础值
        /// </summary>
        /// <remarks>
        /// 基础值是属性的原始值，不受任何修饰符影响。
        /// </remarks>
        public float BaseValue => _value.BaseValue;
        
        /// <summary>
        /// 获取属性的当前值
        /// </summary>
        /// <remarks>
        /// 当前值是经过所有修饰符计算后的最终值。
        /// </remarks>
        public float CurrentValue => _value.CurrentValue;

        /// <summary>
        /// 获取属性的最小值
        /// </summary>
        public float MinValue => _value.MinValue;
        
        /// <summary>
        /// 获取属性的最大值
        /// </summary>
        public float MaxValue => _value.MaxValue;

        /// <summary>
        /// 获取属性的计算模式
        /// </summary>
        public CalculateMode CalculateMode => _value.CalculateMode;
        
        /// <summary>
        /// 获取属性支持的操作类型
        /// </summary>
        public SupportedOperation SupportedOperation => _value.SupportedOperation;

        /// <summary>
        /// 设置属性的拥有者组件
        /// </summary>
        /// <param name="owner">拥有者组件</param>
        public void SetOwner(AbilitySystemComponent owner)
        {
            _owner = owner;
        }

        public void SetMinValue(float min)
        {
            _value.SetMinValue(min);
        }

        public void SetMaxValue(float max)
        {
            _value.SetMaxValue(max);
        }

        public void SetMinMaxValue(float min, float max)
        {
            _value.SetMinValue(min);
            _value.SetMaxValue(max);
        }
        
        public bool IsSupportOperation(GEOperation operation)
        {
            return _value.IsSupportOperation(operation);
        }

        /// <summary>
        /// 设置属性的当前值
        /// </summary>
        /// <param name="value">新的当前值</param>
        /// <remarks>
        /// 设置流程：
        /// 1. 将值限制在最小值和最大值之间
        /// 2. 触发变化前事件
        /// 3. 更新属性值
        /// 4. 如果值发生变化，触发变化后事件
        /// </remarks>
        public void SetCurrentValue(float value)
        {
            value = Mathf.Clamp(value, _value.MinValue, _value.MaxValue);

            _onPreCurrentValueChange?.Invoke(this, value);

            var oldValue = CurrentValue;
            _value.SetCurrentValue(value);

            if (!Mathf.Approximately(oldValue, value)) _onPostCurrentValueChange?.Invoke(this, oldValue, value);
        }

        /// <summary>
        /// 设置属性的基础值
        /// </summary>
        /// <param name="value">新的基础值</param>
        /// <remarks>
        /// 设置流程：
        /// 1. 触发基础值变化前事件（可能修改值）
        /// 2. 更新基础值
        /// 3. 如果值发生变化，触发变化后事件
        /// 
        /// 基础值的变化会影响当前值的计算。
        /// </remarks>
        public void SetBaseValue(float value)
        {
            if (_onPreBaseValueChange != null)
            {
                value = InvokePreBaseValueChangeListeners(value);
            }

            var oldValue = _value.BaseValue;
            _value.SetBaseValue(value);

            if (!Mathf.Approximately(oldValue, value)) _onPostBaseValueChange?.Invoke(this, oldValue, value);
        }

        public void SetCurrentValueWithoutEvent(float value)
        {
            _value.SetCurrentValue(value);
        }

        public void SetBaseValueWithoutEvent(float value)
        {
            _value.SetBaseValue(value);
        }

        public void RegisterPreBaseValueChange(Func<AttributeBase, float, float> func)
        {
            _onPreBaseValueChange += func;
            _preBaseValueChangeListeners =
                _onPreBaseValueChange?.GetInvocationList().Cast<Func<AttributeBase, float, float>>();
        }

        public void RegisterPostBaseValueChange(Action<AttributeBase, float, float> action)
        {
            _onPostBaseValueChange += action;
        }

        public void RegisterPreCurrentValueChange(Action<AttributeBase, float> action)
        {
            _onPreCurrentValueChange += action;
        }

        public void RegisterPostCurrentValueChange(Action<AttributeBase, float, float> action)
        {
            _onPostCurrentValueChange += action;
        }

        public void UnregisterPreBaseValueChange(Func<AttributeBase, float, float> func)
        {
            _onPreBaseValueChange -= func;
            _preBaseValueChangeListeners =
                _onPreBaseValueChange?.GetInvocationList().Cast<Func<AttributeBase, float, float>>();
        }

        public void UnregisterPostBaseValueChange(Action<AttributeBase, float, float> action)
        {
            _onPostBaseValueChange -= action;
        }

        public void UnregisterPreCurrentValueChange(Action<AttributeBase, float> action)
        {
            _onPreCurrentValueChange -= action;
        }

        public void UnregisterPostCurrentValueChange(Action<AttributeBase, float, float> action)
        {
            _onPostCurrentValueChange -= action;
        }

        public virtual void Dispose()
        {
            _onPreBaseValueChange = null;
            _onPostBaseValueChange = null;
            _onPreCurrentValueChange = null;
            _onPostCurrentValueChange = null;
        }

        private float InvokePreBaseValueChangeListeners(float value)
        {
            if (_preBaseValueChangeListeners == null) return value;

            foreach (var t in _preBaseValueChangeListeners)
                value = t.Invoke(this, value);
            return value;
        }
    }
}