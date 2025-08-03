using System;

namespace GAS.General
{
    public class GASTimer
    {
        // TODO 矫正时间差(服务器客户端时间差/暂停游戏导致的时间差)
        /// <summary>
        /// 时间偏移量（用于矫正服务器客户端时间差或暂停游戏导致的时间差）
        /// </summary>
        static int _deltaTime;
        
        public static long Timestamp() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _deltaTime;

        public static long TimestampSeconds() => Timestamp() / 1000;
        
        /// <summary>
        /// 当前帧数
        /// </summary>
        private static int _currentFrameCount;
        public static int CurrentFrameCount => _currentFrameCount; 
        public static void UpdateCurrentFrameCount()
        {
            _currentFrameCount = (int)Math.Floor((Timestamp() - _startTimestamp) / 1000f * FrameRate);
        }

        /// <summary>
        /// 开始时间戳
        /// </summary>
        private static long _startTimestamp;
        public static long StartTimestamp => _startTimestamp;
        public static void InitStartTimestamp()
        {
            _startTimestamp = Timestamp();
        }
        
        
        /// <summary>
        /// 暂停时间戳
        /// </summary>
        private static long _pauseTimestamp;
        public static void Pause()
        {
            _pauseTimestamp = Timestamp();
        }
        
        public static void Unpause()
        {
            _deltaTime -= (int)(Timestamp() - _pauseTimestamp);
        }
        
        /// <summary>
        /// 帧率
        /// </summary>
        private static int _frameRate = 60;
        public static int FrameRate => _frameRate;
    }
}