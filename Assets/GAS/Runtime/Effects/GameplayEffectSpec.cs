using System;
using System.Collections.Generic;
using GAS.General;
using UnityEngine;

namespace GAS.Runtime
{
    /// <summary>
    /// 游戏效果实例规格，管理单个游戏效果的运行时状态和行为
    /// </summary>
    /// <remarks>
    /// GameplayEffectSpec是游戏效果系统的核心运行时组件，负责：
    /// - 效果的生命周期管理（激活、禁用、移除）
    /// - 效果堆叠逻辑处理
    /// - 属性修饰符的应用和管理
    /// - Cue效果的触发和管理
    /// - 授予技能的管理
    /// - 时间相关的更新（持续时间、周期执行）
    /// - 数值计算用的属性快照
    /// 
    /// 每个GameplayEffectSpec对应一个实际应用到目标上的效果实例。
    /// 即使是同一个GameplayEffect，也可以同时存在多个Spec实例。
    /// </remarks>
    public class GameplayEffectSpec
    {
        private Dictionary<GameplayTag, float> _valueMapWithTag = new Dictionary<GameplayTag, float>();
        private Dictionary<string, float> _valueMapWithName = new Dictionary<string, float>();
        private List<GameplayCueDurationalSpec> _cueDurationalSpecs = new List<GameplayCueDurationalSpec>();

        /// <summary>
        /// The execution type of onImmunity is one shot.
        /// </summary>
#pragma warning disable CS0067 // 事件从未使用过
        public event Action<AbilitySystemComponent, GameplayEffectSpec> onImmunity;
#pragma warning restore CS0067 // 事件从未使用过
        
        public event Action<int,int> onStackCountChanged;

        
        /// <summary>
        /// 初始化游戏效果实例规格
        /// </summary>
        /// <param name="gameplayEffect">游戏效果定义</param>
        /// <remarks>
        /// 初始化时从游戏效果定义复制基本属性：
        /// - 持续时间和持续策略
        /// - 堆叠规则
        /// - 属性修饰符
        /// - 为非瞬时效果创建周期计时器
        /// 
        /// 此时尚未与特定的源和目标绑定，需要后续调用Init()。
        /// </remarks>
        public GameplayEffectSpec(GameplayEffect gameplayEffect)
        {
            GameplayEffect = gameplayEffect;
            Duration = GameplayEffect.Duration;
            DurationPolicy = GameplayEffect.DurationPolicy;
            Stacking = GameplayEffect.Stacking;
            Modifiers = GameplayEffect.Modifiers;
            if (gameplayEffect.DurationPolicy != EffectsDurationPolicy.Instant)
            {
                PeriodTicker = new GameplayEffectPeriodTicker(this);
            }
        }

        /// <summary>
        /// 初始化效果实例的源、目标和等级信息
        /// </summary>
        /// <param name="source">效果源</param>
        /// <param name="owner">效果目标/拥有者</param>
        /// <param name="level">效果等级</param>
        /// <remarks>
        /// 初始化流程：
        /// 1. 设置效果的源、目标和等级
        /// 2. 为非瞬时效果创建周期执行效果
        /// 3. 设置授予技能规格
        /// 4. 捕获属性快照用于数值计算
        /// 
        /// 此方法通常在GameplayEffect被应用到目标时调用。
        /// </remarks>
        public void Init(AbilitySystemComponent source, AbilitySystemComponent owner, float level = 1)
        {
            Source = source;
            Owner = owner;
            Level = level;
            if (GameplayEffect.DurationPolicy != EffectsDurationPolicy.Instant)
            {
                PeriodExecution = GameplayEffect.PeriodExecution?.CreateSpec(source, owner);
                SetGrantedAbility(GameplayEffect.GrantedAbilities);
            }
            CaptureAttributesSnapshot();
        }
        /// <summary>
        /// 获取游戏效果定义
        /// </summary>
        public GameplayEffect GameplayEffect { get; }
        
        /// <summary>
        /// 获取效果激活时间
        /// </summary>
        /// <remarks>
        /// 用于Time.time的时间戳，计算效果的剩余时间。
        /// </remarks>
        public float ActivationTime { get; private set; }
        
        /// <summary>
        /// 获取效果等级
        /// </summary>
        /// <remarks>
        /// 效果等级影响属性修饰符的数值计算。
        /// </remarks>
        public float Level { get; private set; }
        
        /// <summary>
        /// 获取效果源组件
        /// </summary>
        /// <remarks>
        /// 施放效果的AbilitySystemComponent，用于属性快照和数值计算。
        /// </remarks>
        public AbilitySystemComponent Source { get; private set; }
        
        /// <summary>
        /// 获取效果目标/拥有者组件
        /// </summary>
        /// <remarks>
        /// 接受效果的AbilitySystemComponent，效果将应用到此组件上。
        /// </remarks>
        public AbilitySystemComponent Owner { get; private set; }
        
        /// <summary>
        /// 检查效果是否已被应用
        /// </summary>
        /// <remarks>
        /// 应用状态表示效果已被添加到目标的效果容器中。
        /// 与激活状态不同，应用但未激活的效果不会产生实际效果。
        /// </remarks>
        public bool IsApplied { get; private set; }
        
        /// <summary>
        /// 检查效果是否处于激活状态
        /// </summary>
        /// <remarks>
        /// 激活状态的效果会实际修改属性值并产生游戏效果。
        /// 只有满足激活条件时效果才会进入激活状态。
        /// </remarks>
        public bool IsActive { get; private set; }
        
        /// <summary>
        /// 获取周期计时器
        /// </summary>
        /// <remarks>
        /// 用于管理持续效果的周期执行，瞬时效果为null。
        /// </remarks>
        public GameplayEffectPeriodTicker PeriodTicker { get; }
        
        /// <summary>
        /// 获取效果持续时间
        /// </summary>
        /// <remarks>
        /// 可以通过SetDuration()动态修改，用于变长或缩短效果持续时间。
        /// </remarks>
        public float Duration { get; private set; }
        
        /// <summary>
        /// 获取效果持续策略
        /// </summary>
        /// <remarks>
        /// 定义效果的时间特性：瞬时、持续或无限。
        /// </remarks>
        public EffectsDurationPolicy DurationPolicy { get; private set; }
        
        /// <summary>
        /// 获取周期执行效果实例
        /// </summary>
        /// <remarks>
        /// 如果此效果有周期执行，此属性为周期执行效果的Spec实例。
        /// </remarks>
        public GameplayEffectSpec PeriodExecution { get; private set; }
        
        /// <summary>
        /// 获取属性修饰符数组
        /// </summary>
        /// <remarks>
        /// 定义此效果对目标属性的修改规则。
        /// </remarks>
        public GameplayEffectModifier[] Modifiers { get; private set; }
        
        /// <summary>
        /// 获取授予技能规格数组
        /// </summary>
        /// <remarks>
        /// 此效果激活时会授予目标的技能列表。
        /// </remarks>
        public GrantedAbilitySpecFromEffect[] GrantedAbilitySpec { get; private set; }
        
        /// <summary>
        /// 获取堆叠规则
        /// </summary>
        /// <remarks>
        /// 定义同类效果的堆叠行为和上限。
        /// </remarks>
        public GameplayEffectStacking Stacking { get; private set; }

        
        /// <summary>
        /// 获取源组件的属性快照
        /// </summary>
        /// <remarks>
        /// 在效果初始化时捕获，用于属性修饰符的数值计算。
        /// 确保数值计算的一致性，不受后续属性变化影响。
        /// </remarks>
        public Dictionary<string, float> SnapshotSourceAttributes { get; private set; }
        
        /// <summary>
        /// 获取目标组件的属性快照
        /// </summary>
        /// <remarks>
        /// 在效果初始化时捕获，用于属性修饰符的数值计算。
        /// 如果源和目标是同一个组件，则与源快照相同。
        /// </remarks>
        public Dictionary<string, float> SnapshotTargetAttributes { get; private set; }

        /// <summary>
        /// 堆叠数
        /// </summary>
        public int StackCount { get; private set; } = 1;
        

        /// <summary>
        /// 计算效果的剩余持续时间
        /// </summary>
        /// <returns>剩余时间（秒），无限效果返回-1</returns>
        /// <remarks>
        /// 基于激活时间和当前时间计算剩余时间。
        /// 如果效果是无限持续的，返回-1。
        /// 已经过期的效果返回0。
        /// </remarks>
        public float DurationRemaining()
        {
            if (DurationPolicy == EffectsDurationPolicy.Infinite)
                return -1;

            return Mathf.Max(0, Duration - (Time.time - ActivationTime));
        }

        /// <summary>
        /// 设置效果等级
        /// </summary>
        /// <param name="level">新的效果等级</param>
        public void SetLevel(float level)
        {
            Level = level;
        }

        /// <summary>
        /// 设置效果激活时间
        /// </summary>
        /// <param name="activationTime">激活时间戳</param>
        public void SetActivationTime(float activationTime)
        {
            ActivationTime = activationTime;
        }
        
        /// <summary>
        /// 设置效果持续时间
        /// </summary>
        /// <param name="duration">新的持续时间（秒）</param>
        /// <remarks>
        /// 常用于动态调整效果持续时间，如技能等级影响效果时长。
        /// </remarks>
        public void SetDuration(float duration)
        {
            Duration = duration;
        }

        /// <summary>
        /// 设置效果持续策略
        /// </summary>
        /// <param name="durationPolicy">新的持续策略</param>
        public void SetDurationPolicy(EffectsDurationPolicy durationPolicy)
        {
            DurationPolicy = durationPolicy;
        }

        /// <summary>
        /// 设置周期执行效果
        /// </summary>
        /// <param name="periodExecution">周期执行效果实例</param>
        public void SetPeriodExecution(GameplayEffectSpec periodExecution)
        {
            PeriodExecution = periodExecution;
        }

        /// <summary>
        /// 设置属性修饰符数组
        /// </summary>
        /// <param name="modifiers">新的修饰符数组</param>
        public void SetModifiers(GameplayEffectModifier[] modifiers)
        {
            Modifiers = modifiers;
        }

        public void SetGrantedAbility(GrantedAbilityFromEffect[] grantedAbility)
        {
            GrantedAbilitySpec = new GrantedAbilitySpecFromEffect[grantedAbility.Length];
            for (var i = 0; i < grantedAbility.Length; i++)
            {
                GrantedAbilitySpec[i] = grantedAbility[i].CreateSpec(this);
            }
        }

        public void SetStacking(GameplayEffectStacking stacking)
        {
            Stacking = stacking;
        }

        /// <summary>
        /// 应用效果到目标
        /// </summary>
        /// <remarks>
        /// 将效果标记为已应用，并检查是否满足激活条件。
        /// 如果满足条件，会自动调用Activate()。
        /// 重复调用会被忽略。
        /// </remarks>
        public void Apply()
        {
            if (IsApplied) return;
            IsApplied = true;

            if (GameplayEffect.CanRunning(Owner))
            {
                Activate();
            }
        }

        /// <summary>
        /// 取消应用效果
        /// </summary>
        /// <remarks>
        /// 将效果标记为未应用，并取消激活状态。
        /// 重复调用会被忽略。
        /// </remarks>
        public void DisApply()
        {
            if (!IsApplied) return;
            IsApplied = false;
            Deactivate();
        }

        /// <summary>
        /// 激活效果
        /// </summary>
        /// <remarks>
        /// 激活流程：
        /// 1. 设置激活状态和激活时间
        /// 2. 触发激活相关的处理逻辑
        /// 3. 应用动态标签和移除冲突效果
        /// 4. 激活授予技能
        /// 重复调用会被忽略。
        /// </remarks>
        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            ActivationTime = Time.time;
            TriggerOnActivation();
        }

        /// <summary>
        /// 取消激活效果
        /// </summary>
        /// <remarks>
        /// 取消激活流程：
        /// 1. 设置为非激活状态
        /// 2. 触发取消激活相关的处理逻辑
        /// 3. 恢复动态标签
        /// 4. 取消激活授予技能
        /// 重复调用会被忽略。
        /// </remarks>
        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            TriggerOnDeactivation();
        }


        /// <summary>
        /// 效果的帧更新
        /// </summary>
        /// <remarks>
        /// 仅调用周期计时器的Tick，用于处理周期执行效果。
        /// 由系统的更新循环自动调用。
        /// </remarks>
        public void Tick()
        {
            PeriodTicker?.Tick();
        }

        void TriggerInstantCues(GameplayCueInstant[] cues)
        {
            foreach (var cue in cues) cue.ApplyFrom(this);
        }

        private void TriggerCueOnExecute()
        {
            if (GameplayEffect.CueOnExecute == null || GameplayEffect.CueOnExecute.Length <= 0) return;
            TriggerInstantCues(GameplayEffect.CueOnExecute);
        }

        private void TriggerCueOnAdd()
        {
            if (GameplayEffect.CueOnAdd != null && GameplayEffect.CueOnAdd.Length > 0)
                TriggerInstantCues(GameplayEffect.CueOnAdd);

            if (GameplayEffect.CueDurational != null && GameplayEffect.CueDurational.Length > 0)
            {
                _cueDurationalSpecs.Clear();
                foreach (var cueDurational in GameplayEffect.CueDurational)
                {
                    var cueSpec = cueDurational.ApplyFrom(this);
                    if (cueSpec != null) _cueDurationalSpecs.Add(cueSpec);
                }

                foreach (var cue in _cueDurationalSpecs) cue.OnAdd();
            }
        }

        private void TriggerCueOnRemove()
        {
            if (GameplayEffect.CueOnRemove != null && GameplayEffect.CueOnRemove.Length > 0)
                TriggerInstantCues(GameplayEffect.CueOnRemove);

            if (GameplayEffect.CueDurational != null && GameplayEffect.CueDurational.Length > 0)
            {
                foreach (var cue in _cueDurationalSpecs) cue.OnRemove();

                _cueDurationalSpecs = null;
            }
        }

        private void TriggerCueOnActivation()
        {
            if (GameplayEffect.CueOnActivate != null && GameplayEffect.CueOnActivate.Length > 0)
                TriggerInstantCues(GameplayEffect.CueOnActivate);

            if (GameplayEffect.CueDurational != null && GameplayEffect.CueDurational.Length > 0)
                foreach (var cue in _cueDurationalSpecs)
                    cue.OnGameplayEffectActivate();
        }

        private void TriggerCueOnDeactivation()
        {
            if (GameplayEffect.CueOnDeactivate != null && GameplayEffect.CueOnDeactivate.Length > 0)
                TriggerInstantCues(GameplayEffect.CueOnDeactivate);

            if (GameplayEffect.CueDurational != null && GameplayEffect.CueDurational.Length > 0)
                foreach (var cue in _cueDurationalSpecs)
                    cue.OnGameplayEffectDeactivate();
        }

        private void CueOnTick()
        {
            if (GameplayEffect.CueDurational == null || GameplayEffect.CueDurational.Length <= 0) return;
            foreach (var cue in _cueDurationalSpecs) cue.OnTick();
        }

        public void TriggerOnExecute()
        {
            Owner.GameplayEffectContainer.RemoveGameplayEffectWithAnyTags(GameplayEffect.TagContainer
                .RemoveGameplayEffectsWithTags);
            
            // 执行 Modifier
            Owner.ApplyModFromInstantGameplayEffect(this);
            
            // 执行 GameplayEffectExecutionCalculation
            if (GameplayEffect.Executions != null && GameplayEffect.Executions.Length > 0)
            {
                foreach (var execution in GameplayEffect.Executions)
                {
                    if (execution == null) continue;
                    
                    var executionParams = new GameplayEffectCustomExecutionParameters(Source, Owner, this, Level);
                    var executionOutput = new GameplayEffectCustomExecutionOutput();
                    
                    execution.Execute(executionParams, executionOutput);
                    execution.ApplyExecutionToTarget(executionParams, executionOutput);
                }
            }
            
            TriggerCueOnExecute();
        }

        public void TriggerOnAdd()
        {
            TriggerCueOnAdd();
        }

        public void TriggerOnRemove()
        {
            TriggerCueOnRemove();
            
            TryRemoveGrantedAbilities();
        }

        private void TriggerOnActivation()
        {
            TriggerCueOnActivation();
            Owner.GameplayTagAggregator.ApplyGameplayEffectDynamicTag(this);
            Owner.GameplayEffectContainer.RemoveGameplayEffectWithAnyTags(GameplayEffect.TagContainer
                .RemoveGameplayEffectsWithTags);
            
            TryActivateGrantedAbilities();
        }

        private void TriggerOnDeactivation()
        {
            TriggerCueOnDeactivation();
            Owner.GameplayTagAggregator.RestoreGameplayEffectDynamicTags(this);
            
            TryDeactivateGrantedAbilities();
        }

        public void TriggerOnTick()
        {
            if (DurationPolicy == EffectsDurationPolicy.Duration ||
                DurationPolicy == EffectsDurationPolicy.Infinite)
                CueOnTick();
        }

        public void TriggerOnImmunity()
        {
            // TODO 免疫触发事件逻辑需要调整
            // onImmunity?.Invoke(Owner, this);
            // onImmunity = null;
        }

        public void RemoveSelf(bool isPrematureRemoval = true)
        {
            Owner.GameplayEffectContainer.RemoveGameplayEffectSpec(this, isPrematureRemoval);
        }

        private void CaptureAttributesSnapshot()
        {
            SnapshotSourceAttributes = Source.DataSnapshot();
            SnapshotTargetAttributes = Source == Owner ? SnapshotSourceAttributes : Owner.DataSnapshot();
        }

        /// <summary>
        /// 注册基于标签的数值映射
        /// </summary>
        /// <param name="tag">标签键</param>
        /// <param name="value">数值</param>
        /// <remarks>
        /// 用于SetByCaller机制，允许在运行时传递数值给效果。
        /// </remarks>
        public void RegisterValue(GameplayTag tag, float value)
        {
            _valueMapWithTag[tag] = value;
        }

        /// <summary>
        /// 注册基于名称的数值映射
        /// </summary>
        /// <param name="name">名称键</param>
        /// <param name="value">数值</param>
        /// <remarks>
        /// 用于SetByCaller机制，允许在运行时传递数值给效果。
        /// </remarks>
        public void RegisterValue(string name, float value)
        {
            _valueMapWithName[name] = value;
        }

        /// <summary>
        /// 注销基于标签的数值映射
        /// </summary>
        /// <param name="tag">要移除的标签键</param>
        /// <returns>如果成功移除返回true</returns>
        public bool UnregisterValue(GameplayTag tag)
        {
            return _valueMapWithTag.Remove(tag);
        }

        /// <summary>
        /// 注销基于名称的数值映射
        /// </summary>
        /// <param name="name">要移除的名称键</param>
        /// <returns>如果成功移除返回true</returns>
        public bool UnregisterValue(string name)
        {
            return _valueMapWithName.Remove(name);
        }

        /// <summary>
        /// 获取基于标签的映射数值
        /// </summary>
        /// <param name="tag">标签键</param>
        /// <returns>数值，如果不存在则返回null</returns>
        public float? GetMapValue(GameplayTag tag)
        {
            return _valueMapWithTag.TryGetValue(tag, out var value) ? value : (float?)null;
        }

        /// <summary>
        /// 获取基于名称的映射数值
        /// </summary>
        /// <param name="name">名称键</param>
        /// <returns>数值，如果不存在则返回null</returns>
        public float? GetMapValue(string name)
        {
            return _valueMapWithName.TryGetValue(name, out var value) ? value : (float?)null;
        }
        
        /// <summary>
        /// 检查是否有基于标签的SetByCaller数值
        /// </summary>
        /// <param name="tag">标签键</param>
        /// <returns>如果存在则返回true</returns>
        public bool HasSetByCallerMagnitude(GameplayTag tag)
        {
            return _valueMapWithTag.ContainsKey(tag);
        }
        
        /// <summary>
        /// 检查是否有基于名称的SetByCaller数值
        /// </summary>
        /// <param name="name">名称键</param>
        /// <returns>如果存在则返回true</returns>
        public bool HasSetByCallerMagnitude(string name)
        {
            return _valueMapWithName.ContainsKey(name);
        }
        
        /// <summary>
        /// 获取基于标签的SetByCaller数值
        /// </summary>
        /// <param name="tag">标签键</param>
        /// <returns>数值，如果不存在则返回0</returns>
        public float GetSetByCallerMagnitude(GameplayTag tag)
        {
            return _valueMapWithTag.TryGetValue(tag, out var value) ? value : 0f;
        }
        
        /// <summary>
        /// 获取基于名称的SetByCaller数值
        /// </summary>
        /// <param name="name">名称键</param>
        /// <returns>数值，如果不存在则返回0</returns>
        public float GetSetByCallerMagnitude(string name)
        {
            return _valueMapWithName.TryGetValue(name, out var value) ? value : 0f;
        }
        
        private void TryActivateGrantedAbilities()
        {
            foreach (var grantedAbilitySpec in GrantedAbilitySpec)
            {
                if (grantedAbilitySpec.ActivationPolicy == GrantedAbilityActivationPolicy.SyncWithEffect)
                {
                    Owner.TryActivateAbility(grantedAbilitySpec.AbilityName);
                }
            }
        }

        private void TryDeactivateGrantedAbilities()
        {
            foreach (var grantedAbilitySpec in GrantedAbilitySpec)
            {
                if (grantedAbilitySpec.DeactivationPolicy == GrantedAbilityDeactivationPolicy.SyncWithEffect)
                {
                    Owner.TryEndAbility(grantedAbilitySpec.AbilityName);
                }
            }
        }

        private void TryRemoveGrantedAbilities()
        {
            foreach (var grantedAbilitySpec in GrantedAbilitySpec)
            {
                if (grantedAbilitySpec.RemovePolicy == GrantedAbilityRemovePolicy.SyncWithEffect)
                {
                    Owner.TryCancelAbility(grantedAbilitySpec.AbilityName);
                    Owner.RemoveAbility(grantedAbilitySpec.AbilityName);
                }
            }
        }

        #region ABOUT STACKING
        /// <summary>
        /// 刷新堆叠数（增加一层）
        /// </summary>
        /// <returns>如果堆叠数发生变化返回true</returns>
        /// <remarks>
        /// 将当前堆叠数+1，并根据堆叠规则处理以下逻辑：
        /// - 检查是否超过堆叠上限
        /// - 处理持续时间刷新
        /// - 处理周期重置
        /// - 触发溢出效果
        /// </remarks>
        public bool RefreshStack()
        {
            var oldStackCount = StackCount;
            RefreshStack(StackCount + 1);
            OnStackCountChange(oldStackCount, StackCount);
            return oldStackCount != StackCount;
        }
        
        public void RefreshStack(int stackCount)
        {
            if (stackCount <= Stacking.limitCount)
            {
                // 更新栈数
                StackCount = Mathf.Max(1,stackCount); // 最小层数为1
                // 是否刷新Duration
                if (Stacking.durationRefreshPolicy == DurationRefreshPolicy.RefreshOnSuccessfulApplication)
                {
                    RefreshDuration();
                }
                // 是否重置Period
                if (Stacking.periodResetPolicy == PeriodResetPolicy.ResetOnSuccessfulApplication)
                {
                    PeriodTicker.ResetPeriod();
                }
            }
            else
            {
                // 溢出GE生效
                foreach (var overflowEffect in Stacking.overflowEffects)
                    Owner.ApplyGameplayEffectToSelf(overflowEffect);

                if (Stacking.durationRefreshPolicy == DurationRefreshPolicy.RefreshOnSuccessfulApplication)
                {
                    if (Stacking.denyOverflowApplication)
                    {
                        //当DenyOverflowApplication为True是才有效，当Overflow时是否直接删除所有层数
                        if (Stacking.clearStackOnOverflow)
                        {
                            RemoveSelf();
                        }
                    }
                    else
                    {
                        RefreshDuration();
                    }
                }
            }
        }

        /// <summary>
        /// 刷新效果持续时间
        /// </summary>
        /// <remarks>
        /// 重新设置激活时间为当前时间，相当于重置效果计时器。
        /// 常用于堆叠效果的持续时间刷新。
        /// </remarks>
        public void RefreshDuration()
        {
            ActivationTime = Time.time;
        }
        
        private void OnStackCountChange(int oldStackCount, int newStackCount)
        {
            
            onStackCountChanged?.Invoke(oldStackCount, newStackCount);
        }
        
        /// <summary>
        /// 注册堆叠数变化回调
        /// </summary>
        /// <param name="callback">回调函数，参数为(旧堆叠数, 新堆叠数)</param>
        public void RegisterOnStackCountChanged(Action<int, int> callback)
        {
            onStackCountChanged += callback;
        }

        /// <summary>
        /// 注销堆叠数变化回调
        /// </summary>
        /// <param name="callback">要移除的回调函数</param>
        public void UnregisterOnStackCountChanged(Action<int, int> callback)
        {
            onStackCountChanged -= callback;
        }

        #endregion
    }
}