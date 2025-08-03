namespace GAS.Runtime
{
    public class GASEvents
    {
        /// <summary>
        /// 属性值变化事件的全局实例
        /// </summary>
        /// <remarks>
        /// 该静态事件实例用于在整个GAS系统中广播属性值的变化，
        /// 当任何属性值发生改变时都会通过此事件通知所有订阅者
        /// </remarks>
        public static AttributeChangedEvent AttributeChanged = new AttributeChangedEvent();
    }
}