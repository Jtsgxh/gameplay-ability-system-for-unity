using System;
using UnityEngine.Serialization;

namespace GAS.Runtime
{
    [Serializable]
    public abstract class TrackEventBase
    {        
        /// <summary>
        /// 开始帧
        /// </summary>
        public int startFrame;
    }
    
    [Serializable]
    public abstract class MarkEventBase:TrackEventBase
    {
    }
    
    [Serializable]
    public abstract class ClipEventBase:TrackEventBase
    {
        /// <summary>
        /// 持续帧数
        /// </summary>
        public int durationFrame;
        public int EndFrame => startFrame + durationFrame;
    }
}