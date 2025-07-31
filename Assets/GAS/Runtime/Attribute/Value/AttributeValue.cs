using Sirenix.OdinInspector;

namespace GAS.Runtime
{
    /// <summary>
    /// 属性计算模式，定义如何处理多个修饰符作用于同一属性的情况
    /// </summary>
    /// <remarks>
    /// 计算模式直接影响GameplayEffect修饰符的叠加行为：
    /// - Stacking: 标准模式，所有修饰符按顺序叠加计算
    /// - MinValueOnly: 竞争模式，多个修饰符中只有最小值生效（如减速效果）
    /// - MaxValueOnly: 竞争模式，多个修饰符中只有最大值生效（如护盾效果）
    /// </remarks>
    public enum CalculateMode
    {
        /// <summary>
        /// 叠加计算 - 所有修饰符按顺序累积应用
        /// </summary>
        /// <remarks>
        /// 最常用的计算模式，修饰符按照以下顺序执行：
        /// 1. 基础值作为起始值
        /// 2. 按效果应用的顺序逐一应用修饰符
        /// 3. 每个修饰符都会影响最终结果
        /// 
        /// 适用场景：大部分属性（生命值、攻击力、移动速度等）
        /// </remarks>
        [LabelText(SdfIconType.Stack, Text = "叠加计算")]
        Stacking,

        /// <summary>
        /// 取最小值 - 多个修饰符中只有最小值生效
        /// </summary>
        /// <remarks>
        /// 竞争性计算模式，只有产生最小值的修饰符会生效。
        /// 主要用于限制性效果，防止多个负面效果叠加过度。
        /// 
        /// 注意：只支持Override操作，其他操作会被忽略
        /// 
        /// 适用场景：
        /// - 减速效果（多个减速只取最强的）
        /// - 伤害减免上限
        /// - 资源消耗增加惩罚
        /// </remarks>
        [LabelText(SdfIconType.GraphDownArrow, Text = "取最小值")]
        MinValueOnly,

        /// <summary>
        /// 取最大值 - 多个修饰符中只有最大值生效
        /// </summary>
        /// <remarks>
        /// 竞争性计算模式，只有产生最大值的修饰符会生效。
        /// 主要用于保护性效果，防止多个正面效果叠加过强。
        /// 
        /// 注意：只支持Override操作，其他操作会被忽略
        /// 
        /// 适用场景：
        /// - 护盾效果（多个护盾只取最强的）
        /// - 伤害增幅上限
        /// - 移动速度加成上限
        /// </remarks>
        [LabelText(SdfIconType.GraphUpArrow, Text = "取最大值")]
        MaxValueOnly,
    }

    /// <summary>
    /// 属性值结构体，封装属性的基础值、当前值和计算配置
    /// </summary>
    /// <remarks>
    /// AttributeValue是属性系统的核心数据结构，包含：
    /// 
    /// 1. **值管理**：
    ///    - BaseValue：属性的基础值，不受GameplayEffect影响
    ///    - CurrentValue：当前实际值，受所有生效的修饰符影响
    /// 
    /// 2. **计算配置**：
    ///    - CalculateMode：定义修饰符如何叠加（叠加/最小值/最大值）
    ///    - SupportedOperation：定义支持的运算类型（加减乘除覆盖）
    /// 
    /// 3. **范围限制**：
    ///    - MinValue/MaxValue：属性值的有效范围
    /// 
    /// 设计特点：
    /// - 不可变配置：计算模式和支持的操作在创建时确定，运行时不可更改
    /// - 高性能：使用struct避免堆分配，适合频繁计算
    /// - 类型安全：通过枚举确保运算类型的有效性
    /// </remarks>
    public struct AttributeValue
    {
        /// <summary>
        /// 初始化属性值
        /// </summary>
        /// <param name="baseValue">基础值，属性的初始数值</param>
        /// <param name="calculateMode">计算模式，定义修饰符如何叠加，默认为叠加计算</param>
        /// <param name="supportedOperation">支持的运算操作，默认支持所有运算类型</param>
        /// <param name="minValue">最小值限制，默认为负无穷</param>
        /// <param name="maxValue">最大值限制，默认为正无穷</param>
        /// <remarks>
        /// 初始化时CurrentValue会被设置为与BaseValue相同的值。
        /// 
        /// 参数选择建议：
        /// - calculateMode：大部分属性使用Stacking，特殊机制使用Min/MaxValueOnly
        /// - supportedOperation：根据游戏设计限制可用的运算类型
        /// - minValue/maxValue：设置合理的属性范围，防止数值异常
        /// </remarks>
        public AttributeValue(float baseValue,
            CalculateMode calculateMode = CalculateMode.Stacking,
            SupportedOperation supportedOperation = SupportedOperation.All,
            float minValue = float.MinValue, float maxValue = float.MaxValue)
        {
            BaseValue = baseValue;
            SupportedOperation = supportedOperation;
            CurrentValue = baseValue;
            CalculateMode = calculateMode;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        /// <summary>
        /// 获取计算模式
        /// </summary>
        /// <value>修饰符叠加的计算模式</value>
        public CalculateMode CalculateMode { get; }
        
        /// <summary>
        /// 获取支持的运算操作
        /// </summary>
        /// <value>此属性支持的GameplayEffect运算类型</value>
        public SupportedOperation SupportedOperation { get; }

        /// <summary>
        /// 获取或设置基础值
        /// </summary>
        /// <value>属性的基础数值，不受GameplayEffect修饰符影响</value>
        /// <remarks>
        /// 基础值的变化会触发AttributeAggregator重新计算CurrentValue。
        /// 只有Instant类型的GameplayEffect可以直接修改基础值。
        /// </remarks>
        public float BaseValue { get; private set; }
        
        /// <summary>
        /// 获取或设置当前值
        /// </summary>
        /// <value>考虑所有修饰符后的最终属性值</value>
        /// <remarks>
        /// CurrentValue是经过所有生效的GameplayEffect修饰符计算后的最终值。
        /// 该值由AttributeAggregator自动计算和维护，通常不应直接修改。
        /// </remarks>
        public float CurrentValue { get; private set; }

        /// <summary>
        /// 获取或设置最小值限制
        /// </summary>
        /// <value>属性值的下限</value>
        public float MinValue { get; private set; }
        
        /// <summary>
        /// 获取或设置最大值限制
        /// </summary>
        /// <value>属性值的上限</value>
        public float MaxValue { get; private set; }

        /// <summary>
        /// 直接设置当前值，忽略最小值和最大值限制
        /// </summary>
        /// <param name="value">要设置的新值</param>
        /// <remarks>
        /// 此方法会绕过所有限制直接设置当前值，主要由AttributeAggregator内部使用。
        /// 一般情况下不建议直接调用，应该通过GameplayEffect来修改属性值。
        /// 
        /// 使用场景：
        /// - AttributeAggregator计算最终值时
        /// - 特殊的游戏逻辑需要强制设置属性值
        /// - 调试和测试目的
        /// </remarks>
        public void SetCurrentValue(float value)
        {
            CurrentValue = value;
        }

        /// <summary>
        /// 设置基础值
        /// </summary>
        /// <param name="value">新的基础值</param>
        /// <remarks>
        /// 修改基础值会触发属性重新计算，影响最终的CurrentValue。
        /// 这是修改属性"永久"数值的主要方式。
        /// 
        /// 常见用途：
        /// - 角色升级时提升属性基础值
        /// - 装备穿戴/卸下影响基础属性
        /// - Instant类型的GameplayEffect永久修改属性
        /// </remarks>
        public void SetBaseValue(float value)
        {
            BaseValue = value;
        }

        /// <summary>
        /// 设置最小值限制
        /// </summary>
        /// <param name="min">新的最小值</param>
        /// <remarks>
        /// 更新最小值限制，用于运行时动态调整属性范围。
        /// 注意：此方法不会自动触发属性值重新计算。
        /// </remarks>
        public void SetMinValue(float min)
        {
            MinValue = min;
        }

        /// <summary>
        /// 设置最大值限制
        /// </summary>
        /// <param name="max">新的最大值</param>
        /// <remarks>
        /// 更新最大值限制，用于运行时动态调整属性范围。
        /// 注意：此方法不会自动触发属性值重新计算。
        /// </remarks>
        public void SetMaxValue(float max)
        {
            MaxValue = max;
        }

        /// <summary>
        /// 同时设置最小值和最大值限制
        /// </summary>
        /// <param name="min">新的最小值</param>
        /// <param name="max">新的最大值</param>
        /// <remarks>
        /// 批量更新最小值和最大值限制，比单独调用两次更高效。
        /// 注意：此方法不会自动触发属性值重新计算。
        /// 
        /// 常见用途：
        /// - 角色等级变化时调整属性范围
        /// - 装备或技能影响属性上下限
        /// - 特殊状态下的属性限制
        /// </remarks>
        public void SetMinMaxValue(float min, float max)
        {
            MinValue = min;
            MaxValue = max;
        }

        /// <summary>
        /// 检查是否支持指定的运算操作
        /// </summary>
        /// <param name="operation">要检查的运算操作类型</param>
        /// <returns>如果支持该操作返回true，否则返回false</returns>
        /// <remarks>
        /// 此方法用于验证GameplayEffect修饰符的运算类型是否被当前属性支持。
        /// 不支持的运算会被AttributeAggregator忽略并输出警告。
        /// 
        /// 运算类型限制的目的：
        /// - 防止不合理的运算（如对布尔属性使用乘法）
        /// - 确保游戏逻辑的一致性
        /// - 提早发现配置错误
        /// 
        /// 示例：
        /// ```csharp
        /// // 检查生命值是否支持加法运算
        /// if (healthAttribute.IsSupportOperation(GEOperation.Add))
        /// {
        ///     // 可以应用治疗效果
        /// }
        /// ```
        /// </remarks>
        public bool IsSupportOperation(GEOperation operation)
        {
            return SupportedOperation.HasFlag((SupportedOperation)(1 << (int)operation));
        }
    }
}