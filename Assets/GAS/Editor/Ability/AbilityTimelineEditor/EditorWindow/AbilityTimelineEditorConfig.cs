#if UNITY_EDITOR
namespace GAS.Editor
{
    using GAS.General;

    public class AbilityTimelineEditorConfig
    {
        /// <summary>
        /// 帧单位宽度
        /// </summary>
        public int FrameUnitWidth = 10;
        /// <summary>
        /// 标准帧单位宽度
        /// </summary>
        public const int StandardFrameUnitWidth = 1;
        /// <summary>
        /// 最大帧单位级别
        /// </summary>
        public const int MaxFrameUnitLevel= 20;
        /// <summary>
        /// 时间轴帧绘制的最小步长
        /// </summary>
        public const float MinTimerShaftFrameDrawStep = 5;
        /// <summary>
        /// 默认帧率
        /// </summary>
        public int DefaultFrameRate => GASTimer.FrameRate;
    }
}
#endif