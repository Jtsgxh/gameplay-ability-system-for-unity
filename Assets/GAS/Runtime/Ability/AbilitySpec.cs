using System;

namespace GAS.Runtime
{
    /// <summary>
    /// 技能实例规格，定义技能的运行时行为和状态管理
    /// </summary>
    /// <remarks>
    /// AbilitySpec是技能系统的核心运行时组件，负责：
    /// - 技能的激活、执行、结束和取消逻辑
    /// - 技能状态管理（激活状态、激活次数等）
    /// - 技能条件检查（冷却、消耗、标签要求等）
    /// - 技能参数和用户数据管理
    /// - 技能生命周期事件回调
    /// 
    /// 每个具体的技能都需要继承此抽象类来实现特定的技能行为。
    /// AbilitySpec与AbstractAbility的关系类似于实例与类型的关系。
    /// </remarks>
    public abstract class AbilitySpec
    {
        /// <summary>
        /// 技能激活时传递的参数数组
        /// 存储调用TryActivateAbility时传入的所有参数，供技能执行期间使用
        /// </summary>
        protected object[] _abilityArguments = Array.Empty<object>();

        /// <summary>
        /// 获取激活能力时传递给能力的参数。
        /// </summary>
        /// <remarks>
        /// <para>该属性返回一个对象数组，表示激活能力时传入的参数。</para>
        /// <para>即使没有参数传递，该数组也绝不会是 <c>null</c>，在这种情况下，它将是一个空数组。</para>
        /// </remarks>
        public object[] AbilityArguments => _abilityArguments;

        /// <summary>
        /// 获取或设置与能力关联的自定义数据。
        /// </summary>
        /// <remarks>
        /// <para>此属性用于存储能力的自定义信息，以便在能力的不同任务之间共享数据。</para>
        /// <para>例如，可以在一个技能的任务(AbilityTask)中设置此数据，然后在同一个技能的另一个任务(AbilityTask)中检索和使用该数据。</para>
        /// </remarks>
        public object UserData { get; set; }


        /// <summary>
        /// 初始化技能实例规格
        /// </summary>
        /// <param name="ability">技能定义</param>
        /// <param name="owner">技能拥有者</param>
        public AbilitySpec(AbstractAbility ability, AbilitySystemComponent owner)
        {
            Ability = ability;
            Owner = owner;
        }

        /// <summary>
        /// 释放技能实例资源
        /// </summary>
        /// <remarks>
        /// 清理所有事件回调，防止内存泄漏。
        /// 通常在技能被移除或组件销毁时调用。
        /// </remarks>
        public virtual void Dispose()
        {
            // EventBus 清理会在 EventBus 本身处理，这里不需要手动清理
        }

        /// <summary>
        /// 获取技能定义数据
        /// </summary>
        public AbstractAbility Ability { get; }

        /// <summary>
        /// 获取技能拥有者组件
        /// </summary>
        public AbilitySystemComponent Owner { get; protected set; }

        /// <summary>
        /// 获取技能等级
        /// </summary>
        /// <remarks>
        /// 技能等级影响技能的威力、消耗、冷却等数值计算。
        /// </remarks>
        public int Level { get; protected set; }

        /// <summary>
        /// 检查技能是否处于激活状态
        /// </summary>
        /// <remarks>
        /// 激活状态的技能会接收Tick更新，并可能阻止其他技能的激活。
        /// </remarks>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 获取技能的总激活次数
        /// </summary>
        /// <remarks>
        /// 用于统计和某些需要激活次数条件的机制。
        /// </remarks>
        public int ActiveCount { get; private set; }


        /// <summary>
        /// 设置技能等级
        /// </summary>
        /// <param name="level">新的技能等级</param>
        /// <remarks>
        /// 技能等级影响伤害、治疗、消耗等数值的计算。
        /// 通常在技能升级或角色等级提升时调用。
        /// </remarks>
        public virtual void SetLevel(int level)
        {
            Level = level;
        }

        /// <summary>
        /// 检查技能是否可以激活
        /// </summary>
        /// <returns>激活结果，成功或失败原因</returns>
        /// <remarks>
        /// 检查流程：
        /// 1. 技能是否已激活
        /// 2. 标签要求是否满足
        /// 3. 消耗资源是否足够
        /// 4. 冷却时间是否结束
        /// 
        /// 子类可以重写此方法添加自定义激活条件。
        /// </remarks>
        public virtual AbilityActivateResult CanActivate()
        {
            if (IsActive) return AbilityActivateResult.FailHasActivated;
            if (!CheckGameplayTagsValidTpActivate()) return AbilityActivateResult.FailTagRequirement;
            if (!CheckSourceTags()) return AbilityActivateResult.FailSourceTagRequirement;
            if (!CheckCost()) return AbilityActivateResult.FailCost;
            if (CheckCooldown().TimeRemaining > 0) return AbilityActivateResult.FailCooldown;

            return AbilityActivateResult.Success;
        }
        
        /// <summary>
        /// 检查技能是否可以对目标激活
        /// </summary>
        /// <param name="target">目标 AbilitySystemComponent</param>
        /// <returns>激活结果</returns>
        public virtual AbilityActivateResult CanActivateOnTarget(AbilitySystemComponent target)
        {
            var result = CanActivate();
            if (result != AbilityActivateResult.Success) return result;
            
            if (target != null && !CheckTargetTags(target))
                return AbilityActivateResult.FailTargetTagRequirement;
                
            return AbilityActivateResult.Success;
        }

        private bool CheckGameplayTagsValidTpActivate()
        {
            var hasAllTags = Owner.HasAllTags(Ability.Tag.ActivationRequiredTags);
            var notHasAnyTags = !Owner.HasAnyTags(Ability.Tag.ActivationBlockedTags);
            var notBlockedByOtherAbility = true;

            foreach (var kv in Owner.AbilityContainer.AbilitySpecs())
            {
                var abilitySpec = kv.Value;
                if (abilitySpec.IsActive)
                    if (Ability.Tag.AssetTag.HasAnyTags(abilitySpec.Ability.Tag.BlockAbilitiesWithTags))
                    {
                        notBlockedByOtherAbility = false;
                        break;
                    }
            }

            return hasAllTags && notHasAnyTags && notBlockedByOtherAbility;
        }
        
        /// <summary>
        /// 检查源（施法者）标签要求
        /// </summary>
        private bool CheckSourceTags()
        {
            // 如果没有配置源标签要求，直接返回true
            if (Ability.Tag.SourceRequiredTags.Empty && Ability.Tag.SourceBlockedTags.Empty)
                return true;
                
            var hasRequired = Owner.HasAllTags(Ability.Tag.SourceRequiredTags);
            var notBlocked = !Owner.HasAnyTags(Ability.Tag.SourceBlockedTags);
            
            return hasRequired && notBlocked;
        }
        
        /// <summary>
        /// 检查目标标签要求
        /// </summary>
        /// <param name="target">目标 AbilitySystemComponent</param>
        private bool CheckTargetTags(AbilitySystemComponent target)
        {
            if (target == null) return true;
            
            // 如果没有配置目标标签要求，直接返回true
            if (Ability.Tag.TargetRequiredTags.Empty && Ability.Tag.TargetBlockedTags.Empty)
                return true;
                
            var hasRequired = target.HasAllTags(Ability.Tag.TargetRequiredTags);
            var notBlocked = !target.HasAnyTags(Ability.Tag.TargetBlockedTags);
            
            return hasRequired && notBlocked;
        }

        /// <summary>
        /// 检查技能消耗是否满足
        /// </summary>
        /// <returns>如果资源足够返回true</returns>
        /// <remarks>
        /// 验证是否有足够的资源（如魔法值、体力值）来使用技能。
        /// 支持加法和减法操作的消耗检查。
        /// </remarks>
        protected virtual bool CheckCost()
        {
            if (Ability.Cost == null) return true;
            var costSpec = Ability.Cost.CreateSpec(Owner, Owner, Level);
            if (costSpec == null) return false;

            if (Ability.Cost.DurationPolicy != EffectsDurationPolicy.Instant) return true;

            foreach (var modifier in Ability.Cost.Modifiers)
            {
                // 常规来说消耗是减法, 但是加一个负数也应该被视为减法
                if (modifier.Operation != GEOperation.Add && modifier.Operation != GEOperation.Minus) continue;

                var costValue = modifier.CalculateMagnitude(costSpec, modifier.ModiferMagnitude);
                var attributeCurrentValue =
                    Owner.GetAttributeCurrentValue(modifier.AttributeSetName, modifier.AttributeShortName);
                
                if(modifier.Operation == GEOperation.Add)
                    if (attributeCurrentValue + costValue < 0) return false;
                
                if(modifier.Operation == GEOperation.Minus)
                    if (attributeCurrentValue - costValue < 0) return false;
            }

            return true;
        }

        /// <summary>
        /// 检查技能冷却状态
        /// </summary>
        /// <returns>冷却计时器信息</returns>
        /// <remarks>
        /// 如果技能没有冷却效果，返回剩余时间为0的计时器。
        /// 否则通过标签系统检查冷却状态。
        /// </remarks>
        protected virtual CooldownTimer CheckCooldown()
        {
            return Ability.Cooldown == null
                ? new CooldownTimer { TimeRemaining = 0, Duration = Ability.CooldownTime }
                : Owner.CheckCooldownFromTags(Ability.Cooldown.TagContainer.GrantedTags);
        }

        /// <summary>
        /// 执行技能消耗和冷却
        /// </summary>
        /// <param name="customCooldownTime">自定义冷却时间，0表示使用默认冷却时间</param>
        /// <remarks>
        /// 某些技能包含前摇和后摇，前摇可能被打断导致技能未成功释放。
        /// 因此技能释放的实际时机和逻辑（触发消耗和冷却）应由开发者在AbilitySpec内决定，
        /// 而不是系统化标准化处理。
        /// 
        /// 此方法通常在技能确认释放时调用，而不是在激活检查时调用。
        /// </remarks>
        public virtual void DoCost(float customCooldownTime = 0)
        {
            if (Ability.Cost != null) Owner.ApplyGameplayEffectToSelf(Ability.Cost);

            if (Ability.Cooldown != null)
            {
                var cdSpec = Owner.ApplyGameplayEffectToSelf(Ability.Cooldown);
                cdSpec.SetDuration(customCooldownTime == 0? Ability.CooldownTime : customCooldownTime);
            }
        }

        /// <summary>
        /// 尝试激活技能
        /// </summary>
        /// <param name="args">传递给技能的参数</param>
        /// <returns>如果激活成功返回true</returns>
        /// <remarks>
        /// 激活流程：
        /// 1. 保存传入的参数
        /// 2. 检查激活条件
        /// 3. 如果条件满足，设置激活状态并应用动态标签
        /// 4. 调用具体的激活逻辑
        /// 5. 触发激活结果回调
        /// 
        /// 注意：此方法不会自动执行消耗和冷却，需要在合适的时机调用DoCost()。
        /// </remarks>
        public virtual bool TryActivateAbility(params object[] args)
        {
            _abilityArguments = args;
            var result = CanActivate();
            var success = result == AbilityActivateResult.Success;
            if (success)
            {
                IsActive = true;
                ActiveCount++;
                Owner.GameplayTagAggregator.ApplyGameplayAbilityDynamicTag(this);

                ActivateAbility(_abilityArguments);
            }

            // 通过EventBus发布技能激活结果事件
            if (Owner?.EventBus != null)
            {
                var eventName = success ? GameplayEvents.OnAbilityActivated : GameplayEvents.OnAbilityFailed;
                Owner.EventBus.Publish(eventName, Owner, null, null, 
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "abilityName", Ability.Name },
                        { "result", result }
                    });
            }
            
            return success;
        }

        /// <summary>
        /// 尝试正常结束技能
        /// </summary>
        /// <remarks>
        /// 结束流程：
        /// 1. 检查技能是否激活
        /// 2. 设置为非激活状态
        /// 3. 恢复动态标签
        /// 4. 调用具体的结束逻辑
        /// 5. 触发结束回调
        /// 
        /// 这是技能的正常结束路径，与取消不同。
        /// </remarks>
        public virtual void TryEndAbility()
        {
            if (!IsActive) return;
            IsActive = false;
            Owner.GameplayTagAggregator.RestoreGameplayAbilityDynamicTags(this);
            EndAbility();
            
            // 通过EventBus发布技能结束事件
            if (Owner?.EventBus != null)
            {
                Owner.EventBus.Publish(GameplayEvents.OnAbilityEnded, Owner, null, null,
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "abilityName", Ability.Name }
                    });
            }
        }

        /// <summary>
        /// 尝试取消技能
        /// </summary>
        /// <remarks>
        /// 取消流程：
        /// 1. 检查技能是否激活
        /// 2. 设置为非激活状态
        /// 3. 恢复动态标签
        /// 4. 调用具体的取消逻辑
        /// 5. 触发取消回调
        /// 
        /// 取消通常由外部条件触发（如眩晕、沉默等），与正常结束不同。
        /// </remarks>
        public virtual void TryCancelAbility()
        {
            if (!IsActive) return;
            IsActive = false;

            Owner.GameplayTagAggregator.RestoreGameplayAbilityDynamicTags(this);
            CancelAbility();
            
            // 通过EventBus发布技能取消事件
            if (Owner?.EventBus != null)
            {
                Owner.EventBus.Publish(GameplayEvents.OnAbilityCancelled, Owner, null, null,
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "abilityName", Ability.Name }
                    });
            }
        }

        /// <summary>
        /// 技能的帧更新
        /// </summary>
        /// <remarks>
        /// 仅在技能激活状态下会调用AbilityTick()。
        /// 由系统的更新循环自动调用。
        /// </remarks>
        public void Tick()
        {
            if (IsActive)
            {
                AbilityTick();
            }
        }

        /// <summary>
        /// 技能激活时的帧更新逻辑
        /// </summary>
        /// <remarks>
        /// 子类可以重写此方法实现技能的持续更新逻辑，
        /// 如持续施法、引导技能、持续伤害等。
        /// </remarks>
        protected virtual void AbilityTick()
        {
        }

        /// <summary>
        /// 激活技能的具体实现（抽象方法）
        /// </summary>
        /// <param name="args">传递给技能的参数</param>
        /// <remarks>
        /// 子类必须实现此方法来定义技能的具体行为。
        /// 在此方法中应该实现技能的主要逻辑。
        /// </remarks>
        public abstract void ActivateAbility(params object[] args);

        /// <summary>
        /// 取消技能的具体实现（抽象方法）
        /// </summary>
        /// <remarks>
        /// 子类必须实现此方法来定义技能被取消时的处理逻辑。
        /// 通常包括停止动画、清理临时效果等。
        /// </remarks>
        public abstract void CancelAbility();

        /// <summary>
        /// 结束技能的具体实现（抽象方法）
        /// </summary>
        /// <remarks>
        /// 子类必须实现此方法来定义技能正常结束时的处理逻辑。
        /// 通常包括播放结束动画、应用最终效果等。
        /// </remarks>
        public abstract void EndAbility();
    }

    /// <summary>
    /// 泛型技能实例规格，提供对特定类型技能的强类型访问
    /// </summary>
    /// <typeparam name="T">技能类型</typeparam>
    /// <remarks>
    /// 这个泛型版本提供了强类型的技能数据访问，避免了类型转换。
    /// 建议具体的技能实现继承此泛型版本。
    /// </remarks>
    public abstract class AbilitySpec<T> : AbilitySpec where T : AbstractAbility
    {
        /// <summary>
        /// 获取强类型的技能数据
        /// </summary>
        public T Data { get; private set; }

        /// <summary>
        /// 初始化泛型技能实例规格
        /// </summary>
        /// <param name="ability">技能定义</param>
        /// <param name="owner">技能拥有者</param>
        protected AbilitySpec(T ability, AbilitySystemComponent owner) : base(ability, owner)
        {
            Data = ability;
        }
    }
}