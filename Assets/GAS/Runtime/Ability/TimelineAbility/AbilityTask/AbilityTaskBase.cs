namespace GAS.Runtime
{
    public abstract class AbilityTaskBase
    {
        /// <summary>
        /// 技能规格实例引用，提供任务执行时所需的上下文信息
        /// </summary>
        protected AbilitySpec _spec;
        public AbilitySpec Spec => _spec;
        public virtual void Init(AbilitySpec spec)
        {
            _spec = spec;
        }
    }
}