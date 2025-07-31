using GAS.General;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// GAS系统的Unity宿主组件，负责提供Unity生命周期支持
    /// </summary>
    /// <remarks>
    /// GasHost是一个不可销毁的MonoBehaviour，为GAS系统提供Unity的生命周期回调。
    /// 它承载在一个隐藏的GameObject上，确保GAS系统能够持续运行。
    /// </remarks>
    public class GasHost : MonoBehaviour
    {
        /// <summary>
        /// 获取GAS系统单例实例的便捷访问器
        /// </summary>
        private GameplayAbilitySystem _gas => GameplayAbilitySystem.GAS;

        /// <summary>
        /// Unity Update生命周期方法，执行GAS系统的帧更新
        /// </summary>
        /// <remarks>
        /// 每帧执行以下操作：
        /// 1. 更新GAS计时器的当前帧计数
        /// 2. 调用GAS系统的Tick方法更新所有注册的ASC
        /// </remarks>
        private void Update()
        {
            GASTimer.UpdateCurrentFrameCount();
            _gas.Tick();
        }

        /// <summary>
        /// Unity OnDestroy生命周期方法，清理GAS系统资源
        /// </summary>
        /// <remarks>
        /// 在宿主对象被销毁时，清理所有注册的ASC组件，
        /// 确保系统资源得到正确释放。
        /// </remarks>
        private void OnDestroy()
        {
            _gas.ClearComponents();
        }
    }
}