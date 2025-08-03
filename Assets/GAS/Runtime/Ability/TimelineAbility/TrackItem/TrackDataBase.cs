using System;

namespace GAS.Runtime
{
    [Serializable]
    public class TrackDataBase
    {
        /// <summary>
        /// 轨道名称
        /// </summary>
        public string trackName;
        
        public virtual void AddToAbilityAsset(TimelineAbilityAssetBase abilityAsset)
        {
        }
        
        public virtual void DefaultInit()
        {
        }
    }
}