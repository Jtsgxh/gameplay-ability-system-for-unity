
namespace GAS.Runtime
{
    public abstract class GameplayCueSpec
    {
        /// <summary>
        /// 游戏提示
        /// </summary>
        protected readonly GameplayCue _cue;
        /// <summary>
        /// 游戏提示参数
        /// </summary>
        protected readonly GameplayCueParameters _parameters;
        public AbilitySystemComponent Owner { get; protected set; }

        public virtual bool Triggerable()
        {
            return _cue.Triggerable(Owner);
        }
        
        public GameplayCueSpec(GameplayCue cue, GameplayCueParameters cueParameters)
        {
            _cue = cue;
            _parameters = cueParameters;
            if (_parameters.sourceGameplayEffectSpec != null)
            {
                Owner = _parameters.sourceGameplayEffectSpec.Owner;
            }
            else if (_parameters.sourceAbilitySpec != null)
            {
                Owner = _parameters.sourceAbilitySpec.Owner;
            }
        }
    }
}