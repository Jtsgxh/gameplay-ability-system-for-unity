using GAS.General;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GAS
{
    /// <summary>
    /// GAS系统的.NET宿主组件，负责提供生命周期和更新循环支持
    /// </summary>
    /// <remarks>
    /// GasHost为GAS系统提供更新循环和生命周期管理。
    /// 使用.NET Timer实现定时更新，确保GAS系统能够持续运行。
    /// </remarks>
    public class GasHost : IDisposable
    {
        /// <summary>
        /// 获取GAS系统单例实例的便捷访问器
        /// </summary>
        private GameplayAbilitySystem _gas => GameplayAbilitySystem.GAS;

        /// <summary>
        /// 更新循环的定时器
        /// </summary>
        private Timer _updateTimer;

        /// <summary>
        /// 是否已启用更新循环
        /// </summary>
        private bool _enabled = true;

        /// <summary>
        /// 是否已释放资源
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// 更新间隔（毫秒），默认约60FPS
        /// </summary>
        public int UpdateIntervalMs { get; set; } = 16;

        /// <summary>
        /// 启用状态
        /// </summary>
        public bool Enabled 
        { 
            get => _enabled; 
            set => _enabled = value; 
        }

        /// <summary>
        /// 初始化GAS宿主
        /// </summary>
        public GasHost()
        {
            StartUpdateLoop();
        }

        /// <summary>
        /// 启动更新循环
        /// </summary>
        private void StartUpdateLoop()
        {
            _updateTimer = new Timer(UpdateCallback, null, 0, UpdateIntervalMs);
        }

        /// <summary>
        /// 更新回调方法，执行GAS系统的帧更新
        /// </summary>
        /// <remarks>
        /// 每次更新执行以下操作：
        /// 1. 更新GAS计时器的当前帧计数
        /// 2. 调用GAS系统的Tick方法更新所有注册的ASC
        /// </remarks>
        private void UpdateCallback(object state)
        {
            if (!_enabled || _disposed) return;

            try
            {
                GASTimer.UpdateCurrentFrameCount();
                _gas.Tick();
            }
            catch (Exception ex)
            {
                // 记录异常但不中断更新循环
                Console.WriteLine($"GAS Update Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <remarks>
        /// 在宿主对象被销毁时，清理所有注册的ASC组件，
        /// 确保系统资源得到正确释放。
        /// </remarks>
        public void Dispose()
        {
            if (_disposed) return;

            _updateTimer?.Dispose();
            _gas.ClearComponents();
            _disposed = true;
        }
    }
}