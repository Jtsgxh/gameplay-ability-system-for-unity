using UnityEngine.Profiling;

namespace GAS.Runtime
{
    /// <summary>
    /// 时间轴技能泛型基类，为基于Unity Timeline的复杂技能提供基础结构
    /// </summary>
    /// <typeparam name="T">继承自TimelineAbilityAssetBase的技能资产类型</typeparam>
    /// <remarks>
    /// TimelineAbilityT提供了时间轴技能的基础实现框架：
    /// 
    /// - 与Unity Timeline系统集成
    /// - 支持复杂的多阶段技能序列
    /// - 可视化的技能编辑体验
    /// - 精确的时间控制和同步
    /// 
    /// 时间轴技能的优势：
    /// - 非程序员也可以通过Timeline编辑技能序列
    /// - 精确的时间控制，支持复杂的技能组合
    /// - 可视化的技能流程，便于调试和优化
    /// - 与动画、音效、特效系统深度集成
    /// 
    /// 适用场景：
    /// - 复杂的连击技能
    /// - 多阶段施法技能
    /// - 需要精确时序控制的技能
    /// - 与动画紧密结合的技能
    /// </remarks>
    public abstract class TimelineAbilityT<T> : AbstractAbility<T> where T : TimelineAbilityAssetBase
    {
        /// <summary>
        /// 初始化时间轴技能
        /// </summary>
        /// <param name="abilityAsset">时间轴技能资产数据</param>
        protected TimelineAbilityT(T abilityAsset) : base(abilityAsset)
        {
        }
    }

    /// <summary>
    /// 时间轴技能实例泛型基类，管理时间轴技能的运行时执行
    /// </summary>
    /// <typeparam name="T">继承自AbstractAbility的技能类型</typeparam>
    /// <remarks>
    /// TimelineAbilitySpecT负责时间轴技能的实际执行：
    /// 
    /// - 管理TimelineAbilityPlayer的生命周期
    /// - 处理技能目标的设置和管理
    /// - 提供标准的激活、取消、结束接口
    /// - 集成Unity Profiler进行性能监控
    /// 
    /// 核心组件：
    /// - TimelineAbilityPlayer: 时间轴播放器，负责驱动Timeline
    /// - Target: 技能的主要目标，用于向性技能
    /// - 标准的AbilitySpec生命周期管理
    /// 
    /// 性能考虑：
    /// - 使用Unity Profiler标记进行性能监控
    /// - 在技能结束时正确停止播放器避免资源泄漏
    /// </remarks>
    public abstract class TimelineAbilitySpecT<T> : AbilitySpec<T> where T : AbstractAbility
    {
        /// <summary>
        /// 时间轴播放器，负责驱动Unity Timeline的播放
        /// </summary>
        /// <remarks>
        /// TimelineAbilityPlayer是时间轴技能系统的核心组件：
        /// - 管理Timeline资产的播放状态
        /// - 处理时间轴上的事件和标记
        /// - 与GAS系统的其他组件进行交互
        /// - 提供精确的时间控制功能
        /// </remarks>
        protected TimelineAbilityPlayer<T> _player;

        /// <summary>
        /// 向性技能的作用目标
        /// 存储技能的主要目标，供时间轴中的各个轨道使用
        /// </summary>
        /// <remarks>
        /// Target在时间轴技能中的作用：
        /// - 为向性技能提供目标引用
        /// - 可以被时间轴中的效果轨道使用
        /// - 支持动态目标切换（如果需要）
        /// - 与TargetCatcher系统配合工作
        /// 
        /// 注意：不是所有时间轴技能都需要目标，
        /// 某些自身buff或范围技能可能不使用此属性。
        /// </remarks>
        public AbilitySystemComponent Target { get; private set; }

        /// <summary>
        /// 初始化时间轴技能实例
        /// </summary>
        /// <param name="ability">技能定义</param>
        /// <param name="owner">技能拥有者</param>
        /// <remarks>
        /// 初始化过程：
        /// 1. 调用基类构造函数设置基础属性
        /// 2. 创建TimelineAbilityPlayer实例
        /// 3. 建立播放器与技能实例的关联
        /// 
        /// TimelineAbilityPlayer会持有此技能实例的引用，
        /// 用于在时间轴播放过程中回调技能的相关方法。
        /// </remarks>
        protected TimelineAbilitySpecT(T ability, AbilitySystemComponent owner) : base(ability, owner)
        {
            _player = new TimelineAbilityPlayer<T>(this);
        }

        /// <summary>
        /// 设置技能的目标对象
        /// </summary>
        /// <param name="mainTarget">主要目标AbilitySystemComponent</param>
        /// <remarks>
        /// 此方法通常在技能激活前调用，用于设置向性技能的目标。
        /// 
        /// 调用时机：
        /// - 在TryActivateAbility之前
        /// - 目标选择完成后
        /// - 可以在技能执行过程中动态调用（如果技能支持）
        /// 
        /// 使用场景：
        /// - 单体目标技能：设置攻击或治疗目标
        /// - 向性范围技能：设置爆炸中心点
        /// - 投射物技能：设置投射目标
        /// </remarks>
        public void SetAbilityTarget(AbilitySystemComponent mainTarget)
        {
            Target = mainTarget;
        }

        /// <summary>
        /// 激活时间轴技能，开始播放Timeline
        /// </summary>
        /// <param name="args">激活参数（时间轴技能通常不使用）</param>
        /// <remarks>
        /// 激活流程：
        /// 1. 调用TimelineAbilityPlayer的Play方法
        /// 2. 开始驱动Unity Timeline的播放
        /// 3. Timeline中的各种轨道开始按时间序列执行
        /// 
        /// Timeline轨道类型：
        /// - 任务轨道：执行特定的技能逻辑
        /// - 效果轨道：应用GameplayEffect
        /// - Cue轨道：播放视听反馈
        /// - 动画轨道：控制角色动画
        /// 
        /// 注意：时间轴技能的实际逻辑在Timeline中定义，
        /// 此方法只是启动播放过程。
        /// </remarks>
        public override void ActivateAbility(params object[] args)
        {
            _player.Play();
        }

        /// <summary>
        /// 取消时间轴技能，立即停止Timeline播放
        /// </summary>
        /// <remarks>
        /// 取消操作：
        /// 1. 调用TimelineAbilityPlayer的Stop方法
        /// 2. 立即停止Timeline播放
        /// 3. 清理正在执行的时间轴任务
        /// 4. 恢复技能状态
        /// 
        /// 取消与正常结束的区别：
        /// - 取消是强制中断，可能不执行完整的清理逻辑
        /// - 某些时间轴上的"结束"标记可能不会被触发
        /// - 适用于被打断、眩晕等外部条件触发的情况
        /// </remarks>
        public override void CancelAbility()
        {
            _player.Stop();
        }

        /// <summary>
        /// 正常结束时间轴技能，停止Timeline播放
        /// </summary>
        /// <remarks>
        /// 结束操作：
        /// 1. 调用TimelineAbilityPlayer的Stop方法
        /// 2. 正常停止Timeline播放
        /// 3. 执行完整的清理流程
        /// 4. 触发结束相关的事件和回调
        /// 
        /// 正常结束通常发生在：
        /// - Timeline播放完毕
        /// - 技能逻辑主动调用EndAbility
        /// - 达到某个结束条件
        /// </remarks>
        public override void EndAbility()
        {
            _player.Stop();
        }

        /// <summary>
        /// 时间轴技能的帧更新，驱动Timeline播放
        /// </summary>
        /// <remarks>
        /// 更新流程：
        /// 1. 使用Unity Profiler标记开始性能采样
        /// 2. 调用TimelineAbilityPlayer的Tick方法
        /// 3. 播放器更新Timeline的播放状态
        /// 4. 处理时间轴上的事件和标记
        /// 5. 结束性能采样
        /// 
        /// 性能监控：
        /// - 使用Unity Profiler进行性能分析
        /// - 采样名称便于在Profiler中识别
        /// - 有助于发现时间轴技能的性能瓶颈
        /// 
        /// 注意：此方法仅在技能激活状态下被调用，
        /// 由AbilitySpec.Tick()自动管理调用时机。
        /// </remarks>
        protected override void AbilityTick()
        {
            Profiler.BeginSample("TimelineAbilitySpecT<T>::AbilityTick()");
            _player.Tick();
            Profiler.EndSample();
        }
    }

    /// <summary>
    /// 最基础的时间轴技能实现，适用于简单的时间轴技能需求
    /// </summary>
    /// <remarks>
    /// 这是一个封装完整的时间轴技能实现，提供了：
    /// 
    /// - 开箱即用的时间轴技能功能
    /// - 与TimelineAbilityAssetBase的直接集成
    /// - 标准的技能创建和管理流程
    /// 
    /// 使用场景：
    /// - 不需要自定义扩展的简单时间轴技能
    /// - 快速原型开发和测试
    /// - 学习时间轴技能系统的入门示例
    /// 
    /// 如果需要更复杂的功能，建议：
    /// - 继承TimelineAbilityT&lt;T&gt;和TimelineAbilitySpecT&lt;T&gt;
    /// - 实现自定义的时间轴事件处理
    /// - 添加特定的技能逻辑和状态管理
    /// </remarks>
    public sealed class TimelineAbility : TimelineAbilityT<TimelineAbilityAssetBase>
    {
        /// <summary>
        /// 创建基础时间轴技能实例
        /// </summary>
        /// <param name="abilityAsset">时间轴技能资产</param>
        public TimelineAbility(TimelineAbilityAssetBase abilityAsset) : base(abilityAsset)
        {
        }

        /// <summary>
        /// 创建对应的时间轴技能规格实例
        /// </summary>
        /// <param name="owner">技能拥有者</param>
        /// <returns>TimelineAbilitySpec实例</returns>
        /// <remarks>
        /// 此方法实现了AbstractAbility的抽象方法，
        /// 返回与此技能匹配的TimelineAbilitySpec实例。
        /// 
        /// 工厂方法模式确保：
        /// - 技能与规格的正确匹配
        /// - 统一的实例创建流程
        /// - 类型安全的对象创建
        /// </remarks>
        public override AbilitySpec CreateSpec(AbilitySystemComponent owner)
        {
            return new TimelineAbilitySpec(this, owner);
        }
    }

    /// <summary>
    /// 最基础的时间轴技能规格实现，管理基础时间轴技能的运行时行为
    /// </summary>
    /// <remarks>
    /// 这是一个完整的时间轴技能规格实现，提供了：
    /// 
    /// - 标准的时间轴技能执行流程
    /// - 与TimelineAbility的完美配合
    /// - 所有必需的生命周期管理
    /// 
    /// 功能特性：
    /// - 自动的Timeline播放管理
    /// - 标准的激活、取消、结束处理
    /// - 集成的性能监控支持
    /// - 目标管理和传递
    /// 
    /// 扩展建议：
    /// 如果需要更复杂的时间轴技能功能，可以：
    /// - 继承TimelineAbilitySpecT&lt;T&gt;基类
    /// - 重写相关的生命周期方法
    /// - 添加自定义的时间轴事件处理
    /// - 实现特定的技能逻辑
    /// </remarks>
    public sealed class TimelineAbilitySpec : TimelineAbilitySpecT<TimelineAbility>
    {
        /// <summary>
        /// 创建基础时间轴技能规格实例
        /// </summary>
        /// <param name="ability">对应的时间轴技能</param>
        /// <param name="owner">技能拥有者</param>
        public TimelineAbilitySpec(TimelineAbility ability, AbilitySystemComponent owner) : base(ability, owner)
        {
        }
    }
}