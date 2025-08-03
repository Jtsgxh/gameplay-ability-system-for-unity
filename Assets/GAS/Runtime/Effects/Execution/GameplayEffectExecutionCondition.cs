using System;
using System.Collections.Generic;
using System.Linq;

namespace GAS.Runtime
{
    /// <summary>
    /// 执行条件类型枚举
    /// </summary>
    public enum ExecutionConditionType
    {
        /// <summary>
        /// 属性条件 - 基于属性值的条件判断
        /// </summary>
        Attribute,
        
        /// <summary>
        /// 标签条件 - 基于GameplayTag的条件判断
        /// </summary>
        Tag,
        
        /// <summary>
        /// 时间条件 - 基于效果持续时间的条件判断
        /// </summary>
        Time,
        
        /// <summary>
        /// 事件条件 - 基于游戏事件的条件判断
        /// </summary>
        Event
    }

    /// <summary>
    /// 比较操作符枚举
    /// </summary>
    public enum ComparisonOperator
    {
        /// <summary>
        /// 等于
        /// </summary>
        Equal,
        
        /// <summary>
        /// 不等于
        /// </summary>
        NotEqual,
        
        /// <summary>
        /// 大于
        /// </summary>
        Greater,
        
        /// <summary>
        /// 大于等于
        /// </summary>
        GreaterOrEqual,
        
        /// <summary>
        /// 小于
        /// </summary>
        Less,
        
        /// <summary>
        /// 小于等于
        /// </summary>
        LessOrEqual
    }

    /// <summary>
    /// 游戏效果执行条件基类
    /// </summary>
    /// <remarks>
    /// ExecutionCondition定义了ExecutionCalculation的执行条件。
    /// 只有当所有条件都满足时，ExecutionCalculation才会被执行。
    /// 这实现了UE中的ConditionalGameplayEffect机制。
    /// </remarks>
    public abstract class GameplayEffectExecutionCondition
    {
        /// <summary>
        /// 条件类型
        /// </summary>
        public abstract ExecutionConditionType ConditionType { get; }
        
        /// <summary>
        /// 条件描述（用于调试和编辑器显示）
        /// </summary>
        public virtual string Description => GetType().Name;
        
        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        /// <param name="executionParams">执行参数，包含源、目标和效果信息</param>
        /// <returns>如果条件满足返回true，否则返回false</returns>
        public abstract bool IsSatisfied(GameplayEffectCustomExecutionParameters executionParams);
        
        /// <summary>
        /// 是否需要持续监听此条件的变化
        /// </summary>
        /// <returns>如果需要监听返回true（如属性变化、标签变化），否则返回false（如一次性时间条件）</returns>
        public virtual bool RequiresContinuousMonitoring => false;
    }

    /// <summary>
    /// 属性执行条件
    /// </summary>
    /// <remarks>
    /// 基于属性值的条件判断，如"血量小于50%时执行"。
    /// 支持对源或目标属性的比较判断。
    /// </remarks>
    public class AttributeExecutionCondition : GameplayEffectExecutionCondition
    {
        public override ExecutionConditionType ConditionType => ExecutionConditionType.Attribute;
        public override bool RequiresContinuousMonitoring => true;
        
        /// <summary>
        /// 属性来源（源或目标）
        /// </summary>
        public enum AttributeSource
        {
            Source,
            Target
        }
        
        /// <summary>
        /// 属性来源
        /// </summary>
        public AttributeSource Source { get; }
        
        /// <summary>
        /// 属性集名称
        /// </summary>
        public string AttributeSetName { get; }
        
        /// <summary>
        /// 属性名称
        /// </summary>
        public string AttributeName { get; }
        
        /// <summary>
        /// 比较操作符
        /// </summary>
        public ComparisonOperator Operator { get; }
        
        /// <summary>
        /// 比较值
        /// </summary>
        public float ComparisonValue { get; }
        
        /// <summary>
        /// 是否使用百分比比较（相对于最大值）
        /// </summary>
        public bool UsePercentage { get; }

        public AttributeExecutionCondition(
            AttributeSource source,
            string attributeSetName,
            string attributeName,
            ComparisonOperator op,
            float comparisonValue,
            bool usePercentage = false)
        {
            Source = source;
            AttributeSetName = attributeSetName;
            AttributeName = attributeName;
            Operator = op;
            ComparisonValue = comparisonValue;
            UsePercentage = usePercentage;
        }

        public override bool IsSatisfied(GameplayEffectCustomExecutionParameters executionParams)
        {
            var fullAttributeName = $"{AttributeSetName}.{AttributeName}";
            float currentValue;
            
            if (Source == AttributeSource.Source)
            {
                currentValue = executionParams.GetSourceAttribute(AttributeSetName, AttributeName);
            }
            else
            {
                currentValue = executionParams.GetTargetAttribute(AttributeSetName, AttributeName);
            }
            
            // 如果使用百分比，需要获取最大值进行计算
            if (UsePercentage)
            {
                var maxAttributeName = $"{AttributeSetName}.Max{AttributeName}";
                float maxValue;
                
                if (Source == AttributeSource.Source)
                {
                    maxValue = executionParams.GetSourceAttribute(AttributeSetName, $"Max{AttributeName}");
                }
                else
                {
                    maxValue = executionParams.GetTargetAttribute(AttributeSetName, $"Max{AttributeName}");
                }
                
                if (maxValue > 0)
                {
                    currentValue = (currentValue / maxValue) * 100f;
                }
            }
            
            return CompareValues(currentValue, ComparisonValue, Operator);
        }
        
        private bool CompareValues(float left, float right, ComparisonOperator op)
        {
            return op switch
            {
                ComparisonOperator.Equal => Math.Abs(left - right) < 0.001f,
                ComparisonOperator.NotEqual => Math.Abs(left - right) >= 0.001f,
                ComparisonOperator.Greater => left > right,
                ComparisonOperator.GreaterOrEqual => left >= right,
                ComparisonOperator.Less => left < right,
                ComparisonOperator.LessOrEqual => left <= right,
                _ => false
            };
        }

        public override string Description => 
            $"{Source}.{AttributeSetName}.{AttributeName} {Operator} {ComparisonValue}{(UsePercentage ? "%" : "")}";
    }

    /// <summary>
    /// 标签执行条件
    /// </summary>
    /// <remarks>
    /// 基于GameplayTag的条件判断，如"拥有特定标签时执行"或"不拥有特定标签时执行"。
    /// </remarks>
    public class TagExecutionCondition : GameplayEffectExecutionCondition
    {
        public override ExecutionConditionType ConditionType => ExecutionConditionType.Tag;
        public override bool RequiresContinuousMonitoring => true;
        
        /// <summary>
        /// 标签来源（源或目标）
        /// </summary>
        public enum TagSource
        {
            Source,
            Target
        }
        
        /// <summary>
        /// 标签检查类型
        /// </summary>
        public enum TagCheckType
        {
            /// <summary>
            /// 拥有所有指定标签
            /// </summary>
            HasAll,
            
            /// <summary>
            /// 拥有任意一个指定标签
            /// </summary>
            HasAny,
            
            /// <summary>
            /// 不拥有所有指定标签
            /// </summary>
            DoesNotHaveAll,
            
            /// <summary>
            /// 不拥有任意一个指定标签
            /// </summary>
            DoesNotHaveAny
        }
        
        /// <summary>
        /// 标签来源
        /// </summary>
        public TagSource Source { get; }
        
        /// <summary>
        /// 检查类型
        /// </summary>
        public TagCheckType CheckType { get; }
        
        /// <summary>
        /// 要检查的标签数组
        /// </summary>
        public GameplayTag[] Tags { get; }

        public TagExecutionCondition(TagSource source, TagCheckType checkType, params GameplayTag[] tags)
        {
            Source = source;
            CheckType = checkType;
            Tags = tags;
        }

        public override bool IsSatisfied(GameplayEffectCustomExecutionParameters executionParams)
        {
            var targetComponent = Source == TagSource.Source ? executionParams.Source : executionParams.Target;
            var tagSet = new GameplayTagSet(Tags);
            
            return CheckType switch
            {
                TagCheckType.HasAll => targetComponent.HasAllTags(tagSet),
                TagCheckType.HasAny => targetComponent.HasAnyTags(tagSet),
                TagCheckType.DoesNotHaveAll => !targetComponent.HasAllTags(tagSet),
                TagCheckType.DoesNotHaveAny => !targetComponent.HasAnyTags(tagSet),
                _ => false
            };
        }

        public override string Description => 
            $"{Source} {CheckType} [{string.Join(", ", Tags)}]";
    }

    /// <summary>
    /// 时间执行条件
    /// </summary>
    /// <remarks>
    /// 基于效果持续时间的条件判断，如"效果存在超过10秒后执行"。
    /// </remarks>
    public class TimeExecutionCondition : GameplayEffectExecutionCondition
    {
        public override ExecutionConditionType ConditionType => ExecutionConditionType.Time;
        public override bool RequiresContinuousMonitoring => true;
        
        /// <summary>
        /// 时间比较类型
        /// </summary>
        public enum TimeComparison
        {
            /// <summary>
            /// 效果已存在时间
            /// </summary>
            ElapsedTime,
            
            /// <summary>
            /// 效果剩余时间
            /// </summary>
            RemainingTime
        }
        
        /// <summary>
        /// 时间比较类型
        /// </summary>
        public TimeComparison Comparison { get; }
        
        /// <summary>
        /// 比较操作符
        /// </summary>
        public ComparisonOperator Operator { get; }
        
        /// <summary>
        /// 比较时间值（秒）
        /// </summary>
        public float TimeValue { get; }

        public TimeExecutionCondition(TimeComparison comparison, ComparisonOperator op, float timeValue)
        {
            Comparison = comparison;
            Operator = op;
            TimeValue = timeValue;
        }

        public override bool IsSatisfied(GameplayEffectCustomExecutionParameters executionParams)
        {
            float currentTime = Comparison switch
            {
                TimeComparison.ElapsedTime => UnityEngine.Time.time - executionParams.EffectSpec.ActivationTime,
                TimeComparison.RemainingTime => executionParams.EffectSpec.DurationRemaining(),
                _ => 0f
            };
            
            return CompareValues(currentTime, TimeValue, Operator);
        }
        
        private bool CompareValues(float left, float right, ComparisonOperator op)
        {
            return op switch
            {
                ComparisonOperator.Equal => Math.Abs(left - right) < 0.001f,
                ComparisonOperator.NotEqual => Math.Abs(left - right) >= 0.001f,
                ComparisonOperator.Greater => left > right,
                ComparisonOperator.GreaterOrEqual => left >= right,
                ComparisonOperator.Less => left < right,
                ComparisonOperator.LessOrEqual => left <= right,
                _ => false
            };
        }

        public override string Description => 
            $"{Comparison} {Operator} {TimeValue}s";
    }

    /// <summary>
    /// 事件执行条件
    /// </summary>
    /// <remarks>
    /// 基于游戏事件的条件判断，如"受到伤害时执行"、"技能释放时执行"等。
    /// 使用字符串事件名，支持自定义事件和预定义事件。
    /// 与GameplayEventBus集成，实现事件驱动的ExecutionCalculation执行。
    /// </remarks>
    public class EventExecutionCondition : GameplayEffectExecutionCondition
    {
        public override ExecutionConditionType ConditionType => ExecutionConditionType.Event;
        public override bool RequiresContinuousMonitoring => true;
        
        /// <summary>
        /// 事件来源（源或目标）
        /// </summary>
        public enum EventSource
        {
            /// <summary>
            /// 事件必须来自效果源
            /// </summary>
            Source,
            
            /// <summary>
            /// 事件必须来自效果目标
            /// </summary>
            Target,
            
            /// <summary>
            /// 事件可以来自任何来源
            /// </summary>
            Any
        }
        
        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventName { get; }
        
        /// <summary>
        /// 事件来源限制
        /// </summary>
        public EventSource Source { get; }
        
        /// <summary>
        /// 事件参数过滤器（可选）
        /// </summary>
        /// <remarks>
        /// 如果指定了事件标签过滤器，只有当事件包含这些标签中的任意一个时，条件才会满足。
        /// </remarks>
        public GameplayTag[] EventTagFilters { get; }
        
        /// <summary>
        /// 事件参数过滤器（可选）
        /// </summary>
        /// <remarks>
        /// 用于检查事件参数是否满足特定条件。
        /// 键为参数名，值为期望的参数值。
        /// </remarks>
        public Dictionary<string, object> ParameterFilters { get; }
        
        /// <summary>
        /// 事件是否已触发（内部状态）
        /// </summary>
        private bool _eventTriggered = false;
        
        /// <summary>
        /// 存储触发的事件数据
        /// </summary>
        private GameplayEventData _triggeredEventData;

        public EventExecutionCondition(
            string eventName, 
            EventSource source = EventSource.Any,
            GameplayTag[] eventTagFilters = null,
            Dictionary<string, object> parameterFilters = null)
        {
            EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
            Source = source;
            EventTagFilters = eventTagFilters;
            ParameterFilters = parameterFilters;
        }

        public override bool IsSatisfied(GameplayEffectCustomExecutionParameters executionParams)
        {
            // 事件条件的满足状态由事件监听器设置
            return _eventTriggered;
        }
        
        /// <summary>
        /// 检查事件数据是否匹配此条件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <param name="executionParams">执行参数</param>
        /// <returns>如果事件匹配条件返回true</returns>
        public bool MatchesEvent(GameplayEventData eventData, GameplayEffectCustomExecutionParameters executionParams)
        {
            if (eventData == null || eventData.EventName != EventName)
                return false;
            
            // 检查事件来源限制
            if (Source != EventSource.Any)
            {
                var expectedSource = Source == EventSource.Source ? executionParams.Source : executionParams.Target;
                if (eventData.Source != expectedSource && eventData.Target != expectedSource)
                {
                    return false;
                }
            }
            
            // 检查事件标签过滤器
            if (EventTagFilters != null && EventTagFilters.Length > 0)
            {
                if (eventData.EventTags == null || eventData.EventTags.Length == 0)
                {
                    return false;
                }
                
                var filterTagSet = new GameplayTagSet(EventTagFilters);
                var eventTagSet = new GameplayTagSet(eventData.EventTags);
                
                if (!eventTagSet.HasAnyTags(filterTagSet))
                {
                    return false;
                }
            }
            
            // 检查参数过滤器
            if (ParameterFilters != null && ParameterFilters.Count > 0)
            {
                foreach (var filter in ParameterFilters)
                {
                    if (!eventData.HasParameter(filter.Key))
                    {
                        return false;
                    }
                    
                    var eventValue = eventData.GetParameter<object>(filter.Key);
                    if (!Equals(eventValue, filter.Value))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 触发事件条件（由事件监听器调用）
        /// </summary>
        /// <param name="eventData">触发的事件数据</param>
        /// <returns>是否成功触发</returns>
        internal bool TriggerEvent(GameplayEventData eventData)
        {
            _eventTriggered = true;
            _triggeredEventData = eventData;
            return true;
        }
        
        /// <summary>
        /// 重置事件状态
        /// </summary>
        /// <remarks>
        /// 通常在ExecutionCalculation执行完毕后调用，准备下次事件触发。
        /// </remarks>
        internal void ResetEvent()
        {
            _eventTriggered = false;
            _triggeredEventData = null;
        }
        
        /// <summary>
        /// 获取触发的事件数据
        /// </summary>
        /// <returns>最后一次触发的事件数据，如果没有触发过则返回null</returns>
        public GameplayEventData GetTriggeredEventData()
        {
            return _triggeredEventData;
        }

        public override string Description
        {
            get
            {
                var desc = $"{Source} event '{EventName}'";
                
                if (EventTagFilters?.Length > 0)
                {
                    desc += $" with tags [{string.Join(", ", EventTagFilters)}]";
                }
                
                if (ParameterFilters?.Count > 0)
                {
                    var paramDesc = string.Join(", ", ParameterFilters.Select(p => $"{p.Key}={p.Value}"));
                    desc += $" with params [{paramDesc}]";
                }
                
                return desc;
            }
        }
    }
}