using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// 游戏效果自定义执行参数，提供对源和目标的完全访问能力
    /// 对应UE中的FGameplayEffectCustomExecutionParameters
    /// </summary>
    public class GameplayEffectCustomExecutionParameters
    {
        public AbilitySystemComponent Source { get; }
        public AbilitySystemComponent Target { get; }
        public GameplayEffectSpec EffectSpec { get; }
        public float Level { get; }
        
        public GameplayEffectCustomExecutionParameters(AbilitySystemComponent source, AbilitySystemComponent target, 
            GameplayEffectSpec effectSpec, float level)
        {
            Source = source;
            Target = target;
            EffectSpec = effectSpec;
            Level = level;
        }
        
        /// <summary>
        /// 获取源的属性值（从快照中获取）
        /// </summary>
        public float GetSourceAttribute(string attributeSetName, string attributeName)
        {
            var fullName = $"{attributeSetName}.{attributeName}";
            return EffectSpec.SnapshotSourceAttributes.TryGetValue(fullName, out var value) ? value : 0f;
        }
        
        /// <summary>
        /// 获取目标的属性值（从快照中获取）
        /// </summary>
        public float GetTargetAttribute(string attributeSetName, string attributeName)
        {
            var fullName = $"{attributeSetName}.{attributeName}";
            return EffectSpec.SnapshotTargetAttributes.TryGetValue(fullName, out var value) ? value : 0f;
        }
        
        /// <summary>
        /// 检查是否有捕获指定的源属性
        /// </summary>
        public bool HasCapturedSourceAttribute(string attributeSetName, string attributeName)
        {
            var fullName = $"{attributeSetName}.{attributeName}";
            return EffectSpec.SnapshotSourceAttributes.ContainsKey(fullName);
        }
        
        /// <summary>
        /// 检查是否有捕获指定的目标属性
        /// </summary>
        public bool HasCapturedTargetAttribute(string attributeSetName, string attributeName)
        {
            var fullName = $"{attributeSetName}.{attributeName}";
            return EffectSpec.SnapshotTargetAttributes.ContainsKey(fullName);
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
    /// 游戏效果执行计算基类 - 支持UE式的AttributeCapture机制
    /// 对应UE中的UGameplayEffectExecutionCalculation
    /// </summary>
    /// <remarks>
    /// GameplayEffectExecutionCalculation 用于实现完全自定义的游戏效果执行逻辑。
    /// 在Execute方法中，你可以：
    /// - 直接修改任何属性
    /// - 触发其他GameplayEffect
    /// - 处理GameplayCues
    /// - 实现任意复杂的游戏逻辑
    /// - 根据条件执行不同的操作
    /// 
    /// 支持UE式的Snapshot和No-Snapshot属性捕获机制。
    /// </remarks>
    public abstract class GameplayEffectExecutionCalculation
    {
        /// <summary>
        /// 定义需要捕获的属性 - 子类需要重写此方法来指定要捕获的属性
        /// </summary>
        /// <returns>属性捕获定义数组</returns>
        public virtual GameplayEffectAttributeCaptureDefinition[] GetAttributeCaptureDefinitions()
        {
            return System.Array.Empty<GameplayEffectAttributeCaptureDefinition>();
        }
        
        /// <summary>
        /// 创建执行计算实例
        /// </summary>
        public virtual GameplayEffectExecutionCalculation CreateSpec(AbilitySystemComponent source, 
            AbilitySystemComponent target)
        {
            return Activator.CreateInstance(GetType()) as GameplayEffectExecutionCalculation;
        }

        /// <summary>
        /// 是否支持周期执行
        /// </summary>
        public virtual bool SupportsPeriodExecution()
        {
            return false;
        }

        /// <summary>
        /// 获取执行优先级 - 数值越大优先级越高
        /// </summary>
        public virtual int GetExecutionPriority()
        {
            return 0;
        }

        /// <summary>
        /// 获取执行条件 - 定义ExecutionCalculation的执行条件
        /// </summary>
        /// <returns>执行条件数组，所有条件都满足时才执行</returns>
        /// <remarks>
        /// 子类可以重写此方法来定义自己的执行条件。
        /// 如果返回空数组或null，则表示无条件执行。
        /// 条件包括：属性条件、标签条件、时间条件、事件条件等。
        /// </remarks>
        public virtual GameplayEffectExecutionCondition[] GetExecutionConditions()
        {
            return System.Array.Empty<GameplayEffectExecutionCondition>();
        }

        /// <summary>
        /// 检查是否应该执行此ExecutionCalculation
        /// </summary>
        /// <param name="executionParams">执行参数</param>
        /// <returns>如果所有条件都满足返回true，否则返回false</returns>
        /// <remarks>
        /// 此方法会检查所有GetExecutionConditions()返回的条件。
        /// 只有当所有条件都满足时，ExecutionCalculation才会被执行。
        /// 子类通常不需要重写此方法，而应该重写GetExecutionConditions()。
        /// </remarks>
        public virtual bool ShouldExecute(GameplayEffectCustomExecutionParameters executionParams)
        {
            var conditions = GetExecutionConditions();
            
            // 如果没有条件，则总是执行
            if (conditions == null || conditions.Length == 0)
            {
                return true;
            }
            
            // 所有条件都必须满足
            foreach (var condition in conditions)
            {
                if (condition == null) continue;
                
                if (!condition.IsSatisfied(executionParams))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 执行自定义逻辑 - 在这里可以做任何事情
        /// </summary>
        /// <param name="executionParams">执行参数，包含源、目标和效果信息，支持正确的属性捕获</param>
        /// <remarks>
        /// 在这个方法中，你可以：
        /// 1. 获取属性值：executionParams.GetSourceAttribute("SetName", "AttrName") - 自动处理Snapshot/No-Snapshot
        /// 2. 直接修改属性：executionParams.Target.AttributeSetContainer.Sets["SetName"].ChangeAttributeBase("AttrName", newValue)
        /// 3. 触发其他效果：executionParams.Target.ApplyGameplayEffectToSelf(otherEffect)
        /// 4. 触发GameplayCues：executionParams.Target.TriggerGameplayCue(cueTag)
        /// 5. 实现复杂的条件逻辑和计算
        /// 6. 调用其他系统的功能
        /// </remarks>
        public abstract void Execute(GameplayEffectCustomExecutionParameters executionParams);
    }
}