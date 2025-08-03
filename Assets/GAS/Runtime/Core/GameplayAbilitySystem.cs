using System.Collections.Generic;
using GAS.General;
using GAS.Runtime;
using System.Diagnostics;

namespace GAS
{
    /// <summary>
    /// 游戏能力系统核心管理器，负责整个GAS框架的全局管理和协调
    /// </summary>
    /// <remarks>
    /// GameplayAbilitySystem是整个GAS框架的中央控制器，采用单例模式管理：
    /// 
    /// 核心职责：
    /// - 管理所有AbilitySystemComponent的注册和注销
    /// - 协调全局的Tick更新循环
    /// - 提供系统级的暂停和恢复功能
    /// - 管理GAS系统的生命周期
    /// - 性能监控和调试支持
    /// 
    /// 架构特点：
    /// - 单例模式，全局唯一实例
    /// - 基于Unity MonoBehaviour的更新循环
    /// - 支持运行时动态注册/注销组件
    /// - 提供系统级暂停功能
    /// - 内置性能分析支持
    /// 
    /// 使用场景：
    /// - 游戏初始化时自动创建
    /// - 管理场景中所有使用GAS的实体
    /// - 提供全局的GAS系统控制
    /// - 性能监控和调试
    /// 
    /// 注意事项：
    /// - 系统会自动创建一个不可销毁的GameObject作为宿主
    /// - 所有ASC必须注册到此系统才能正常工作
    /// - 暂停系统会影响所有注册的组件
    /// </remarks>
    /// <example>
    /// // 获取GAS系统实例（自动创建）
    /// var gas = GameplayAbilitySystem.GAS;
    /// 
    /// // 暂停整个GAS系统
    /// gas.Pause();
    /// 
    /// // 恢复GAS系统
    /// gas.Unpause();
    /// 
    /// // 检查系统状态
    /// if (!gas.IsPaused)
    /// {
    ///     Debug.Log($"当前注册的ASC数量: {gas.AbilitySystemComponents.Count}");
    /// }
    /// 
    /// // 清理所有组件（通常在游戏结束时）
    /// gas.ClearComponents();
    /// </example>
    public class GameplayAbilitySystem
    {
        /// <summary>
        /// 全局GAS系统实例
        /// </summary>
        private static GameplayAbilitySystem _gas;

        /// <summary>
        /// 私有构造函数，初始化GAS系统
        /// </summary>
        /// <remarks>
        /// 构造过程包括：
        /// 1. 初始化组件列表（预分配1024容量）
        /// 2. 初始化GAS计时器系统
        /// 3. 创建GAS宿主实例
        /// 4. 配置宿主对象的生命周期管理
        /// 
        /// 宿主对象特性：
        /// - 使用.NET Timer提供更新循环
        /// - 自动管理资源生命周期
        /// - 承载GAS系统的更新逻辑
        /// </remarks>
        private GameplayAbilitySystem()
        {
            const int capacity = 1024;
            AbilitySystemComponents = new List<AbilitySystemComponent>(capacity);
            _cachedAbilitySystemComponents = new List<AbilitySystemComponent>(capacity);
            GASTimer.InitStartTimestamp();

            GasHost = new GasHost();
        }

        /// <summary>
        /// 获取当前注册到系统中的所有AbilitySystemComponent组件列表
        /// </summary>
        /// <remarks>
        /// 此列表包含所有活跃的ASC组件，用于：
        /// - 系统级的Tick更新循环
        /// - 全局组件管理和查询
        /// - 系统状态监控
        /// 
        /// 注意：直接修改此列表可能导致系统不稳定，建议使用Register/Unregister方法。
        /// </remarks>
        public List<AbilitySystemComponent> AbilitySystemComponents { get; }

        /// <summary>
        /// 缓存的组件列表，用于安全的迭代更新
        /// </summary>
        /// <remarks>
        /// 在Tick更新过程中复制主列表到此缓存，避免在迭代过程中列表被修改导致的问题。
        /// 这种模式确保了更新过程的稳定性和线程安全性。
        /// </remarks>
        private readonly List<AbilitySystemComponent> _cachedAbilitySystemComponents;

        /// <summary>
        /// GAS系统的宿主组件
        /// </summary>
        /// <remarks>
        /// 提供生命周期支持，包括更新循环和组件管理。
        /// 使用.NET Timer实现定时更新机制。
        /// </remarks>
        private GasHost GasHost { get; }

        /// <summary>
        /// 获取GAS系统的全局单例实例
        /// </summary>
        /// <remarks>
        /// 使用懒加载模式，首次访问时自动创建系统实例。
        /// 这是访问GAS系统的主要入口点，确保全局只有一个系统实例。
        /// 
        /// 初始化时机：
        /// - 首次访问此属性时自动创建
        /// - 在游戏开始时通常由第一个ASC的注册触发
        /// - 可以主动访问来预初始化系统
        /// </remarks>
        /// <example>
        /// // 获取系统实例
        /// var gasSystem = GameplayAbilitySystem.GAS;
        /// 
        /// // 检查系统状态
        /// if (gasSystem.IsPaused)
        /// {
        ///     Debug.Log("GAS系统当前处于暂停状态");
        /// }
        /// </example>
        public static GameplayAbilitySystem GAS
        {
            get
            {
                _gas ??= new GameplayAbilitySystem();
                return _gas;
            }
        }

        /// <summary>
        /// 检查GAS系统是否处于暂停状态
        /// </summary>
        /// <remarks>
        /// 暂停状态下：
        /// - 所有ASC的Tick更新会停止
        /// - 新的组件注册可能受限
        /// - 系统级功能暂停执行
        /// 
        /// 常用于游戏暂停、菜单界面、加载过程等场景。
        /// </remarks>
        /// <example>
        /// // 检查系统状态
        /// if (GameplayAbilitySystem.GAS.IsPaused)
        /// {
        ///     Console.WriteLine("GAS系统已暂停，技能和效果不会更新");
        /// }
        /// </example>
        public bool IsPaused => !GasHost.Enabled;

        /// <summary>
        /// 注册AbilitySystemComponent到GAS系统
        /// </summary>
        /// <param name="abilitySystemComponent">要注册的ASC组件</param>
        /// <remarks>
        /// 注册流程：
        /// 1. 检查组件是否已注册（避免重复注册）
        /// 2. 将组件添加到系统管理列表
        /// 3. 组件开始接受系统级的Tick更新
        /// 
        /// 注册后的组件会：
        /// - 参与系统的统一更新循环
        /// - 受系统暂停/恢复控制
        /// - 被包含在系统级的管理和监控中
        /// 
        /// 通常在ASC的OnEnable或Init方法中自动调用。
        /// </remarks>
        /// <example>
        /// // ASC通常在启用时自动注册
        /// public void OnEnable()
        /// {
        ///     GameplayAbilitySystem.GAS.Register(this);
        /// }
        /// </example>
        public void Register(AbilitySystemComponent abilitySystemComponent)
        {
            // if (!GasHost.enabled)
            // {
            //     Debug.LogWarning("[EX] GAS is paused, can't register new ASC!");
            //     return;
            // }

            if (AbilitySystemComponents.Contains(abilitySystemComponent)) return;
            AbilitySystemComponents.Add(abilitySystemComponent);
        }

        /// <summary>
        /// 从GAS系统中注销AbilitySystemComponent
        /// </summary>
        /// <param name="abilitySystemComponent">要注销的ASC组件</param>
        /// <returns>如果成功注销返回true，如果组件未注册则返回false</returns>
        /// <remarks>
        /// 注销流程：
        /// 1. 从系统管理列表中移除组件
        /// 2. 组件停止接受系统级的Tick更新
        /// 3. 释放系统级的资源引用
        /// 
        /// 注销后的组件：
        /// - 不再参与系统更新循环
        /// - 不受系统暂停/恢复影响
        /// - 从系统级管理中移除
        /// 
        /// 通常在ASC的OnDisable或销毁时自动调用。
        /// </remarks>
        /// <example>
        /// // ASC通常在禁用时自动注销
        /// public void OnDisable()
        /// {
        ///     GameplayAbilitySystem.GAS.Unregister(this);
        /// }
        /// </example>
        public bool Unregister(AbilitySystemComponent abilitySystemComponent)
        {
            // if (!GasHost.enabled)
            // {
            //     Debug.LogWarning("[EX] GAS is paused, can't unregister ASC!");
            //     return false;
            // }

            return AbilitySystemComponents.Remove(abilitySystemComponent);
        }

        /// <summary>
        /// 暂停整个GAS系统
        /// </summary>
        /// <remarks>
        /// 暂停后的影响：
        /// - 所有ASC的Tick更新停止
        /// - 技能、效果、属性的时间相关更新暂停
        /// - 冷却时间、持续时间等计时器暂停
        /// - 系统级事件和回调暂停
        /// 
        /// 适用场景：
        /// - 游戏暂停菜单
        /// - 加载界面
        /// - 对话系统
        /// - 任何需要暂停游戏逻辑的情况
        /// </remarks>
        /// <example>
        /// // 进入暂停菜单时
        /// public void OnPauseMenuOpen()
        /// {
        ///     GameplayAbilitySystem.GAS.Pause();
        /// }
        /// </example>
        public void Pause()
        {
            GasHost.Enabled = false;
        }

        /// <summary>
        /// 恢复整个GAS系统的运行
        /// </summary>
        /// <remarks>
        /// 恢复后的影响：
        /// - 所有ASC重新开始Tick更新
        /// - 技能、效果、属性的时间相关更新继续
        /// - 冷却时间、持续时间等计时器继续计时
        /// - 系统级事件和回调恢复
        /// 
        /// 注意：暂停期间经过的时间不会被补偿，计时器从暂停的位置继续。
        /// </remarks>
        /// <example>
        /// // 退出暂停菜单时
        /// public void OnPauseMenuClose()
        /// {
        ///     GameplayAbilitySystem.GAS.Unpause();
        /// }
        /// </example>
        public void Unpause()
        {
            GasHost.Enabled = true;
        }

        /// <summary>
        /// 清理所有注册的ASC组件
        /// </summary>
        /// <remarks>
        /// 清理流程：
        /// 1. 逐个禁用所有注册的ASC组件
        /// 2. 清空系统管理列表
        /// 3. 释放所有相关资源
        /// 
        /// 此操作会：
        /// - 强制禁用所有ASC组件
        /// - 清理所有技能、效果、属性状态
        /// - 重置系统到初始状态
        /// 
        /// 警告：这是一个破坏性操作，会立即停止所有GAS相关功能。
        /// 通常只在游戏结束、场景切换或系统重置时使用。
        /// </remarks>
        /// <example>
        /// // 游戏结束时清理所有GAS组件
        /// public void OnGameEnd()
        /// {
        ///     GameplayAbilitySystem.GAS.ClearComponents();
        /// }
        /// 
        /// // 切换到主菜单时清理
        /// public void ReturnToMainMenu()
        /// {
        ///     GameplayAbilitySystem.GAS.ClearComponents();
        ///     SceneManager.LoadScene("MainMenu");
        /// }
        /// </example>
        public void ClearComponents()
        {
            foreach (var t in AbilitySystemComponents)
                t.Dispose();

            AbilitySystemComponents.Clear();
        }

        /// <summary>
        /// 执行GAS系统的帧更新循环
        /// </summary>
        /// <remarks>
        /// Tick更新流程：
        /// 1. 开始性能分析采样
        /// 2. 复制组件列表到缓存（避免迭代时修改冲突）
        /// 3. 逐个更新所有注册的ASC组件
        /// 4. 清理缓存列表
        /// 5. 结束性能分析采样
        /// 
        /// 更新内容包括：
        /// - 技能的生命周期管理
        /// - 游戏效果的时间更新和过期检查
        /// - 属性值的重新计算
        /// - 冷却时间和持续时间的递减
        /// - 各种计时器和状态机的更新
        /// 
        /// 性能考虑：
        /// - 使用Unity Profiler进行性能监控
        /// - 采用缓存列表避免并发修改问题
        /// - 支持大量ASC的高效更新
        /// 
        /// 此方法通常由GasHost的Timer自动调用，不需要手动调用。
        /// </remarks>
        /// <example>
        /// // 通常不需要手动调用，但可以用于调试
        /// void DebugManualTick()
        /// {
        ///     if (!GameplayAbilitySystem.GAS.IsPaused)
        ///     {
        ///         GameplayAbilitySystem.GAS.Tick();
        ///     }
        /// }
        /// </example>
        public void Tick()
        {
            // 使用.NET Stopwatch进行性能监控（可选）
            var stopwatch = Stopwatch.StartNew();

            _cachedAbilitySystemComponents.Clear();
            _cachedAbilitySystemComponents.AddRange(AbilitySystemComponents);

            foreach (var abilitySystemComponent in _cachedAbilitySystemComponents)
            {
                abilitySystemComponent.Tick();
            }

            _cachedAbilitySystemComponents.Clear();

            stopwatch.Stop();
            // 如果需要性能调试，可以输出时间
            // Console.WriteLine($"GAS Tick took: {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}