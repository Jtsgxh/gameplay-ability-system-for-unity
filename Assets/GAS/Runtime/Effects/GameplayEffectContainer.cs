using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// 游戏效果容器，管理一个组件上的所有游戏效果
    /// </summary>
    /// <remarks>
    /// 这个容器负责：
    /// - 游戏效果的生命周期管理（添加、移除、过期）
    /// - 效果堆叠逻辑处理
    /// - 效果的Tick更新
    /// - 标签检查和过滤
    /// - 效果间的互相作用管理
    /// </remarks>
    public class GameplayEffectContainer
    {
        /// <summary>
        /// 容器拥有者
        /// </summary>
        private readonly AbilitySystemComponent _owner;
        
        /// <summary>
        /// 当前活跃的游戏效果实例列表
        /// </summary>
        private readonly List<GameplayEffectSpec> _gameplayEffectSpecs = new List<GameplayEffectSpec>();
        
        /// <summary>
        /// 缓存的游戏效果实例列表，用于安全遍历
        /// </summary>
        private readonly List<GameplayEffectSpec> _cachedGameplayEffectSpecs = new List<GameplayEffectSpec>();

        public GameplayEffectContainer(AbilitySystemComponent owner)
        {
            _owner = owner;
        }

        // 容器状态变化事件已改为通过EventBus发布

        /// <summary>
        /// 获取当前容器中的所有游戏效果列表
        /// </summary>
        /// <returns>游戏效果实例列表</returns>
        /// <remarks>
        /// 返回的是内部列表的直接引用，请谨慎修改。
        /// 主要用于调试、UI显示或特殊逻辑处理。
        /// </remarks>
        /// <example>
        /// // 获取所有正在作用的效果
        /// var effects = container.GameplayEffects();
        /// foreach (var effect in effects)
        /// {
        ///     Debug.Log($"效果: {effect.GameplayEffect.GameplayEffectName}");
        /// }
        /// </example>
        public List<GameplayEffectSpec> GameplayEffects()
        {
            return _gameplayEffectSpecs;
        }

        public void Tick()
        {
            _cachedGameplayEffectSpecs.AddRange(_gameplayEffectSpecs);

            foreach (var gameplayEffectSpec in _cachedGameplayEffectSpecs)
            {
                if (gameplayEffectSpec.IsActive)
                {
                    gameplayEffectSpec.Tick();
                }
            }

            _cachedGameplayEffectSpecs.Clear();
        }

        // 委托注册/取消注册方法已移除，请使用EventBus订阅相应事件

        /// <summary>
        /// 移除包含指定标签的所有游戏效果
        /// </summary>
        /// <param name="tags">要匹配的标签集合</param>
        /// <remarks>
        /// 会检查效果的AssetTags和GrantedTags，只要任一类型包含指定标签就会移除。
        /// 常用于实现“净化”、“取消增益”等机制。
        /// 空标签集合会被忽略。
        /// </remarks>
        /// <example>
        /// // 移除所有与“毒素”相关的效果
        /// var poisonTags = new GameplayTagSet(new GameplayTag[] {
        ///     new GameplayTag("Status.Poison"),
        ///     new GameplayTag("Debuff.Poison")
        /// });
        /// container.RemoveGameplayEffectWithAnyTags(poisonTags);
        /// </example>
        public void RemoveGameplayEffectWithAnyTags(GameplayTagSet tags)
        {
            if (tags.Empty) return;

            var removeList = new List<GameplayEffectSpec>();
            foreach (var gameplayEffectSpec in _gameplayEffectSpecs)
            {
                var assetTags = gameplayEffectSpec.GameplayEffect.TagContainer.AssetTags;
                if (!assetTags.Empty && assetTags.HasAnyTags(tags))
                {
                    removeList.Add(gameplayEffectSpec);
                    continue;
                }

                var grantedTags = gameplayEffectSpec.GameplayEffect.TagContainer.GrantedTags;
                if (!grantedTags.Empty && grantedTags.HasAnyTags(tags)) removeList.Add(gameplayEffectSpec);
            }

            foreach (var gameplayEffectSpec in removeList) RemoveGameplayEffectSpec(gameplayEffectSpec);
        }

        /// <summary>
        /// </summary>
        /// <param name="spec"></param>
        /// <returns>
        ///     Returns true if the gameplay effect is successfully applied and remains active.
        ///     Returns false if the gameplay effect is applied but immediately removed due to a tag(in `AssetTags` or `GrantedTags`) match
        ///     with the `RemoveGameplayEffectsWithTags` function, indicating that the effect did not persist.
        /// </returns>
        public GameplayEffectSpec AddGameplayEffectSpec(AbilitySystemComponent source,GameplayEffectSpec effectSpec,bool overwriteEffectLevel = false,int effectLevel = 0)
        {
            if (!effectSpec.GameplayEffect.CanApplyTo(_owner)) return null;
            
            if (effectSpec.GameplayEffect.IsImmune(_owner))
            {
                // TODO 免疫Cue触发
                // var lv = overwriteEffectLevel ? effectLevel : source.Level;
                // effectSpec.Init(source, _owner, lv);
                // effectSpec.TriggerOnImmunity();
                return null;
            }
            
            var level = overwriteEffectLevel ? effectLevel : source.Level;
            if (effectSpec.DurationPolicy == EffectsDurationPolicy.Instant)
            {
                effectSpec.Init(source, _owner, level);
                effectSpec.TriggerOnExecute();
                return null;
            }

            // Check GE Stacking
            if (effectSpec.Stacking.stackingType == StackingType.None)
            {
                return Operation_AddNewGameplayEffectSpec(source, effectSpec,overwriteEffectLevel,effectLevel);
            }
            
            // 处理GE堆叠
            // 基于Target类型GE堆叠
            if (effectSpec.Stacking.stackingType == StackingType.AggregateByTarget)
            {
                GetStackingEffectSpecByData(effectSpec.GameplayEffect, out var geSpec);
                // 新添加GE
                if (geSpec == null)
                    return Operation_AddNewGameplayEffectSpec(source, effectSpec,overwriteEffectLevel,effectLevel);
                bool stackCountChange = geSpec.RefreshStack();
                if (stackCountChange) OnRefreshStackCountMakeContainerDirty();
                return geSpec;
            }
            
            // 基于Source类型GE堆叠
            if (effectSpec.Stacking.stackingType == StackingType.AggregateBySource)
            {
                GetStackingEffectSpecByDataFrom(effectSpec.GameplayEffect,source, out var geSpec);
                if (geSpec == null)
                    return Operation_AddNewGameplayEffectSpec(source, effectSpec,overwriteEffectLevel,effectLevel);
                bool stackCountChange = geSpec.RefreshStack();
                if (stackCountChange) OnRefreshStackCountMakeContainerDirty();
                return geSpec;
            }

            return null;
        }

        public GameplayEffectSpec AddGameplayEffectSpec(AbilitySystemComponent source, GameplayEffect effect,int effectLevel)
        {
            var spec = effect.CreateSpec();
            return AddGameplayEffectSpec(source, spec, true,effectLevel);
        }
        
        public void RemoveGameplayEffectSpec(GameplayEffectSpec spec, bool isPrematureRemoval = true)
        {
            spec.DisApply();
            spec.TriggerOnRemove();
            _gameplayEffectSpecs.Remove(spec);
            
            // 通过EventBus发布游戏效果移除事件
            if (_owner?.EventBus != null)
            {
                _owner.PublishGameplayEffectEvent(spec, GameplayEvents.OnGameplayEffectRemoved);
            }
            
            // Trigger expiration effects based on removal type
            GameplayEffect[] expirationEffects = isPrematureRemoval 
                ? spec.GameplayEffect.PrematureExpirationEffects 
                : spec.GameplayEffect.RoutineExpirationEffects;
                
            if (expirationEffects != null)
            {
                foreach (var expirationEffect in expirationEffects)
                {
                    if (expirationEffect != null)
                    {
                        // 创建效果规格并设置正确的源
                        var expirationSpec = expirationEffect.CreateSpec(spec.Source, _owner, spec.Level);
                        _owner.ApplyGameplayEffectToSelf(expirationSpec);
                    }
                }
            }

            // 通过EventBus发布容器状态变化事件
            if (_owner?.EventBus != null)
            {
                _owner.EventBus.Publish(GameplayEvents.OnGameplayEffectContainerChanged, _owner, null, null, null);
            }
        }

        public void RefreshGameplayEffectState()
        {
            foreach (var gameplayEffectSpec in _gameplayEffectSpecs)
            {
                if (!gameplayEffectSpec.IsApplied) continue;
                if (!gameplayEffectSpec.IsActive)
                {
                    // new active gameplay effects
                    if (gameplayEffectSpec.GameplayEffect.CanRunning(_owner)) gameplayEffectSpec.Activate();
                }
                else
                {
                    // new deactive gameplay effects
                    if (!gameplayEffectSpec.GameplayEffect.CanRunning(_owner)) gameplayEffectSpec.Deactivate();
                }
            }

            // 通过EventBus发布容器状态变化事件
            if (_owner?.EventBus != null)
            {
                _owner.EventBus.Publish(GameplayEvents.OnGameplayEffectContainerChanged, _owner, null, null, null);
            }
        }

        public CooldownTimer CheckCooldownFromTags(GameplayTagSet tags)
        {
            float longestCooldown = 0;
            float maxDuration = 0;

            // Check if the cooldown tag is granted to the player, and if so, capture the remaining duration for that tag
            foreach (var spec in _gameplayEffectSpecs)
            {
                if (spec.IsActive)
                {
                    var grantedTags = spec.GameplayEffect.TagContainer.GrantedTags;
                    if (grantedTags.Empty) continue;
                    foreach (var t in grantedTags.Tags)
                    foreach (var targetTag in tags.Tags)
                    {
                        if (t != targetTag) continue;
                        // If this is an infinite GE, then return null to signify this is on CD
                        if (spec.GameplayEffect.DurationPolicy ==
                            EffectsDurationPolicy.Infinite)
                            return new CooldownTimer { TimeRemaining = -1, Duration = 0 };

                        var durationRemaining = spec.DurationRemaining();

                        if (!(durationRemaining > longestCooldown)) continue;
                        longestCooldown = durationRemaining;
                        maxDuration = spec.Duration;
                    }
                }
            }

            return new CooldownTimer { TimeRemaining = longestCooldown, Duration = maxDuration };
        }

        public void ClearGameplayEffect()
        {
            foreach (var gameplayEffectSpec in _gameplayEffectSpecs)
            {
                gameplayEffectSpec.DisApply();
                gameplayEffectSpec.TriggerOnRemove();
            }

            _gameplayEffectSpecs.Clear();

            // 通过EventBus发布容器状态变化事件
            if (_owner?.EventBus != null)
            {
                _owner.EventBus.Publish(GameplayEvents.OnGameplayEffectContainerChanged, _owner, null, null, null);
            }
        }

        private void GetStackingEffectSpecByData(GameplayEffect effect, out GameplayEffectSpec spec)
        {
            foreach (var gameplayEffectSpec in _gameplayEffectSpecs)
                if (gameplayEffectSpec.GameplayEffect.StackEqual(effect))
                {
                    spec = gameplayEffectSpec;
                    return;
                }

            spec = null;
        }

        private void GetStackingEffectSpecByDataFrom(GameplayEffect effect,AbilitySystemComponent source, 
            out GameplayEffectSpec spec)
        {
            foreach (var gameplayEffectSpec in _gameplayEffectSpecs)
                if (gameplayEffectSpec.Source == source && 
                    gameplayEffectSpec.GameplayEffect.StackEqual(effect))
                {
                    spec = gameplayEffectSpec;
                    return;
                }

            spec = null;
        }

        private void OnRefreshStackCountMakeContainerDirty()
        {
            // 通过EventBus发布容器状态变化事件
            if (_owner?.EventBus != null)
            {
                _owner.EventBus.Publish(GameplayEvents.OnGameplayEffectContainerChanged, _owner, null, null, null);
            }
        }
        
        private GameplayEffectSpec Operation_AddNewGameplayEffectSpec(AbilitySystemComponent source,GameplayEffectSpec effectSpec,
            bool overwriteEffectLevel,int effectLevel)
        {
            var level = overwriteEffectLevel ? effectLevel : source.Level;
            effectSpec.Init(source, _owner, level);
            _gameplayEffectSpecs.Add(effectSpec);
            effectSpec.TriggerOnAdd();
            effectSpec.Apply();

            // 通过EventBus发布游戏效果应用事件
            if (_owner?.EventBus != null)
            {
                _owner.PublishGameplayEffectEvent(effectSpec, GameplayEvents.OnGameplayEffectApplied);
            }

            // If the gameplay effect was removed immediately after being applied, return false
            if (!_gameplayEffectSpecs.Contains(effectSpec))
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning(
                    $"GameplayEffect {effectSpec.GameplayEffect.GameplayEffectName} was removed immediately after being applied. This may indicate a problem with the RemoveGameplayEffectsWithTags.");
#endif
                // No need to trigger OnGameplayEffectContainerIsDirty, it has already been triggered when it was removed.
                return null;
            }

            // 通过EventBus发布容器状态变化事件
            if (_owner?.EventBus != null)
            {
                _owner.EventBus.Publish(GameplayEvents.OnGameplayEffectContainerChanged, _owner, null, null, null);
            }
            return effectSpec;
        }
    }
}