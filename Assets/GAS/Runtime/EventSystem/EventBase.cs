using System;

namespace GAS.Runtime
{
    public class EventBase<T> where T : EventArgs
    {
        /// <summary>
        /// 事件处理器委托，用于存储所有订阅此事件的处理方法
        /// </summary>
        /// <remarks>
        /// 所有通过Subscribe方法订阅的事件处理方法都会被添加到此委托中，
        /// 当调用Publish方法时，会触发此委托调用所有订阅的方法
        /// </remarks>
        public event EventHandler<T> EventHandler;

        public void Publish(T args)
        {
            EventHandler?.Invoke(this, args);
        }

        public void Subscribe(EventHandler<T> handler)
        {
            EventHandler += handler;
        }

        public void Unsubscribe(EventHandler<T> handler)
        {
            EventHandler -= handler;
        }
    }
}