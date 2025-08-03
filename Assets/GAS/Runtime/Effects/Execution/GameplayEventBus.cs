using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// 游戏事件数据
    /// </summary>
    /// <remarks>
    /// 包含事件的所有相关信息，用于在事件总线中传递。
    /// </remarks>
    public class GameplayEventData
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventName { get; }
        
        /// <summary>
        /// 事件源组件
        /// </summary>
        public AbilitySystemComponent Source { get; }
        
        /// <summary>
        /// 事件目标组件（可为null）
        /// </summary>
        public AbilitySystemComponent Target { get; }
        
        /// <summary>
        /// 事件相关标签
        /// </summary>
        public GameplayTag[] EventTags { get; }
        
        /// <summary>
        /// 事件参数字典
        /// </summary>
        public Dictionary<string, object> Parameters { get; }

        public GameplayEventData(
            string eventName,
            AbilitySystemComponent source,
            AbilitySystemComponent target = null,
            GameplayTag[] eventTags = null,
            Dictionary<string, object> parameters = null)
        {
            EventName = eventName;
            Source = source;
            Target = target;
            EventTags = eventTags;
            Parameters = parameters ?? new Dictionary<string, object>();
        }
        
        /// <summary>
        /// 获取事件参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数键</param>
        /// <returns>参数值，如果不存在则返回默认值</returns>
        public T GetParameter<T>(string key)
        {
            if (Parameters.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default(T);
        }
        
        /// <summary>
        /// 检查是否包含指定参数
        /// </summary>
        /// <param name="key">参数键</param>
        /// <returns>如果包含返回true</returns>
        public bool HasParameter(string key)
        {
            return Parameters.ContainsKey(key);
        }
    }

    /// <summary>
    /// 预定义的游戏事件名称常量
    /// </summary>
    /// <remarks>
    /// 提供常用的游戏事件名称，使用分层命名约定。
    /// 开发者也可以使用自定义的事件名称。
    /// </remarks>
    public static class GameplayEvents
    {
        // 伤害相关事件
        public const string OnDamageReceived = "Gameplay.Damage.Received";
        public const string OnDamageDealt = "Gameplay.Damage.Dealt";
        public const string OnDamageBlocked = "Gameplay.Damage.Blocked";
        public const string OnDamageMitigated = "Gameplay.Damage.Mitigated";
        
        // 治疗相关事件
        public const string OnHealingReceived = "Gameplay.Healing.Received";
        public const string OnHealingDealt = "Gameplay.Healing.Dealt";
        
        // 技能相关事件
        public const string OnAbilityActivated = "Gameplay.Ability.Activated";
        public const string OnAbilityEnded = "Gameplay.Ability.Ended";
        public const string OnAbilityCancelled = "Gameplay.Ability.Cancelled";
        public const string OnAbilityFailed = "Gameplay.Ability.Failed";
        public const string OnAbilityCooldownStarted = "Gameplay.Ability.Cooldown.Started";
        public const string OnAbilityCooldownEnded = "Gameplay.Ability.Cooldown.Ended";
        
        // 标签相关事件
        public const string OnTagAdded = "Gameplay.Tag.Added";
        public const string OnTagRemoved = "Gameplay.Tag.Removed";
        public const string OnTagCountChanged = "Gameplay.Tag.CountChanged";
        
        // 游戏效果相关事件
        public const string OnGameplayEffectApplied = "Gameplay.Effect.Applied";
        public const string OnGameplayEffectRemoved = "Gameplay.Effect.Removed";
        public const string OnGameplayEffectActivated = "Gameplay.Effect.Activated";
        public const string OnGameplayEffectDeactivated = "Gameplay.Effect.Deactivated";
        public const string OnGameplayEffectStackChanged = "Gameplay.Effect.StackChanged";
        
        // 属性相关事件
        public const string OnAttributeChanged = "Gameplay.Attribute.Changed";
        public const string OnAttributePreChange = "Gameplay.Attribute.PreChange";
        public const string OnAttributePostChange = "Gameplay.Attribute.PostChange";
        
        // 生命状态事件
        public const string OnDeath = "Gameplay.Death";
        public const string OnRevive = "Gameplay.Revive";
        public const string OnHealthCritical = "Gameplay.Health.Critical";
        public const string OnResourceDepleted = "Gameplay.Resource.Depleted";
        
        // 战斗状态事件
        public const string OnCombatStarted = "Gameplay.Combat.Started";
        public const string OnCombatEnded = "Gameplay.Combat.Ended";
        public const string OnTargetChanged = "Gameplay.Target.Changed";
    }

    /// <summary>
    /// 游戏事件总线 - 全局事件管理系统
    /// </summary>
    /// <remarks>
    /// GameplayEventBus提供了一个全局的事件发布/订阅系统，
    /// 用于在游戏的不同组件之间传递事件信息。
    /// 
    /// 特点：
    /// - 基于字符串的事件名称，支持自定义事件
    /// - 线程安全的订阅/取消订阅操作
    /// - 自动清理失效的订阅者引用
    /// - 支持事件参数传递
    /// </remarks>
    public class GameplayEventBus
    {
        /// <summary>
        /// 单例实例
        /// </summary>
        private static GameplayEventBus _instance;
        
        /// <summary>
        /// 获取事件总线单例实例
        /// </summary>
        public static GameplayEventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameplayEventBus();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 事件处理器字典 - 事件名 -> 处理器列表
        /// </summary>
        private readonly Dictionary<string, List<Action<GameplayEventData>>> _eventHandlers;
        
        /// <summary>
        /// 锁对象，用于线程安全
        /// </summary>
        private readonly object _lock = new object();

        private GameplayEventBus()
        {
            _eventHandlers = new Dictionary<string, List<Action<GameplayEventData>>>();
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="handler">事件处理器</param>
        /// <remarks>
        /// 订阅指定名称的事件。当该事件被发布时，处理器将被调用。
        /// 同一个处理器可以多次订阅同一个事件，但不建议这样做。
        /// </remarks>
        public void Subscribe(string eventName, Action<GameplayEventData> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null)
                return;

            lock (_lock)
            {
                if (!_eventHandlers.TryGetValue(eventName, out var handlers))
                {
                    handlers = new List<Action<GameplayEventData>>();
                    _eventHandlers[eventName] = handlers;
                }

                handlers.Add(handler);
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="handler">要移除的事件处理器</param>
        /// <returns>如果成功移除返回true，否则返回false</returns>
        public bool Unsubscribe(string eventName, Action<GameplayEventData> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null)
                return false;

            lock (_lock)
            {
                if (_eventHandlers.TryGetValue(eventName, out var handlers))
                {
                    bool removed = handlers.Remove(handler);
                    
                    // 如果处理器列表为空，移除整个条目
                    if (handlers.Count == 0)
                    {
                        _eventHandlers.Remove(eventName);
                    }
                    
                    return removed;
                }
            }

            return false;
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <remarks>
        /// 向所有订阅了该事件的处理器发送事件数据。
        /// 如果处理器执行过程中抛出异常，会被捕获并记录，但不会中断其他处理器的执行。
        /// </remarks>
        public void Publish(GameplayEventData eventData)
        {
            if (eventData == null || string.IsNullOrEmpty(eventData.EventName))
                return;

            List<Action<GameplayEventData>> handlersToCall = null;

            // 获取处理器列表的副本，避免在调用过程中被修改
            lock (_lock)
            {
                if (_eventHandlers.TryGetValue(eventData.EventName, out var handlers) && handlers.Count > 0)
                {
                    handlersToCall = new List<Action<GameplayEventData>>(handlers);
                }
            }

            // 在锁外执行处理器，避免死锁
            if (handlersToCall != null)
            {
                foreach (var handler in handlersToCall)
                {
                    try
                    {
                        handler?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        // 记录异常但不中断其他处理器的执行
                        UnityEngine.Debug.LogError($"Error executing event handler for event '{eventData.EventName}': {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// 发布事件（简化版本）
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="source">事件源</param>
        /// <param name="target">事件目标（可选）</param>
        /// <param name="eventTags">事件标签（可选）</param>
        /// <param name="parameters">事件参数（可选）</param>
        public void Publish(string eventName, AbilitySystemComponent source, 
            AbilitySystemComponent target = null, 
            GameplayTag[] eventTags = null, 
            Dictionary<string, object> parameters = null)
        {
            var eventData = new GameplayEventData(eventName, source, target, eventTags, parameters);
            Publish(eventData);
        }

        /// <summary>
        /// 检查是否有订阅者订阅指定事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <returns>如果有订阅者返回true</returns>
        public bool HasSubscribers(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return false;

            lock (_lock)
            {
                return _eventHandlers.TryGetValue(eventName, out var handlers) && handlers.Count > 0;
            }
        }

        /// <summary>
        /// 获取指定事件的订阅者数量
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <returns>订阅者数量</returns>
        public int GetSubscriberCount(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return 0;

            lock (_lock)
            {
                return _eventHandlers.TryGetValue(eventName, out var handlers) ? handlers.Count : 0;
            }
        }

        /// <summary>
        /// 清除所有事件订阅
        /// </summary>
        /// <remarks>
        /// 通常在游戏结束或场景切换时调用，清理所有事件订阅以避免内存泄漏。
        /// </remarks>
        public void ClearAll()
        {
            lock (_lock)
            {
                _eventHandlers.Clear();
            }
        }

        /// <summary>
        /// 清除指定事件的所有订阅
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <returns>如果成功清除返回true</returns>
        public bool ClearEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return false;

            lock (_lock)
            {
                return _eventHandlers.Remove(eventName);
            }
        }

        /// <summary>
        /// 获取所有已注册的事件名称
        /// </summary>
        /// <returns>事件名称数组</returns>
        public string[] GetRegisteredEvents()
        {
            lock (_lock)
            {
                return new List<string>(_eventHandlers.Keys).ToArray();
            }
        }
    }
}