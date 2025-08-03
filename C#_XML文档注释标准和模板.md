# EX-GAS C# XML 文档注释标准和模板

## 概述

为EX-GAS项目中所有C#代码添加统一、专业的XML文档注释，提高代码可读性和API文档质量。

## 注释标准

### 1. 基本原则

- **完整性**: 所有公共API（public, protected）必须有完整文档
- **准确性**: 注释内容必须与代码实现一致
- **简洁性**: 用词准确，避免冗余
- **中英文**: 支持中英文混合，以英文为主，关键概念用中文补充
- **一致性**: 使用统一的术语和格式

### 2. 必须注释的元素

- [x] 类 (class, struct, interface)
- [x] 方法 (public, protected methods)
- [x] 属性 (public, protected properties)
- [x] 字段 (public, protected fields)
- [x] 事件 (public, protected events)
- [x] 枚举 (enum and enum values)
- [x] 委托 (delegate)

### 3. XML标签使用规范

| 标签 | 用途 | 必需性 |
|------|------|---------|
| `<summary>` | 简要描述 | 必需 |
| `<param>` | 参数说明 | 有参数时必需 |
| `<returns>` | 返回值说明 | 有返回值时必需 |
| `<exception>` | 异常说明 | 可能抛出异常时必需 |
| `<remarks>` | 详细说明 | 可选 |
| `<example>` | 使用示例 | 复杂API推荐 |
| `<see>` | 交叉引用 | 推荐 |
| `<seealso>` | 相关链接 | 推荐 |

## 模板库

### 1. 类模板

#### 1.1 普通类
```csharp
/// <summary>
/// [类的简要描述]
/// </summary>
/// <remarks>
/// [详细说明，包括使用场景、注意事项等]
/// </remarks>
public class ClassName
{
    // ...
}
```

#### 1.2 单例类
```csharp
/// <summary>
/// [单例类的简要描述]
/// </summary>
/// <remarks>
/// 单例模式实现，确保全局只有一个实例。
/// 通过 <see cref="Instance"/> 属性访问实例。
/// </remarks>
public class SingletonClass
{
    /// <summary>
    /// 获取单例实例
    /// </summary>
    /// <value>全局唯一的单例实例</value>
    public static SingletonClass Instance { get; private set; }
}
```

#### 1.3 抽象基类
```csharp
/// <summary>
/// [抽象基类的简要描述]
/// </summary>
/// <remarks>
/// 为 [具体功能] 提供基础实现和通用接口。
/// 子类必须实现 <see cref="AbstractMethod"/> 方法。
/// </remarks>
public abstract class AbstractBaseClass
{
    /// <summary>
    /// [抽象方法描述]
    /// </summary>
    /// <param name="parameter">[参数描述]</param>
    /// <returns>[返回值描述]</returns>
    protected abstract ReturnType AbstractMethod(ParameterType parameter);
}
```

#### 1.4 容器/管理器类
```csharp
/// <summary>
/// [容器类的简要描述]，管理 [管理的对象类型] 的生命周期和操作
/// </summary>
/// <remarks>
/// <para>主要功能：</para>
/// <list type="bullet">
/// <item><description>添加和移除 [对象类型]</description></item>
/// <item><description>查询和筛选操作</description></item>
/// <item><description>生命周期管理</description></item>
/// </list>
/// </remarks>
public class ContainerClass
{
    // ...
}
```

### 2. 方法模板

#### 2.1 普通方法
```csharp
/// <summary>
/// [方法的简要描述]
/// </summary>
/// <param name="param1">[第一个参数的描述]</param>
/// <param name="param2">[第二个参数的描述，如果是可选参数说明默认值]</param>
/// <returns>[返回值描述，包括可能的返回值类型和含义]</returns>
/// <exception cref="ArgumentException">当 <paramref name="param1"/> 为无效值时抛出</exception>
/// <exception cref="InvalidOperationException">当对象处于无效状态时抛出</exception>
/// <remarks>
/// [详细说明，包括使用注意事项、性能考虑等]
/// </remarks>
/// <example>
/// <code>
/// var result = SomeMethod("example", 42);
/// Console.WriteLine(result);
/// </code>
/// </example>
public ReturnType SomeMethod(string param1, int param2 = 0)
{
    // ...
}
```

#### 2.2 Try模式方法
```csharp
/// <summary>
/// 尝试 [执行的操作]
/// </summary>
/// <param name="input">[输入参数描述]</param>
/// <param name="result">如果操作成功，包含 [结果描述]；否则为默认值</param>
/// <returns>如果操作成功返回 <c>true</c>，否则返回 <c>false</c></returns>
/// <remarks>
/// 此方法不会抛出异常，通过返回值指示操作是否成功。
/// </remarks>
public bool TryDoSomething(InputType input, out ResultType result)
{
    // ...
}
```

#### 2.3 事件处理方法
```csharp
/// <summary>
/// 处理 [事件名称] 事件
/// </summary>
/// <param name="sender">事件发送者</param>
/// <param name="args">事件参数，包含 [参数描述]</param>
/// <remarks>
/// 此方法在 [触发条件] 时被调用。
/// </remarks>
private void OnEventHandler(object sender, EventArgsType args)
{
    // ...
}
```

#### 2.4 生命周期方法（Unity）
```csharp
/// <summary>
/// Unity 生命周期方法，在 [生命周期阶段] 调用
/// </summary>
/// <remarks>
/// [该方法的具体作用和执行时机]
/// </remarks>
private void Awake()
{
    // ...
}
```

### 3. 属性模板

#### 3.1 简单属性
```csharp
/// <summary>
/// 获取或设置 [属性描述]
/// </summary>
/// <value>[属性值的详细描述，包括有效范围等]</value>
public PropertyType PropertyName { get; set; }
```

#### 3.2 只读属性
```csharp
/// <summary>
/// 获取 [属性描述]
/// </summary>
/// <value>[属性值的详细描述]</value>
/// <remarks>
/// 此属性为只读，值在 [设置时机] 时确定。
/// </remarks>
public PropertyType ReadOnlyProperty { get; private set; }
```

#### 3.3 计算属性
```csharp
/// <summary>
/// 获取计算得出的 [属性描述]
/// </summary>
/// <value>基于 [计算依据] 计算得出的值</value>
/// <remarks>
/// 此属性每次访问时重新计算，可能影响性能。
/// </remarks>
public PropertyType ComputedProperty
{
    get
    {
        // ...
    }
}
```

### 4. 字段模板

#### 4.1 常量
```csharp
/// <summary>
/// [常量描述]
/// </summary>
/// <remarks>
/// [常量的用途和注意事项]
/// </remarks>
public const int CONSTANT_NAME = 42;
```

#### 4.2 静态只读字段
```csharp
/// <summary>
/// [静态只读字段描述]
/// </summary>
/// <remarks>
/// [字段的用途和初始化说明]
/// </remarks>
public static readonly FieldType StaticReadOnlyField = new FieldType();
```

### 5. 事件模板

```csharp
/// <summary>
/// 当 [触发条件] 时发生
/// </summary>
/// <remarks>
/// 订阅此事件以接收 [事件类型] 通知。
/// 事件在 [触发时机] 触发。
/// </remarks>
public event EventHandler<EventArgsType> EventName;
```

### 6. 枚举模板

```csharp
/// <summary>
/// [枚举的用途描述]
/// </summary>
public enum EnumName
{
    /// <summary>
    /// [枚举值1的描述]
    /// </summary>
    Value1 = 0,
    
    /// <summary>
    /// [枚举值2的描述]
    /// </summary>
    Value2 = 1,
    
    /// <summary>
    /// [枚举值3的描述]
    /// </summary>
    Value3 = 2
}
```

### 7. 接口模板

```csharp
/// <summary>
/// [接口的用途描述]
/// </summary>
/// <remarks>
/// 实现此接口的类需要提供 [功能描述]。
/// <para>主要方法：</para>
/// <list type="bullet">
/// <item><description><see cref="Method1"/> - [方法1描述]</description></item>
/// <item><description><see cref="Method2"/> - [方法2描述]</description></item>
/// </list>
/// </remarks>
public interface IInterfaceName
{
    /// <summary>
    /// [接口方法描述]
    /// </summary>
    /// <param name="parameter">[参数描述]</param>
    /// <returns>[返回值描述]</returns>
    ReturnType Method1(ParameterType parameter);
}
```

## EX-GAS 特定术语和模板

### 1. 核心概念术语

| 英文术语 | 中文术语 | 使用场景 |
|----------|----------|----------|
| GameplayAbilitySystem | GAS 系统 | 核心系统引用 |
| AbilitySystemComponent | ASC 组件 | 组件引用 |
| GameplayEffect | 游戏效果 | 效果系统 |
| GameplayTag | 游戏标签 | 标签系统 |
| Ability | 技能/能力 | 技能系统 |
| AttributeSet | 属性集 | 属性系统 |
| GameplayCue | 游戏提示 | 反馈系统 |

### 2. EX-GAS 专用模板

#### 2.1 ASC容器类
```csharp
/// <summary>
/// [容器名称]，管理 <see cref="AbilitySystemComponent"/> 中的 [管理对象]
/// </summary>
/// <remarks>
/// <para>此容器负责：</para>
/// <list type="bullet">
/// <item><description>[功能1描述]</description></item>
/// <item><description>[功能2描述]</description></item>
/// <item><description>与其他容器的协调</description></item>
/// </list>
/// <para>生命周期由 <see cref="AbilitySystemComponent"/> 管理。</para>
/// </remarks>
public class ContainerName
{
    // ...
}
```

#### 2.2 GameplayEffect 相关
```csharp
/// <summary>
/// [效果描述]，用于 [效果用途]
/// </summary>
/// <remarks>
/// <para>效果类型：[Instant/Duration/Infinite]</para>
/// <para>主要修饰符：</para>
/// <list type="bullet">
/// <item><description>[修饰符1] - [作用描述]</description></item>
/// <item><description>[修饰符2] - [作用描述]</description></item>
/// </list>
/// <para>标签需求：[Required/Block/Grant 标签列表]</para>
/// </remarks>
public class GameplayEffectClass
{
    // ...
}
```

#### 2.3 技能类
```csharp
/// <summary>
/// [技能名称] 技能实现
/// </summary>
/// <remarks>
/// <para>技能特性：</para>
/// <list type="bullet">
/// <item><description>冷却时间：[冷却描述]</description></item>
/// <item><description>消耗资源：[消耗描述]</description></item>
/// <item><description>目标类型：[目标描述]</description></item>
/// <item><description>施放范围：[范围描述]</description></item>
/// </list>
/// <para>继承自 <see cref="AbstractAbility"/>，实现 WHO-DO-WHAT 模式。</para>
/// </remarks>
public class AbilityClass : AbstractAbility
{
    // ...
}
```

#### 2.4 标签相关
```csharp
/// <summary>
/// 管理 GameplayTag 的 [功能描述]
/// </summary>
/// <remarks>
/// <para>支持的标签操作：</para>
/// <list type="bullet">
/// <item><description>添加/移除标签</description></item>
/// <item><description>标签匹配查询</description></item>
/// <item><description>层次结构遍历</description></item>
/// </list>
/// <para>使用点分层次结构（如：Combat.Damage.Fire）。</para>
/// </remarks>
public class TagClass
{
    // ...
}
```

## 注释质量检查清单

### 写作质量
- [ ] 语法正确，无拼写错误
- [ ] 用词准确，避免歧义
- [ ] 简洁明了，避免冗余
- [ ] 专业术语使用一致

### 技术准确性
- [ ] 参数描述与实际参数匹配
- [ ] 返回值描述准确
- [ ] 异常情况完整列出
- [ ] 引用链接正确有效

### 完整性
- [ ] 所有公共API都有注释
- [ ] 复杂逻辑有详细说明
- [ ] 重要注意事项已说明
- [ ] 相关类型已交叉引用

### 一致性
- [ ] 使用统一的术语表
- [ ] 格式风格保持一致
- [ ] 标签使用规范正确
- [ ] 与项目风格匹配

## 常见注释示例

### 1. 事件系统
```csharp
/// <summary>
/// 当属性值发生变化时触发的事件
/// </summary>
/// <remarks>
/// 此事件在属性的 CurrentValue 或 BaseValue 改变时触发。
/// 事件参数包含变化前后的值以及变化原因。
/// </remarks>
public event Action<AttributeChangedEventArgs> OnAttributeChanged;
```

### 2. 工厂模式
```csharp
/// <summary>
/// 创建指定类型的 <see cref="GameplayEffect"/> 实例
/// </summary>
/// <param name="effectType">要创建的效果类型</param>
/// <param name="level">效果等级，影响数值计算</param>
/// <returns>新创建的游戏效果实例</returns>
/// <exception cref="ArgumentException">当 <paramref name="effectType"/> 不是有效的效果类型时抛出</exception>
/// <remarks>
/// 使用对象池优化内存分配，返回的实例可能是复用的。
/// </remarks>
public static GameplayEffect CreateEffect(Type effectType, int level = 1)
{
    // ...
}
```

### 3. 扩展方法
```csharp
/// <summary>
/// 为 <see cref="AbilitySystemComponent"/> 添加检查指定标签的扩展方法
/// </summary>
/// <param name="asc">要检查的 ASC 实例</param>
/// <param name="tag">要检查的游戏标签</param>
/// <returns>如果 ASC 拥有指定标签返回 <c>true</c>，否则返回 <c>false</c></returns>
/// <exception cref="ArgumentNullException">当 <paramref name="asc"/> 或 <paramref name="tag"/> 为 null 时抛出</exception>
public static bool HasTag(this AbilitySystemComponent asc, GameplayTag tag)
{
    // ...
}
```

---

**版本**: v1.0  
**更新时间**: 2024年当前时间  
**适用项目**: EX-GAS Unity Gameplay Ability System