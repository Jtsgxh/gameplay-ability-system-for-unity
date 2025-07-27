using System;
using System.Linq;
using UnityEngine;
using GAS.General;

namespace GAS.Runtime
{
    /// <summary>
    /// 游戏标签，GAS系统中用于标识和分类的核心数据结构
    /// </summary>
    /// <remarks>
    /// GameplayTag使用点分层次结构（如"Combat.Damage.Fire"），支持：
    /// - 层次化标签管理
    /// - 祖先标签继承关系
    /// - 高效的哈希比较
    /// - 标签匹配和筛选
    /// 
    /// 标签在GAS中的用途：
    /// - 技能分类和条件检查
    /// - 游戏效果的标识和过滤
    /// - 状态管理和逻辑控制
    /// - 免疫和抗性系统
    /// </remarks>
    /// <example>
    /// // 创建层次化标签
    /// var fireTag = new GameplayTag("Combat.Damage.Fire");
    /// var combatTag = new GameplayTag("Combat");
    /// 
    /// // 检查继承关系
    /// bool isFireDamage = fireTag.IsDescendantOf(combatTag); // true
    /// </example>
    [Serializable]
    public struct GameplayTag
    {
        [SerializeField] private string _name;
        [SerializeField] private int _hashCode;
        [SerializeField] private string _shortName;
        [SerializeField] private int[] _ancestorHashCodes;
        [SerializeField] private string[] _ancestorNames;

        /// <summary>
        /// 创建游戏标签实例
        /// </summary>
        /// <param name="name">标签名称，使用点分层次结构（如"Combat.Damage.Fire"）</param>
        /// <remarks>
        /// 构造过程会：
        /// 1. 解析层次结构，生成祖先标签数组
        /// 2. 计算稳定哈希值用于快速比较
        /// 3. 提取短名称（最后一级标签名）
        /// 
        /// 标签名称建议使用英文和点号，避免特殊字符。
        /// </remarks>
        /// <example>
        /// // 创建技能冷却标签
        /// var cooldownTag = new GameplayTag("Ability.Cooldown.Attack");
        /// // cooldownTag.ShortName = "Attack"
        /// // cooldownTag.AncestorNames = ["Ability", "Ability.Cooldown"]
        /// </example>
        public GameplayTag(string name)
        {
            _name = name;
            _hashCode = StringHashUtil.GetStableHashCode(name);

            var tags = name.Split('.');
            // if (tags.Length > GasDefine.GAS_TAG_MAX_GENERATIONS)
            //     throw new Exception(
            //         $"GameplayTag {name} has more than {GasDefine.GAS_TAG_MAX_GENERATIONS} generations");

            _ancestorNames = new string[tags.Length - 1];
            _ancestorHashCodes = new int[tags.Length - 1];
            var i = 0;
            var ancestorTag = "";
            while (i < tags.Length - 1)
            {
                ancestorTag += tags[i];
                _ancestorHashCodes[i] = StringHashUtil.GetStableHashCode(ancestorTag);
                _ancestorNames[i] = ancestorTag;
                ancestorTag += ".";
                i++;
            }

            _shortName = tags.Last();
        }

        /// <summary>
        /// 获取完整的标签名称
        /// </summary>
        /// <remarks>
        /// 返回创建时传入的完整标签名称，主要用于显示和调试。
        /// 实际比较操作使用HashCode进行，性能更高。
        /// </remarks>
        /// <example>
        /// var tag = new GameplayTag("Combat.Damage.Fire");
        /// Debug.Log(tag.Name); // "Combat.Damage.Fire"
        /// </example>
        public string Name => _name;

        /// <summary>
        /// 获取标签的短名称（最后一级名称）
        /// </summary>
        /// <remarks>
        /// 从完整标签名中提取的最后一部分，常用于UI显示。
        /// 例如"Combat.Damage.Fire"的短名称是"Fire"。
        /// </remarks>
        /// <example>
        /// var tag = new GameplayTag("Status.Buff.Shield");
        /// Debug.Log(tag.ShortName); // "Shield"
        /// </example>
        public string ShortName => _shortName;

        /// <summary>
        /// 获取标签的稳定哈希值
        /// </summary>
        /// <remarks>
        /// 用于标签比较的核心属性，使用稳定哈希算法确保跨平台一致性。
        /// 所有标签比较操作实际上都是通过比较哈希值完成的。
        /// </remarks>
        /// <example>
        /// var tag1 = new GameplayTag("Combat.Attack");
        /// var tag2 = new GameplayTag("Combat.Attack");
        /// bool same = tag1.HashCode == tag2.HashCode; // true
        /// </example>
        public int HashCode => _hashCode;

        /// <summary>
        /// 获取所有祖先标签的名称数组
        /// </summary>
        /// <remarks>
        /// 包含从根标签到父标签的所有层级名称。
        /// 例如"A.B.C"的祖先名称为["A", "A.B"]。
        /// 主要用于调试和层次结构分析。
        /// </remarks>
        /// <example>
        /// var tag = new GameplayTag("Combat.Damage.Fire");
        /// // tag.AncestorNames = ["Combat", "Combat.Damage"]
        /// </example>
        public string[] AncestorNames => _ancestorNames;

        /// <summary>
        /// 检查是否为根标签（没有父标签）
        /// </summary>
        /// <remarks>
        /// 根标签是最顶层的标签，没有点分隔符。
        /// 例如"Combat"是根标签，"Combat.Attack"不是。
        /// </remarks>
        /// <example>
        /// var rootTag = new GameplayTag("Combat");
        /// var childTag = new GameplayTag("Combat.Attack");
        /// Debug.Log(rootTag.Root);   // true
        /// Debug.Log(childTag.Root);  // false
        /// </example>
        public bool Root => _ancestorHashCodes.Length == 0;

        /// <summary>
        /// 获取所有祖先标签的哈希值数组
        /// </summary>
        /// <remarks>
        /// 用于高效的祖先标签匹配，避免字符串比较的性能开销。
        /// 与AncestorNames一一对应，但使用哈希值进行快速查找。
        /// </remarks>
        /// <example>
        /// var tag = new GameplayTag("Combat.Damage.Fire");
        /// // 包含"Combat"和"Combat.Damage"的哈希值
        /// </example>
        public int[] AncestorHashCodes => _ancestorHashCodes;

        /// <summary>
        /// 检查当前标签是否是指定标签的后代
        /// </summary>
        /// <param name="other">要检查的祖先标签</param>
        /// <returns>如果当前标签是other的后代则返回true</returns>
        /// <remarks>
        /// 后代关系基于标签的层次结构：
        /// - "Combat.Damage.Fire"是"Combat"的后代
        /// - "Combat.Damage.Fire"是"Combat.Damage"的后代
        /// - "Combat"不是"Combat.Damage"的后代
        /// 
        /// 使用哈希值进行高效匹配，避免字符串操作。
        /// </remarks>
        /// <example>
        /// var fireTag = new GameplayTag("Combat.Damage.Fire");
        /// var combatTag = new GameplayTag("Combat");
        /// var healTag = new GameplayTag("Heal");
        /// 
        /// bool isChild = fireTag.IsDescendantOf(combatTag); // true
        /// bool notChild = fireTag.IsDescendantOf(healTag);  // false
        /// </example>
        public bool IsDescendantOf(GameplayTag other)
        {
            return _ancestorHashCodes.Contains(other.HashCode);
        }

        /// <summary>
        /// 重写对象相等性检查
        /// </summary>
        /// <param name="obj">要比较的对象</param>
        /// <returns>如果对象是相同的GameplayTag则返回true</returns>
        /// <remarks>
        /// 基于哈希值进行比较，确保性能和准确性。
        /// </remarks>
        public override bool Equals(object obj)
        {
            return obj is GameplayTag tag && this == tag;
        }

        /// <summary>
        /// 重写哈希值获取方法
        /// </summary>
        /// <returns>标签的稳定哈希值</returns>
        /// <remarks>
        /// 返回内部计算的稳定哈希值，用于字典和集合操作。
        /// </remarks>
        public override int GetHashCode()
        {
            return HashCode;
        }

        /// <summary>
        /// 标签相等性比较运算符
        /// </summary>
        /// <param name="x">第一个标签</param>
        /// <param name="y">第二个标签</param>
        /// <returns>如果两个标签相等则返回true</returns>
        /// <remarks>
        /// 基于哈希值进行快速比较，相同名称的标签总是相等。
        /// </remarks>
        public static bool operator ==(GameplayTag x, GameplayTag y)
        {
            return x.HashCode == y.HashCode;
        }

        /// <summary>
        /// 标签不等性比较运算符
        /// </summary>
        /// <param name="x">第一个标签</param>
        /// <param name="y">第二个标签</param>
        /// <returns>如果两个标签不相等则返回true</returns>
        public static bool operator !=(GameplayTag x, GameplayTag y)
        {
            return x.HashCode != y.HashCode;
        }

        /// <summary>
        /// 检查当前标签是否包含指定标签
        /// </summary>
        /// <param name="tag">要检查的标签</param>
        /// <returns>如果当前标签包含指定标签则返回true</returns>
        /// <remarks>
        /// "包含"的定义：
        /// 1. 当前标签与指定标签完全相同
        /// 2. 指定标签是当前标签的祖先标签
        /// 
        /// 这个方法实现了标签的层次匹配逻辑，常用于技能条件检查。
        /// </remarks>
        /// <example>
        /// var fireTag = new GameplayTag("Combat.Damage.Fire");
        /// var combatTag = new GameplayTag("Combat");
        /// var iceTag = new GameplayTag("Combat.Damage.Ice");
        /// 
        /// bool hasFireTag = fireTag.HasTag(fireTag);    // true (相同)
        /// bool hasCombatTag = fireTag.HasTag(combatTag); // true (祖先)
        /// bool hasIceTag = fireTag.HasTag(iceTag);       // false (不相关)
        /// </example>
        public bool HasTag(GameplayTag tag)
        {
            foreach (var ancestorHashCode in _ancestorHashCodes)
                if (ancestorHashCode == tag.HashCode)
                    return true;

            return this == tag;
        }
    }
}