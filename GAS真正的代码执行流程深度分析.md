# Unity Gameplay Ability System 代码执行流程深度分析

## 整体架构数据流分析

┌───────────────────────────────────────────────────────────────────┐
│                    GAS 数据流与执行流程总览                        │
│                                                                   │
│  游戏循环 → GasHost.Update → GameplayAbilitySystem.Tick          │
│      ↓                                                            │
│  分发到所有 AbilitySystemComponent.Tick                          │
│      ↓                                                            │
│  并行执行四大子系统更新:                                          │
│  • AbilityContainer.Tick → 激活技能的帧更新                      │
│  • GameplayEffectContainer.Tick → 效果周期执行与过期检查         │
│  • AttributeSetContainer.Tick → 属性值重计算与依赖更新           │
│  • GameplayTagAggregator.Tick → 标签状态维护                    │
│      ↓                                                            │
│  事件触发链: 变化 → 验证 → 应用 → 通知 → 级联影响                │
└───────────────────────────────────────────────────────────────────┘

## 第一部分：技能激活完整数据流分析

┌───────────────────────────────────────────────────────────────────┐
│               技能激活的多层级验证与执行流程                       │
│                                                                   │
│  1. 触发阶段 (TryActivateAbility)                                │
│     输入: abilityName + args[]                                   │
│     数据查找: _abilitySpecs.FirstOrDefault(名称匹配)              │
│     验证点: 技能是否存在？                                        │
│                                                                   │
│  2. 条件验证阶段 (AbilitySpec.CanActivate)                       │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ a) 状态检查                                             │   │
│     │    数据源: IsActive, IsEnding, IsCancelling             │   │
│     │    逻辑: 单次激活技能防重入检查                         │   │
│     │                                                         │   │
│     │ b) 标签验证                                             │   │
│     │    数据源: _gameplayTagAggregator._tagCountMap          │   │
│     │    必需标签: HasAllTags(ActivationRequiredTags)         │   │
│     │    阻止标签: !HasAnyTags(ActivationBlockedTags)         │   │
│     │    复杂度: O(n) 其中 n = 标签总数                      │   │
│     │                                                         │   │
│     │ c) 资源消耗验证                                         │   │
│     │    数据流: Ability.Cost.Modifiers → 遍历每个修改器      │   │
│     │    计算链: AttributeName → GetCurrentValue → 比较消耗   │   │
│     │    验证点: 当前值 >= 消耗值 (所有资源)                  │   │
│     │                                                         │   │
│     │ d) 冷却检查                                             │   │
│     │    数据源: GameplayEffectContainer._gameplayEffectSpecs │   │
│     │    查找逻辑: 效果的GrantedTags ∩ 技能CooldownTags       │   │
│     │    时间计算: DurationRemaining() > 0 表示冷却中         │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  3. 执行阶段 (成功路径)                                           │
│     状态变更: IsActive = true, ActiveCount++                     │
│     标签应用: ApplyGameplayAbilityDynamicTag(DynamicTags)         │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 标签应用的级联效应:                                     │   │
│     │ DynamicTag添加 → _tagCountMap[tag]++                   │   │
│     │ → OnTagChanged(tag, true, count)                       │   │
│     │ → RefreshGameplayEffectState()                         │   │
│     │ → 遍历所有GE检查 CanRunning 条件                       │   │
│     │ → 可能激活/停用其他效果                                │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  4. 技能逻辑执行                                                  │
│     调用: abilitySpec.ActivateAbility(args)                      │
│     数据传递: args参数传入具体技能实现                            │
│                                                                   │
│  5. 消耗与冷却应用                                                │
│     消耗应用: ApplyGameplayEffectToSelf(Ability.Cost)             │
│     冷却应用: ApplyGameplayEffectToSelf(Ability.Cooldown)         │
│     数据流向: 新GE → GameplayEffectContainer.AddGameplayEffectSpec│
│                                                                   │
│  6. 冲突技能取消                                                  │
│     查找逻辑: CancelAbilitiesWithTags 匹配当前激活技能的标签     │
│     取消操作: 调用匹配技能的 CancelAbility()                     │
│                                                                   │
│  7. 事件发布                                                      │
│     事件类型: AbilityEvent(Activated, abilitySpec)                │
│     传播路径: ASC → GameplayEventBus → 所有订阅者                │
└───────────────────────────────────────────────────────────────────┘

## 第二部分：GameplayEffect 应用与管理的复杂数据流

┌───────────────────────────────────────────────────────────────────┐
│                GameplayEffect 的多阶段处理流程                    │
│                                                                   │
│  1. 效果应用入口 (ApplyGameplayEffectTo)                          │
│     参数流: source, target, gameplayEffect/spec, level            │
│     路由逻辑: target.GameplayEffectContainer.AddGameplayEffectSpec│
│                                                                   │
│  2. 前置验证阶段                                                  │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ CanApplyTo 检查:                                       │   │
│     │ - ApplicationRequiredTags: target.HasAllTags()         │   │
│     │ - ApplicationBlockedTags: !target.HasAnyTags()         │   │
│     │ 数据源: target._gameplayTagAggregator                   │   │
│     │                                                         │   │
│     │ IsImmune 检查:                                         │   │
│     │ - ImmunityTags: target.HasAnyTags(ImmunityTags)        │   │
│     │ 免疫触发: 返回null但可能触发免疫反馈                   │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  3. 持续时间策略分流                                              │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ Instant (即时效果):                                     │   │
│     │ 执行路径: Init → TriggerOnExecute → 立即销毁           │   │
│     │ TriggerOnExecute 内部流程:                              │   │
│     │ • RemoveGameplayEffectsWithTags(RemovalTags)            │   │
│     │ • ApplyModFromInstantGameplayEffect(spec)               │   │
│     │ • ExecutionCalculations 按优先级执行                   │   │
│     │ • TriggerCueOnExecute() 触发视听反馈                   │   │
│     │                                                         │   │
│     │ Duration/Infinite (持续效果):                           │   │
│     │ 数据持久化: 加入 _gameplayEffectSpecs 列表              │   │
│     │ 状态管理: 需要处理堆叠、周期执行、到期等               │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  4. 堆叠处理算法                                                  │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ None: 直接添加新实例                                   │   │
│     │ 数据结构: List<GameplayEffectSpec> 线性增长             │   │
│     │                                                         │   │
│     │ AggregateByTarget:                                      │   │
│     │ 查找算法: 遍历现有效果，比较 StackEqual()               │   │
│     │ 匹配条件: 相同的 GameplayEffect 定义                   │   │
│     │ 刷新操作: RefreshStack() → 重置持续时间和堆叠数        │   │
│     │                                                         │   │
│     │ AggregateBySource:                                      │   │
│     │ 查找算法: 检查 Source == source && StackEqual()        │   │
│     │ 数据关联: 同一个施法者的同类效果才能堆叠               │   │
│     │ 溢出处理: StackOverflowEffects 应用到目标              │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  5. 效果激活与应用                                                │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ Apply() 阶段:                                           │   │
│     │ • ApplyDynamicTags() → 标签聚合器更新                  │   │
│     │ • GrantAbilities() → 临时技能授予                      │   │
│     │ • RegisterPeriodTicker() → 周期执行注册                │   │
│     │ • SetupEventListeners() → 事件监听器注册               │   │
│     │                                                         │   │
│     │ Activate() 阶段:                                        │   │
│     │ • IsActive = true                                       │   │
│     │ • 开始影响属性计算 (AttributeAggregator)                │   │
│     │ • 周期计时器开始工作                                   │   │
│     └─────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────┘

## 第三部分：属性系统的依赖网络与计算链

┌───────────────────────────────────────────────────────────────────┐
│                  属性计算的多层级依赖处理                          │
│                                                                   │
│  1. 属性修改器的条件筛选                                          │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ GameplayEffectModifier.ShouldApplyModifier():           │   │
│     │                                                         │   │
│     │ 标签条件矩阵验证:                                       │   │
│     │ ┌─────────────────┬─────────────────┬─────────────────┐ │   │
│     │ │                 │ Source 检查     │ Target 检查     │ │   │
│     │ ├─────────────────┼─────────────────┼─────────────────┤ │   │
│     │ │ Required Tags   │ HasAllTags()    │ HasAllTags()    │ │   │
│     │ │ Ignored Tags    │ !HasAnyTags()   │ !HasAnyTags()   │ │   │
│     │ └─────────────────┴─────────────────┴─────────────────┘ │   │
│     │                                                         │   │
│     │ 性能优化: 如果所有标签数组为空，直接返回true            │   │
│     │ 数据源: source/target._gameplayTagAggregator            │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  2. 修改器数值计算链                                              │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ AttributeBasedModCalculation 的四种模式:                │   │
│     │                                                         │   │
│     │ Source + SnapShot:                                      │   │
│     │ 数据路径: spec.SnapshotSourceAttributes[attributeName]  │   │
│     │ 时机: 效果应用时捕获的属性值                           │   │
│     │                                                         │   │
│     │ Source + Track:                                         │   │
│     │ 数据路径: spec.Source.GetAttributeCurrentValue()        │   │
│     │ 时机: 每次计算时的实时值                               │   │
│     │                                                         │   │
│     │ Target + SnapShot:                                      │   │
│     │ 数据路径: spec.SnapshotTargetAttributes[attributeName]  │   │
│     │ 时机: 效果应用时捕获的目标属性值                       │   │
│     │                                                         │   │
│     │ Target + Track:                                         │   │
│     │ 数据路径: spec.Owner.GetAttributeCurrentValue()         │   │
│     │ 时机: 每次计算时的目标实时值                           │   │
│     │                                                         │   │
│     │ 计算公式统一: attributeValue * k + b                   │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  3. AttributeAggregator 的智能聚合算法                           │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 三种计算模式的数据处理:                                 │   │
│     │                                                         │   │
│     │ Stacking (累加模式):                                    │   │
│     │ 算法: foreach modifier: sum += CalculateMagnitude()     │   │
│     │ 复杂度: O(n) 其中 n = 修改器数量                       │   │
│     │ 数据结构: List<GameplayEffectModifier>                  │   │
│     │                                                         │   │
│     │ MinValueOnly (最小值模式):                              │   │
│     │ 算法: Math.Min(所有修改器计算结果)                      │   │
│     │ 用途: 护甲穿透、免伤等需要最严格限制的属性             │   │
│     │                                                         │   │
│     │ MaxValueOnly (最大值模式):                              │   │
│     │ 算法: Math.Max(所有修改器计算结果)                      │   │
│     │ 用途: 移动速度、攻击速度等有上限的属性                 │   │
│     │                                                         │   │
│     │ 依赖追踪系统:                                           │   │
│     │ 建立图: AttributeBasedModCalculation → 依赖的属性名     │   │
│     │ 触发器: 当被依赖属性变化时，自动重算依赖属性           │   │
│     │ 防循环: 深度优先搜索检测循环依赖                       │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  4. 属性变化的级联传播                                            │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 变化触发链:                                             │   │
│     │ AttributeBase.CurrentValue 变化                         │   │
│     │ → _onPostValueChange.Invoke(oldVal, newVal)             │   │
│     │ → AttributeSetContainer.OnAttributeChanged()            │   │
│     │ → ASC.OnAttributeChanged事件                            │   │
│     │ → PublishAttributeEvent()                               │   │
│     │ → GameplayEventBus 全局广播                            │   │
│     │ → 所有订阅者接收 AttributeChangedEvent                  │   │
│     │                                                         │   │
│     │ 依赖重计算触发:                                         │   │
│     │ 属性A变化 → 检查依赖图 → 找到依赖A的属性B、C           │   │
│     │ → 异步/同步重新计算B、C → 可能再次触发依赖链           │   │
│     └─────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────┘

## 第四部分：事件系统的异步传播网络

┌───────────────────────────────────────────────────────────────────┐
│                    GameplayEventBus 事件处理机制                  │
│                                                                   │
│  1. 线程安全的发布订阅实现                                        │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 数据结构: Dictionary<Type, List<Delegate>>              │   │
│     │ 锁机制: lock (_lockObject) 确保线程安全                │   │
│     │                                                         │   │
│     │ Publish<T> 流程:                                        │   │
│     │ 1. 获取锁                                               │   │
│     │ 2. 查找类型T的所有处理器                               │   │
│     │ 3. 遍历调用 handler.Invoke(eventData)                   │   │
│     │ 4. 释放锁                                               │   │
│     │                                                         │   │
│     │ 性能考虑:                                               │   │
│     │ - 处理器调用在锁内进行（可能阻塞）                     │   │
│     │ - 大量订阅者时性能下降                                 │   │
│     │ - 异常处理器会影响后续处理器                           │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  2. ASC 的便捷事件发布接口                                        │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ PublishGameplayEvent():                                 │   │
│     │ 参数: eventType, instigator, target, eventData          │   │
│     │ 用途: 通用游戏事件，自定义数据负载                     │   │
│     │                                                         │   │
│     │ PublishDamageEvent():                                   │   │
│     │ 参数: damage, source, target, gameplayEffect           │   │
│     │ 数据流: 伤害计算完成 → 伤害应用 → 伤害反馈             │   │
│     │                                                         │   │
│     │ PublishHealingEvent():                                  │   │
│     │ 参数: healing, source, target, gameplayEffect          │   │
│     │ 数据流: 治疗计算 → 治疗应用 → 治疗反馈                 │   │
│     │                                                         │   │
│     │ PublishAbilityEvent():                                  │   │
│     │ 参数: eventType, abilitySpec                            │   │
│     │ 事件类型: Granted, Activated, Ended, Cancelled, Failed  │   │
│     │                                                         │   │
│     │ PublishGameplayEffectEvent():                           │   │
│     │ 参数: eventType, effectSpec                             │   │
│     │ 事件类型: Applied, Removed, StackChanged               │   │
│     │                                                         │   │
│     │ PublishTagEvent():                                      │   │
│     │ 参数: tag, added, stackCount                            │   │
│     │ 用途: 标签状态变化通知                                 │   │
│     │                                                         │   │
│     │ PublishAttributeEvent():                                │   │
│     │ 参数: attributeName, oldValue, newValue                 │   │
│     │ 触发时机: 任何属性值发生变化                           │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  3. GameplayEffectSpec 的条件事件监听                            │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ SetupEventListeners() 详细流程:                         │   │
│     │                                                         │   │
│     │ 1. 从 ExecutionCalculations 收集条件:                   │   │
│     │    foreach calculation in ExecutionCalculations:       │   │
│     │        foreach condition in calculation.Conditions:     │   │
│     │            eventType = condition.EventType             │   │
│     │            GameplayEventBus.Subscribe<eventType>(handler)│   │
│     │                                                         │   │
│     │ 2. 事件处理器逻辑:                                       │   │
│     │    OnEventReceived(eventData):                          │   │
│     │        if (condition.ShouldExecute(eventData)):         │   │
│     │            condition.TriggerEvent(this, eventData)      │   │
│     │            CheckAndExecuteConditionalCalculations()     │   │
│     │                                                         │   │
│     │ 3. 条件匹配算法:                                         │   │
│     │    - 事件类型匹配                                       │   │
│     │    - 标签条件检查                                       │   │
│     │    - 数值范围检查                                       │   │
│     │    - 自定义逻辑验证                                     │   │
│     │                                                         │   │
│     │ 4. 执行结果:                                             │   │
│     │    - 可能修改属性值                                     │   │
│     │    - 可能应用新的 GameplayEffect                        │   │
│     │    - 可能移除现有效果                                   │   │
│     │    - 可能触发新的事件                                   │   │
│     └─────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────┘

## 第五部分：标签系统的状态管理与查询优化

┌───────────────────────────────────────────────────────────────────┐
│                GameplayTagAggregator 核心算法分析                 │
│                                                                   │
│  1. 标签存储与计数机制                                            │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 数据结构: Dictionary<GameplayTag, int> _tagCountMap     │   │
│     │                                                         │   │
│     │ 设计优势:                                               │   │
│     │ - O(1) 查找复杂度                                       │   │
│     │ - 自动处理标签堆叠                                     │   │
│     │ - 内存高效（只存储存在的标签）                         │   │
│     │                                                         │   │
│     │ 标签分类管理:                                           │   │
│     │ FixedTags: 通过 AddFixedTag/RemoveFixedTag 管理         │   │
│     │ DynamicTags: 通过技能/效果临时添加，自动堆叠计数        │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  2. 动态标签应用的级联效应                                        │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ ApplyGameplayAbilityDynamicTag() 流程:                  │   │
│     │                                                         │   │
│     │ foreach tag in dynamicTags:                             │   │
│     │     oldCount = _tagCountMap.GetValueOrDefault(tag, 0)   │   │
│     │     _tagCountMap[tag] = oldCount + 1                    │   │
│     │     if (oldCount == 0): // 从无到有                    │   │
│     │         OnTagChanged(tag, true, 1)                      │   │
│     │         触发级联效应 ↓                                  │   │
│     │                                                         │   │
│     │ 级联效应触发链:                                         │   │
│     │ OnTagChanged → RefreshGameplayEffectState()             │   │
│     │ → 遍历所有 GameplayEffectSpec                           │   │
│     │ → 检查每个效果的 CanRunning() 条件                      │   │
│     │ → 状态变化: Activate() 或 Deactivate()                 │   │
│     │ → 可能影响属性计算                                     │   │
│     │ → 可能触发更多事件                                     │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  3. 标签查询算法优化                                              │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ HasTag(tag) 实现:                                       │   │
│     │ return _tagCountMap.ContainsKey(tag) && 
│     │        _tagCountMap[tag] > 0                            │   │
│     │ 复杂度: O(1)                                            │   │
│     │                                                         │   │
│     │ HasAllTags(tagSet) 实现:                                │   │
│     │ foreach tag in tagSet.Tags:                             │   │
│     │     if (!HasTag(tag)) return false                     │   │
│     │ return true                                             │   │
│     │ 复杂度: O(m) 其中 m = tagSet 大小                      │   │
│     │                                                         │   │
│     │ HasAnyTags(tagSet) 实现:                                │   │
│     │ foreach tag in tagSet.Tags:                             │   │
│     │     if (HasTag(tag)) return true                       │   │
│     │ return false                                            │   │
│     │ 复杂度: O(m)，但平均情况下可能提前返回                 │   │
│     │                                                         │   │
│     │ 性能优化策略:                                           │   │
│     │ - 小标签集合直接遍历                                   │   │
│     │ - 大标签集合可考虑位掩码优化                           │   │
│     │ - 缓存常用查询结果                                     │   │
│     └─────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────┘

## 第六部分：Timeline技能系统的时序控制机制

┌───────────────────────────────────────────────────────────────────┐
│                TimelineAbility 的精确时序管理                     │
│                                                                   │
│  1. TimelineAbilityPlayer 的播放控制                             │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 播放器生命周期:                                         │   │
│     │ 创建: new TimelineAbilityPlayer<T>(abilitySpec)         │   │
│     │ 关联: 双向引用 Player ↔ AbilitySpec                    │   │
│     │                                                         │   │
│     │ Play() 启动流程:                                        │   │
│     │ 1. 获取 Timeline 资产从 AbilityAsset                    │   │
│     │ 2. 查找或创建 PlayableDirector                          │   │
│     │ 3. 设置 Timeline 绑定:                                  │   │
│     │    - 角色动画器绑定                                    │   │
│     │    - 技能目标绑定 (Target)                             │   │
│     │    - 特效节点绑定                                      │   │
│     │ 4. 配置 PlayableDirector:                               │   │
│     │    director.playableAsset = timelineAsset              │   │
│     │    director.timeUpdateMode = Manual/GameTime           │   │
│     │ 5. 开始播放: director.Play()                            │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  2. Timeline 轨道类型与数据绑定                                   │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ Animation Track (动画轨道):                             │   │
│     │ 绑定对象: Animator component                            │   │
│     │ 数据流: Timeline → AnimationClip → Animator             │   │
│     │ 控制: 角色动作序列，精确到帧                           │   │
│     │                                                         │   │
│     │ Task Track (任务轨道):                                  │   │
│     │ 自定义实现: 执行特定的技能逻辑                         │   │
│     │ 时机控制: 在特定时间点触发代码逻辑                     │   │
│     │ 数据传递: 可访问 AbilitySpec 和 Target                 │   │
│     │                                                         │   │
│     │ Effect Track (效果轨道):                                │   │
│     │ 功能: 在指定时间应用 GameplayEffect                     │   │
│     │ 目标选择: 可以是 Self、Target 或自定义查找              │   │
│     │ 数据流: Timeline → GameplayEffect → EffectContainer    │   │
│     │                                                         │   │
│     │ Cue Track (反馈轨道):                                   │   │
│     │ 功能: 播放音效、特效、UI反馈                           │   │
│     │ 同步性: 与动画和逻辑精确同步                           │   │
│     │ 资源管理: 自动加载和卸载音效/特效资源                 │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  3. 帧更新与时间同步机制                                          │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ AbilityTick() 中的 Timeline 驱动:                       │   │
│     │                                                         │   │
│     │ Profiler.BeginSample("TimelineAbilitySpecT<T>::AbilityTick()")│   │
│     │ _player.Tick() 详细流程:                                │   │
│     │                                                         │   │
│     │ 1. 检查 PlayableDirector 状态                           │   │
│     │    if (!director.isActiveAndEnabled) return            │   │
│     │                                                         │   │
│     │ 2. 更新播放时间 (Manual模式):                           │   │
│     │    director.time += Time.deltaTime                     │   │
│     │    director.Evaluate() // 强制更新                     │   │
│     │                                                         │   │
│     │ 3. 处理 Timeline 事件:                                  │   │
│     │    - SignalReceiver 接收信号                           │   │
│     │    - Marker 标记点触发                                 │   │
│     │    - Track 轨道状态变化                                │   │
│     │                                                         │   │
│     │ 4. 检查播放完成:                                        │   │
│     │    if (director.time >= director.duration):            │   │
│     │        OnTimelineComplete()                            │   │
│     │        可能自动调用 EndAbility()                       │   │
│     │                                                         │   │
│     │ 性能监控: Unity Profiler 标记整个过程                  │   │
│     │ Profiler.EndSample()                                   │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  4. 目标管理与动态更新                                            │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ SetAbilityTarget(mainTarget) 流程:                      │   │
│     │                                                         │   │
│     │ 1. 存储目标引用: Target = mainTarget                    │   │
│     │                                                         │   │
│     │ 2. 更新 Timeline 绑定:                                  │   │
│     │    - 查找需要目标的轨道                                │   │
│     │    - 重新绑定 Transform/Component                       │   │
│     │    - 刷新 Playable Graph                               │   │
│     │                                                         │   │
│     │ 3. 与 TargetCatcher 系统协作:                           │   │
│     │    - 接收目标捕获事件                                  │   │
│     │    - 动态切换技能目标                                  │   │
│     │    - 支持多目标技能                                    │   │
│     │                                                         │   │
│     │ 使用场景:                                               │   │
│     │ - 单体攻击技能的目标设定                               │   │
│     │ - 向性范围技能的中心点                                 │   │
│     │ - 投射物技能的飞行终点                                 │   │
│     │ - 连击技能的连续目标                                   │   │
│     └─────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────┘

## 第七部分：GameplayEffectContainer 的高效管理算法

┌───────────────────────────────────────────────────────────────────┐
│            GameplayEffectContainer 的智能容器管理                 │
│                                                                   │
│  1. 安全的并发更新机制                                            │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ Tick() 方法的缓存策略:                                   │   │
│     │                                                         │   │
│     │ _cachedGameplayEffectSpecs.Clear()                      │   │
│     │ _cachedGameplayEffectSpecs.AddRange(_gameplayEffectSpecs)│   │
│     │                                                         │   │
│     │ 设计目的:                                               │   │
│     │ - 避免遍历过程中集合被修改                             │   │
│     │ - 支持效果在 Tick 中移除自己                           │   │
│     │ - 支持效果在 Tick 中添加新效果                         │   │
│     │                                                         │   │
│     │ 更新流程:                                               │   │
│     │ foreach effectSpec in _cachedGameplayEffectSpecs:       │   │
│     │     if (effectSpec.IsActive):                          │   │
│     │         effectSpec.Tick()                              │   │
│     │         可能的副作用:                                  │   │
│     │         - 效果到期自动移除                             │   │
│     │         - 周期执行触发新效果                           │   │
│     │         - 属性变化影响其他效果                         │   │
│     │                                                         │   │
│     │ _cachedGameplayEffectSpecs.Clear() // 清理缓存          │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  2. 标签匹配的批量移除算法                                        │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ RemoveGameplayEffectWithAnyTags() 优化实现:             │   │
│     │                                                         │   │
│     │ 输入验证:                                               │   │
│     │ if (tags.Empty) return // 早期退出                     │   │
│     │                                                         │   │
│     │ 两阶段扫描算法:                                         │   │
│     │ Phase 1 - 收集待移除效果:                               │   │
│     │ var removeList = new List<GameplayEffectSpec>()         │   │
│     │ foreach effectSpec in _gameplayEffectSpecs:             │   │
│     │     // 检查 AssetTags                                  │   │
│     │     if (!assetTags.Empty && assetTags.HasAnyTags(tags)):│   │
│     │         removeList.Add(effectSpec)                     │   │
│     │         continue // 避免重复添加                       │   │
│     │     // 检查 GrantedTags                                │   │
│     │     if (!grantedTags.Empty && grantedTags.HasAnyTags(tags)):│   │
│     │         removeList.Add(effectSpec)                     │   │
│     │                                                         │   │
│     │ Phase 2 - 批量移除:                                     │   │
│     │ foreach effectSpec in removeList:                       │   │
│     │     RemoveGameplayEffectSpec(effectSpec)               │   │
│     │                                                         │   │
│     │ 性能分析:                                               │   │
│     │ 时间复杂度: O(n*m) n=效果数量, m=平均标签数量           │   │
│     │ 空间复杂度: O(k) k=匹配的效果数量                      │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  3. 冷却检查的高效查询算法                                        │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ CheckCooldownFromTags() 的智能查找:                     │   │
│     │                                                         │   │
│     │ 初始化:                                                 │   │
│     │ float longestCooldown = 0                               │   │
│     │ float maxDuration = 0                                   │   │
│     │                                                         │   │
│     │ 嵌套循环优化:                                           │   │
│     │ foreach spec in _gameplayEffectSpecs:                   │   │
│     │     if (!spec.IsActive) continue // 早期过滤            │   │
│     │     var grantedTags = spec.GrantedTags                  │   │
│     │     if (grantedTags.Empty) continue // 早期过滤         │   │
│     │                                                         │   │
│     │     // 双重循环查找标签交集                             │   │
│     │     foreach grantedTag in grantedTags.Tags:             │   │
│     │         foreach targetTag in tags.Tags:                 │   │
│     │             if (grantedTag == targetTag):               │   │
│     │                 // 无限效果特殊处理                    │   │
│     │                 if (spec.DurationPolicy == Infinite):   │   │
│     │                     return CooldownTimer{-1, 0}        │   │
│     │                 // 更新最长冷却时间                    │   │
│     │                 var remaining = spec.DurationRemaining()│   │
│     │                 if (remaining > longestCooldown):       │   │
│     │                     longestCooldown = remaining         │   │
│     │                     maxDuration = spec.Duration        │   │
│     │                                                         │   │
│     │ 返回结果:                                               │   │
│     │ return CooldownTimer{longestCooldown, maxDuration}      │   │
│     │                                                         │   │
│     │ 优化策略:                                               │   │
│     │ - 使用 HashSet 进行标签交集计算                        │   │
│     │ - 缓存常用的冷却查询结果                               │   │
│     │ - 预先过滤无关的效果                                   │   │
│     └─────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────┘

## 第八部分：硬编码事件系统的完整触发机制分析

┌───────────────────────────────────────────────────────────────────┐
│                GameplayEffectSpec 的九大硬编码事件钩子             │
│                                                                   │
│  1. TriggerOnExecute() - 即时效果执行事件                         │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 触发时机: 即时效果(Instant)应用时                       │   │
│     │ 执行顺序:                                               │   │
│     │ 1. RemoveGameplayEffectsWithTags() 移除冲突效果         │   │
│     │ 2. ApplyModFromInstantGameplayEffect() 应用属性修改     │   │
│     │ 3. ExecutionCalculations 按优先级执行:                  │   │
│     │    - 筛选: execution != null                           │   │
│     │    - 排序: OrderByDescending(GetExecutionPriority())   │   │
│     │    - 条件: ShouldExecute(executionParams)              │   │
│     │    - 执行: execution.Execute(executionParams)          │   │
│     │ 4. TriggerCueOnExecute() 触发即时视听反馈               │   │
│     │                                                         │   │
│     │ 关键数据结构:                                           │   │
│     │ GameplayEffectCustomExecutionParameters:                │   │
│     │ - Source: AbilitySystemComponent                        │   │
│     │ - Target: AbilitySystemComponent                        │   │
│     │ - EffectSpec: GameplayEffectSpec                        │   │
│     │ - Level: int                                            │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  2. TriggerOnAdd() - 效果添加到容器时                             │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 触发时机: 持续/无限效果加入GameplayEffectContainer时    │   │
│     │ 执行内容:                                               │   │
│     │ - TriggerCueOnAdd() 播放添加时的视听反馈                │   │
│     │ - 不执行修改器或ExecutionCalculation                    │   │
│     │ - 主要用于UI反馈和状态指示                              │   │
│     │                                                         │   │
│     │ Cue类型处理:                                            │   │
│     │ - CueOnAdd[] 数组遍历执行                               │   │
│     │ - 每个Cue调用其OnAdd()方法                              │   │
│     │ - 可能包括特效、音效、UI提示等                          │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  3. TriggerOnRemove() - 效果从容器移除时                          │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 触发时机: 效果到期、被主动移除或被其他效果驱逐时        │   │
│     │ 执行顺序:                                               │   │
│     │ 1. TriggerCueOnRemove() 播放移除反馈                    │   │
│     │ 2. TryRemoveGrantedAbilities() 移除授予的技能           │   │
│     │ 3. CleanupEventListeners() 清理事件监听器               │   │
│     │                                                         │   │
│     │ 清理范围:                                               │   │
│     │ - 停止所有持续性Cue                                    │   │
│     │ - 注销GameplayEventBus事件监听                          │   │
│     │ - 释放引用避免内存泄漏                                  │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  4. TriggerOnActivation() - 效果激活时                            │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 触发时机: 效果的IsActive从false变为true时               │   │
│     │ 执行内容:                                               │   │
│     │ 1. TriggerCueOnActivation() 激活反馈                    │   │
│     │ 2. ApplyGameplayEffectDynamicTag() 应用动态标签         │   │
│     │ 3. RemoveGameplayEffectsWithTags() 移除冲突效果         │   │
│     │ 4. TryActivateGrantedAbilities() 激活授予的技能         │   │
│     │                                                         │   │
│     │ 标签影响链:                                             │   │
│     │ 动态标签添加 → TagAggregator.OnTagChanged               │   │
│     │ → RefreshGameplayEffectState() → 其他效果状态检查       │   │
│     │ → 可能级联激活/停用更多效果                             │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  5. TriggerOnDeactivation() - 效果停用时                         │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 触发时机: 效果的IsActive从true变为false时               │   │
│     │ 执行内容:                                               │   │
│     │ 1. TriggerCueOnDeactivation() 停用反馈                  │   │
│     │ 2. RestoreGameplayEffectDynamicTags() 移除动态标签      │   │
│     │ 3. TryDeactivateGrantedAbilities() 停用授予的技能       │   │
│     │                                                         │   │
│     │ 注意: 停用≠移除                                         │   │
│     │ - 效果仍在容器中，只是暂时不生效                        │   │
│     │ - 当条件满足时可以重新激活                             │   │
│     │ - 持续时间仍在计算                                     │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  6. TriggerOnTick() - 每帧更新时                                  │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 触发时机: GameplayEffectContainer.Tick()中每帧调用      │   │
│     │ 执行条件: DurationPolicy == Duration/Infinite           │   │
│     │ 执行内容:                                               │   │
│     │ - CueOnTick() 更新持续性Cue                             │   │
│     │ - 每个DurationalCue调用OnTick()                         │   │
│     │                                                         │   │
│     │ 持续性Cue的OnTick()实现:                                │   │
│     │ - CueVFX: 更新粒子系统状态                              │   │
│     │ - CueAnimation: 更新动画混合权重                        │   │
│     │ - CueAnimationSpeedModifier: 动态调整动画速度           │   │
│     │ - CuePlaySound: 更新音频参数                            │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  7. TriggerOnPeriodExecute() - 周期执行时                         │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 触发时机: PeriodTicker达到周期间隔时                    │   │
│     │ 筛选条件: execution.SupportsPeriodExecution() == true   │   │
│     │ 执行流程:                                               │   │
│     │ 1. 筛选支持周期执行的ExecutionCalculation               │   │
│     │ 2. 按优先级排序 OrderByDescending()                    │   │
│     │ 3. 逐个检查 ShouldExecute() 条件                       │   │
│     │ 4. 满足条件则执行 Execute()                             │   │
│     │                                                         │   │
│     │ 应用场景:                                               │   │
│     │ - DOT/HOT效果的周期性伤害/治疗                          │   │
│     │ - 周期性的属性检查和调整                               │   │
│     │ - 定时触发的特殊效果                                   │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  8. 事件条件触发 (Event-Driven ExecutionCalculation)             │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 初始化: SetupEventListeners()                           │   │
│     │ 扫描所有ExecutionCalculation的EventExecutionCondition   │   │
│     │ 为每个事件类型注册GameplayEventBus监听器                │   │
│     │                                                         │   │
│     │ 事件处理流程:                                           │   │
│     │ 1. GameplayEventBus收到事件                             │   │
│     │ 2. OnEventReceived(eventData)被调用                     │   │
│     │ 3. 检查事件条件: condition.ShouldTrigger(eventData)     │   │
│     │ 4. 满足条件: condition.TriggerEvent(spec, eventData)    │   │
│     │ 5. CheckAndExecuteConditionalCalculations()             │   │
│     │                                                         │   │
│     │ 支持的事件类型:                                         │   │
│     │ - DamageEvent: 伤害造成/受到时触发                      │   │
│     │ - HealingEvent: 治疗施放/接受时触发                     │   │
│     │ - AttributeChangedEvent: 特定属性变化时触发             │   │
│     │ - TagEvent: 特定标签添加/移除时触发                     │   │
│     │ - AbilityEvent: 技能激活/结束时触发                     │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  9. Timeline中的Task事件 (TimelineAbilityPlayer)                  │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ InstantTask.OnExecute():                                │   │
│     │ - 在Timeline特定时间点触发                              │   │
│     │ - DefaultInstantAbilityTask: 空实现，供继承              │   │
│     │ - ApplyCostAndCoolDown: 应用消耗和冷却                  │   │
│     │                                                         │   │
│     │ OngoingTask.OnTick():                                   │   │
│     │ - 持续性Task的每帧更新                                 │   │
│     │ - 参数: frameIndex, startFrame, endFrame               │   │
│     │ - 可实现复杂的持续性逻辑                               │   │
│     │                                                         │   │
│     │ DurationalCue事件:                                      │   │
│     │ - OnAdd(): Cue开始播放时                               │   │
│     │ - OnTick(): Cue播放过程中每帧                          │   │
│     │ - OnRemove(): Cue播放结束时                             │   │
│     │                                                         │   │
│     │ 注意: Timeline中的Cue不执行GameplayEffect相关方法       │   │
│     └─────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────┘

## 第九部分：ASC发布事件接口的完整数据流分析

┌───────────────────────────────────────────────────────────────────┐
│                AbilitySystemComponent 六大事件发布接口             │
│                                                                   │
│  1. PublishGameplayEvent() - 通用游戏事件发布                     │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 方法签名:                                               │   │
│     │ PublishGameplayEvent(string eventName,                  │   │
│     │     AbilitySystemComponent target,                      │   │
│     │     GameplayTag[] eventTags,                            │   │
│     │     params object[] parameters)                         │   │
│     │                                                         │   │
│     │ 数据封装过程:                                           │   │
│     │ 1. eventName作为事件类型标识                            │   │
│     │ 2. this作为事件发起者(instigator)                       │   │
│     │ 3. target作为事件目标                                   │   │
│     │ 4. eventTags提供事件分类信息                            │   │
│     │ 5. parameters携带自定义数据负载                         │   │
│     │                                                         │   │
│     │ 路由路径: ASC → GameplayEventBus → 所有订阅者           │   │
│     │ 用途: 自定义游戏逻辑事件，最灵活的事件接口             │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  2. PublishDamageEvent() - 伤害事件专用接口                       │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 方法签名:                                               │   │
│     │ PublishDamageEvent(float damageAmount,                  │   │
│     │     AbilitySystemComponent target,                      │   │
│     │     GameplayTag? damageType = null,                     │   │
│     │     bool isDealt = true)                                │   │
│     │                                                         │   │
│     │ 事件名称生成逻辑:                                       │   │
│     │ string eventName = isDealt ? "Damage.Dealt" : "Damage.Taken"│   │
│     │                                                         │   │
│     │ 标签数组构建:                                           │   │
│     │ eventTags = ["Damage"]                                  │   │
│     │ if (damageType.HasValue)                                │   │
│     │     eventTags.Add(damageType.Value)                     │   │
│     │                                                         │   │
│     │ 参数数组:                                               │   │
│     │ parameters = [damageAmount, target.gameObject]          │   │
│     │                                                         │   │
│     │ 应用场景:                                               │   │
│     │ - 伤害计算完成后的通知                                 │   │
│     │ - 伤害统计和记录                                       │   │
│     │ - 反击机制触发                                         │   │
│     │ - UI血条更新                                           │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  3. PublishHealingEvent() - 治疗事件专用接口                      │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 设计模式: 与PublishDamageEvent()完全对称                │   │
│     │                                                         │   │
│     │ 事件名称: "Healing.Dealt" / "Healing.Taken"             │   │
│     │ 标签构建: ["Healing"] + 可选的healType标签              │   │
│     │ 参数传递: [healAmount, target.gameObject]               │   │
│     │                                                         │   │
│     │ 数据一致性:                                             │   │
│     │ - healAmount统一为正值表示治疗量                        │   │
│     │ - 与伤害事件形成完整的生命值变化监控                   │   │
│     │ - 支持相同的事件处理逻辑                               │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  4. PublishAbilityEvent() - 技能状态事件                          │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 方法签名:                                               │   │
│     │ PublishAbilityEvent(string abilityName,                 │   │
│     │     string eventType,                                   │   │
│     │     AbilitySystemComponent target = null)               │   │
│     │                                                         │   │
│     │ 参数构建:                                               │   │
│     │ parameters = [abilityName, this.gameObject]             │   │
│     │ if (target != null)                                     │   │
│     │     parameters.Add(target.gameObject)                   │   │
│     │                                                         │   │
│     │ 常用eventType值:                                        │   │
│     │ - "Ability.Granted": 技能授予                           │   │
│     │ - "Ability.Activated": 技能激活                         │   │
│     │ - "Ability.Ended": 技能正常结束                         │   │
│     │ - "Ability.Cancelled": 技能被取消                       │   │
│     │ - "Ability.Failed": 技能激活失败                        │   │
│     │                                                         │   │
│     │ 监听应用:                                               │   │
│     │ - UI技能按钮状态更新                                   │   │
│     │ - 技能连击系统                                         │   │
│     │ - 成就系统统计                                         │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  5. PublishTagEvent() - 标签状态变化事件                          │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 方法签名:                                               │   │
│     │ PublishTagEvent(GameplayTag tag,                        │   │
│     │     bool isAdded, int stackCount = 1)                   │   │
│     │                                                         │   │
│     │ 事件名称生成:                                           │   │
│     │ string eventName = isAdded ? "Tag.Added" : "Tag.Removed"│   │
│     │                                                         │   │
│     │ 特殊处理:                                               │   │
│     │ eventTags = [tag] // 标签本身作为事件标签               │   │
│     │ parameters = [tag.Name, stackCount, this.gameObject]    │   │
│     │                                                         │   │
│     │ 触发时机:                                               │   │
│     │ - GameplayTagAggregator.OnTagChanged调用时              │   │
│     │ - 技能动态标签应用/移除时                               │   │
│     │ - GameplayEffect动态标签变化时                          │   │
│     │                                                         │   │
│     │ 级联效应:                                               │   │
│     │ 标签变化 → TagEvent → 可能触发其他系统响应              │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  6. PublishGameplayEffectEvent() - 效果状态事件                   │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 方法签名:                                               │   │
│     │ PublishGameplayEffectEvent(GameplayEffectSpec effectSpec,│   │
│     │     string eventType)                                   │   │
│     │                                                         │   │
│     │ 参数封装:                                               │   │
│     │ parameters = [                                          │   │
│     │     effectSpec.GameplayEffect.GameplayEffectName,       │   │
│     │     effectSpec.Source.gameObject,                       │   │
│     │     this.gameObject, // target                          │   │
│     │     effectSpec.Level,                                   │   │
│     │     effectSpec.StackCount                               │   │
│     │ ]                                                       │   │
│     │                                                         │   │
│     │ 常用eventType值:                                        │   │
│     │ - "GameplayEffect.Applied": 效果应用                    │   │
│     │ - "GameplayEffect.Removed": 效果移除                    │   │
│     │ - "GameplayEffect.StackChanged": 堆叠数变化             │   │
│     │ - "GameplayEffect.Activated": 效果激活                  │   │
│     │ - "GameplayEffect.Deactivated": 效果停用                │   │
│     │                                                         │   │
│     │ 丰富的上下文信息:                                       │   │
│     │ - 效果名称便于识别                                     │   │
│     │ - Source和Target信息完整                                │   │
│     │ - Level和StackCount提供数值信息                         │   │
│     └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  7. PublishAttributeEvent() - 属性变化事件                        │
│     ┌─────────────────────────────────────────────────────────┐   │
│     │ 方法签名:                                               │   │
│     │ PublishAttributeEvent(string attributeName,             │   │
│     │     float oldValue, float newValue,                     │   │
│     │     string eventType = null)                            │   │
│     │                                                         │   │
│     │ 事件名称逻辑:                                           │   │
│     │ string finalEventType = eventType ??                    │   │
│     │     "Attribute.Changed." + attributeName                │   │
│     │                                                         │   │
│     │ 参数详情:                                               │   │
│     │ parameters = [                                          │   │
│     │     attributeName,                                      │   │
│     │     oldValue,                                           │   │
│     │     newValue,                                           │   │
│     │     newValue - oldValue, // delta                       │   │
│     │     this.gameObject                                     │   │
│     │ ]                                                       │   │
│     │                                                         │   │
│     │ 自动触发链:                                             │   │
│     │ AttributeBase变化 → AttributeSetContainer               │   │
│     │ → ASC.OnAttributeChanged → PublishAttributeEvent       │   │
│     │                                                         │   │
│     │ 高频事件优化考虑:                                       │   │
│     │ - 属性变化频繁，可能影响性能                           │   │
│     │ - 建议使用事件过滤和批处理                             │   │
│     └─────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────┘

## 核心数据流总结与性能瓶颈分析

┌───────────────────────────────────────────────────────────────────┐
│                        系统性能热点分析                           │
│                                                                   │
│  高频调用路径 (每帧执行):                                         │
│  1. GAS.Tick() → ASC.Tick() → 四大容器.Tick()                    │
│     瓶颈: 大量ASC时的线性扫描                                     │
│     优化: 可考虑优先级队列或分帧处理                              │
│                                                                   │
│  2. AttributeAggregator 的依赖重计算                             │
│     瓶颈: 复杂依赖网络的递归计算                                  │
│     优化: 脏标记、增量计算、计算缓存                              │
│                                                                   │
│  3. GameplayTagAggregator 的标签查询                             │
│     瓶颈: 大量标签的 HasAllTags/HasAnyTags 调用                  │
│     优化: 位掩码、布隆过滤器、查询缓存                            │
│                                                                   │
│  4. 硬编码事件的频繁触发                                          │
│     瓶颈: TriggerOnTick() 每帧调用，持续性Cue更新                │
│     优化: 按需更新、LOD系统、事件批处理                           │
│                                                                   │
│  中频调用路径 (技能/效果变化时):                                  │
│  1. 技能激活的条件检查链                                          │
│     瓶颈: 多层级验证的串行执行                                    │
│     优化: 快速失败、条件优先级排序                                │
│                                                                   │
│  2. GameplayEffect 的堆叠查找                                     │
│     瓶颈: 线性搜索现有效果                                        │
│     优化: 哈希表索引、效果分类                                    │
│                                                                   │
│  3. ExecutionCalculation 的条件执行                              │
│     瓶颈: 复杂条件判断和优先级排序                                │
│     优化: 条件预计算、执行计划缓存                                │
│                                                                   │
│  低频调用路径 (初始化/清理时):                                    │
│  1. ASC 的初始化和容器创建                                        │
│  2. 复杂 Timeline 的 Playable Graph 构建                         │
│  3. 事件监听器的注册和注销                                        │
│  4. SetupEventListeners() 的ExecutionCalculation扫描             │
│                                                                   │
│  内存使用分析:                                                    │
│  · 主要开销: GameplayEffectSpec 实例和修改器列表                 │
│  · 缓存开销: 各种 _cached 列表的额外内存                         │
│  · 事件开销: GameplayEventBus 的委托链                           │
│  · Cue开销: 持续性Cue的状态维护                                  │
│                                                                   │
│  扩展性考虑:                                                      │
│  · 支持数百个并发 ASC                                             │
│  · 支持数千个并发 GameplayEffect                                  │
│  · 支持复杂的属性依赖网络                                         │
│  · 支持实时的标签状态查询                                         │
│  · 支持大量的事件监听器注册                                       │
└───────────────────────────────────────────────────────────────────┘

## 事件驱动架构的完整数据流

┌───────────────────────────────────────────────────────────────────┐
│                    端到端事件传播示例分析                          │
│                                                                   │
│  场景: 玩家释放火球术攻击敌人                                     │
│                                                                   │
│  1. 技能激活触发链:                                               │
│     TryActivateAbility("Fireball") → 条件检查通过                │
│     → ActivateAbility() → 应用消耗和冷却                         │
│     → PublishAbilityEvent(Activated, fireballSpec)               │
│                                                                   │
│  2. Timeline 执行序列:                                            │
│     TimelinePlayer.Play() → 动画轨道开始播放                     │
│     → 1.2s时: Effect轨道触发伤害计算 GameplayEffect               │
│     → 1.5s时: Cue轨道播放火球特效和音效                          │
│     → 2.0s时: Timeline完成，自动 EndAbility()                    │
│                                                                   │
│  3. 伤害效果应用链:                                               │
│     ApplyGameplayEffectToTarget(enemy, damageEffect)              │
│     → GameplayEffectContainer.AddGameplayEffectSpec()             │
│     → TriggerOnExecute() → 即时伤害计算                          │
│     → AttributeModification: enemy.Health -= damage              │
│     → PublishAttributeEvent("Health", oldHP, newHP)               │
│                                                                   │
│  4. 属性变化级联:                                                 │
│     Health属性变化 → 检查依赖属性                                │
│     → 可能触发 "低血量" 标签添加                                  │
│     → RefreshGameplayEffectState() → 激活救急技能                │
│     → PublishTagEvent("LowHealth", true, 1)                      │
│                                                                   │
│  5. 事件监听器响应:                                               │
│     UI系统: 更新血条显示                                          │
│     AI系统: 切换为逃跑行为                                        │
│     成就系统: 检查伤害里程碑                                      │
│     音效系统: 播放受击音效                                        │
│                                                                   │
│  6. 后续效果处理:                                                 │
│     如果敌人死亡 (Health <= 0):                                   │
│     → 触发死亡事件 → 移除所有效果 → 授予经验                     │
│     → PublishGameplayEvent(Death, killer, victim)                │
│     → 可能触发连击效果或技能重置                                  │
│                                                                   │
│  数据流总结:                                                      │
│  输入 → 验证 → 执行 → 修改 → 通知 → 响应 → 级联                  │
│  每个环节都有明确的数据结构和算法支撑                             │
│  整个流程具备良好的扩展性和可调试性                               │
└───────────────────────────────────────────────────────────────────┘