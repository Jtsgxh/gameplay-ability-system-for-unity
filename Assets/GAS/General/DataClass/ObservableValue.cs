using System;

namespace GAS.General
{
    public class ObservableValue<T>
    {
        /// <summary>
        /// 存储的值
        /// </summary>
        private T _value;

        public ObservableValue(T initialValue)
        {
            _value = initialValue;
        }

        public T Value
        {
            get => _value;
            set
            {
                var oldValue = _value;
                _value = value;
                OnValueChanged?.Invoke(oldValue, value);
            }
        }

        /// <summary>
        /// 值变更事件，参数为旧值和新值
        /// </summary>
        public event Action<T, T> OnValueChanged;
    }
}