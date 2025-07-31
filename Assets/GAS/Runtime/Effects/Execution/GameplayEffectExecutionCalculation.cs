using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// 游戏效果自定义执行参数，提供对源和目标属性的访问
    /// 对应UE中的FGameplayEffectCustomExecutionParameters
    /// </summary>
    public class GameplayEffectCustomExecutionParameters
    {
        public AbilitySystemComponent Source { get; }
        public AbilitySystemComponent Target { get; }
        public GameplayEffectSpec EffectSpec { get; }
        public float Level { get; }
        
        /// <summary>
        /// 捕获的属性快照
        /// </summary>
        private readonly Dictionary<string, float> _capturedSourceAttributes = new();
        private readonly Dictionary<string, float> _capturedTargetAttributes = new();
        
        public GameplayEffectCustomExecutionParameters(AbilitySystemComponent source, AbilitySystemComponent target, 
            GameplayEffectSpec effectSpec, float level)
        {
            Source = source;
            Target = target;
            EffectSpec = effectSpec;
            Level = level;
        }
        
        /// <summary>
        /// 获取源的属性值
        /// </summary>
        public float GetSourceAttribute(string attributeSetName, string attributeName)
        {
            var key = $"{attributeSetName}.{attributeName}";
            if (_capturedSourceAttributes.TryGetValue(key, out var value))
                return value;
                
            value = Source.GetAttributeCurrentValue(attributeSetName, attributeName) ?? 0f;
            _capturedSourceAttributes[key] = value;
            return value;
        }
        
        /// <summary>
        /// 获取目标的属性值
        /// </summary>
        public float GetTargetAttribute(string attributeSetName, string attributeName)
        {
            var key = $"{attributeSetName}.{attributeName}";
            if (_capturedTargetAttributes.TryGetValue(key, out var value))
                return value;
                
            value = Target.GetAttributeCurrentValue(attributeSetName, attributeName) ?? 0f;
            _capturedTargetAttributes[key] = value;
            return value;
        }
        
        /// <summary>
        /// 获取效果规格中的SetByCaller值
        /// </summary>
        public float GetSetByCallerMagnitude(GameplayTag tag)
        {
            return EffectSpec.GetSetByCallerMagnitude(tag);
        }
        
        public float GetSetByCallerMagnitude(string name)
        {
            return EffectSpec.GetSetByCallerMagnitude(name);
        }
    }
    
    /// <summary>
    /// 游戏效果自定义执行输出，包含要应用的修改
    /// 对应UE中的FGameplayEffectCustomExecutionOutput
    /// </summary>
    public class GameplayEffectCustomExecutionOutput
    {
        /// <summary>
        /// 要修改的属性列表
        /// </summary>
        public List<AttributeModification> Modifications { get; } = new();
        
        /// <summary>
        /// 添加属性修改
        /// </summary>
        public void AddModification(string attributeSetName, string attributeName, 
            GEOperation operation, float magnitude)
        {
            Modifications.Add(new AttributeModification
            {
                AttributeSetName = attributeSetName,
                AttributeName = attributeName,
                Operation = operation,
                Magnitude = magnitude
            });
        }
        
        public struct AttributeModification
        {
            public string AttributeSetName;
            public string AttributeName;
            public GEOperation Operation;
            public float Magnitude;
        }
    }
    
    /// <summary>
    /// 游戏效果执行计算基类
    /// 对应UE中的UGameplayEffectExecutionCalculation
    /// </summary>
    /// <remarks>
    /// GameplayEffectExecutionCalculation 用于实现复杂的游戏效果计算逻辑，
    /// 不同于简单的 Modifier，它可以：
    /// - 访问多个源和目标属性
    /// - 执行条件判断和复杂公式
    /// - 根据游戏状态动态计算结果
    /// - 一次性应用多个属性修改
    /// </remarks>
    public abstract class GameplayEffectExecutionCalculation
    {
        /// <summary>
        /// 创建执行计算实例
        /// </summary>
        public virtual GameplayEffectExecutionCalculation CreateSpec(AbilitySystemComponent source, 
            AbilitySystemComponent target)
        {
            return Activator.CreateInstance(GetType()) as GameplayEffectExecutionCalculation;
        }
        
        /// <summary>
        /// 执行计算
        /// </summary>
        /// <param name="context">执行上下文，包含源、目标和效果信息</param>
        /// <param name="output">执行输出，用于返回计算结果</param>
        public abstract void Execute(GameplayEffectCustomExecutionParameters executionParams, GameplayEffectCustomExecutionOutput executionOutput);
        
        /// <summary>
        /// 应用执行结果到目标
        /// </summary>
        internal void ApplyExecutionToTarget(GameplayEffectCustomExecutionParameters executionParams, GameplayEffectCustomExecutionOutput executionOutput)
        {
            foreach (var mod in executionOutput.Modifications)
            {
                var currentValue = executionParams.Target.GetAttributeCurrentValue(
                    mod.AttributeSetName, mod.AttributeName) ?? 0f;
                    
                float newValue = mod.Operation switch
                {
                    GEOperation.Add => currentValue + mod.Magnitude,
                    GEOperation.Minus => currentValue - mod.Magnitude,
                    GEOperation.Multiply => currentValue * mod.Magnitude,
                    GEOperation.Divide => mod.Magnitude != 0 ? currentValue / mod.Magnitude : currentValue,
                    GEOperation.Override => mod.Magnitude,
                    _ => currentValue
                };
                
                // 直接修改属性的基础值
                var attributeSet = executionParams.Target.AttributeSetContainer.Sets[mod.AttributeSetName];
                if (attributeSet != null)
                {
                    attributeSet.ChangeAttributeBase(mod.AttributeName, newValue);
                }
            }
        }
    }
}