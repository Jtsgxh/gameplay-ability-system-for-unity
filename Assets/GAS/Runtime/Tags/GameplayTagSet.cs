using System;

namespace GAS.Runtime
{
    /// <summary>
    /// 不可变游戏标签集合，用于高性能的标签集合操作
    /// </summary>
    /// <remarks>
    /// 当标签集合稳定且不需要运行时修改时使用此结构体，性能优于GameplayTagContainer。
    /// 
    /// 主要特点：
    /// - 只读结构体，创建后无法修改
    /// - 内存效率更高，无额外的动态分配
    /// - 快速的标签匹配操作
    /// - 支持层次化标签检查
    /// - 零垃圾回收压力
    /// 
    /// 适用场景：
    /// - 技能配置中的固定标签需求
    /// - 游戏效果的固定标签设置
    /// - 静态条件检查
    /// - 配置数据中的标签定义
    /// </remarks>
    /// <example>
    /// // 从字符串数组创建
    /// var tags1 = new GameplayTagSet(new string[] {
    ///     "Combat.Attack",
    ///     "Status.Alive"
    /// });
    /// 
    /// // 从GameplayTag数组创建
    /// var tags2 = new GameplayTagSet(
    ///     new GameplayTag("Magic.Fire"),
    ///     new GameplayTag("Magic.Ice")
    /// );
    /// 
    /// // 检查标签
    /// bool hasFire = tags2.HasTag(new GameplayTag("Magic.Fire")); // true
    /// bool hasMagic = tags2.HasTag(new GameplayTag("Magic"));     // true (层次匹配)
    /// </example>
    public readonly struct GameplayTagSet
    {
        /// <summary>
        /// 获取标签集合中的所有标签数组
        /// </summary>
        /// <remarks>
        /// 只读字段，包含集合中的所有标签。
        /// 由于是不可变结构体，此数组创建后不会被修改。
        /// </remarks>
        public readonly GameplayTag[] Tags;
        
        /// <summary>
        /// 检查标签集合是否为空
        /// </summary>
        /// <remarks>
        /// 空集合在逻辑运算中有特殊意义：
        /// - HasAllTags对空集合总是返回true
        /// - HasAnyTags对空集合总是返回true
        /// - HasNoneTags对空集合总是返回true
        /// </remarks>
        /// <example>
        /// var emptySet = new GameplayTagSet();
        /// var nonEmptySet = new GameplayTagSet(new GameplayTag("Test"));
        /// 
        /// Debug.Log(emptySet.Empty);    // true
        /// Debug.Log(nonEmptySet.Empty); // false
        /// </example>
        public bool Empty => Tags.Length == 0;
        
        /// <summary>
        /// 从字符串数组创建游戏标签集合
        /// </summary>
        /// <param name="tagNames">标签名称字符串数组</param>
        /// <remarks>
        /// 将字符串数组转换为GameplayTag数组创建集合。
        /// 这是从配置文件或序列化数据创建标签集合的常用方式。
        /// 每个字符串会被转换为对应的GameplayTag实例。
        /// </remarks>
        /// <example>
        /// // 从配置创建标签集合
        /// string[] configTags = { "Combat.Melee", "Status.Buff" };
        /// var tagSet = new GameplayTagSet(configTags);
        /// </example>
        public GameplayTagSet(string[] tagNames)
        {
            Tags = new GameplayTag[tagNames.Length];
            for (var i = 0; i < tagNames.Length; i++)
            {
                Tags[i] = new GameplayTag(tagNames[i]);
            }
        }
        
        /// <summary>
        /// 从GameplayTag数组创建游戏标签集合
        /// </summary>
        /// <param name="tags">GameplayTag数组，如果为null则创建空集合</param>
        /// <remarks>
        /// 直接使用提供的GameplayTag数组创建集合。
        /// 如果传入null，会创建一个空的标签集合。
        /// 这是创建标签集合最直接和高效的方式。
        /// </remarks>
        /// <example>
        /// // 创建战斗相关标签集合
        /// var combatTags = new GameplayTagSet(
        ///     new GameplayTag("Combat.Attack"),
        ///     new GameplayTag("Combat.Defense")
        /// );
        /// 
        /// // 创建空集合
        /// var emptySet = new GameplayTagSet((GameplayTag[])null);
        /// </example>
        public GameplayTagSet(params GameplayTag[] tags)
        {
            Tags = tags?? Array.Empty<GameplayTag>();
        }
        
        /// <summary>
        /// 检查集合是否包含指定标签（支持层次匹配）
        /// </summary>
        /// <param name="tag">要检查的标签</param>
        /// <returns>如果集合包含指定标签或其后代标签则返回true</returns>
        /// <remarks>
        /// 使用层次化匹配逻辑：
        /// - 如果集合中有完全相同的标签，返回true
        /// - 如果集合中有指定标签的后代标签，返回true
        /// - 例如：集合中有"Combat.Attack.Sword"，查询"Combat"会返回true
        /// 
        /// 性能特点：O(n)时间复杂度，其中n是集合中标签的数量。
        /// </remarks>
        /// <example>
        /// var tagSet = new GameplayTagSet(
        ///     new GameplayTag("Combat.Attack.Sword"),
        ///     new GameplayTag("Magic.Fire")
        /// );
        /// 
        /// bool exact = tagSet.HasTag(new GameplayTag("Magic.Fire"));    // true
        /// bool parent = tagSet.HasTag(new GameplayTag("Combat"));       // true
        /// bool missing = tagSet.HasTag(new GameplayTag("Defense"));    // false
        /// </example>
        public bool HasTag(GameplayTag tag)
        {
            foreach (var t in Tags)
            {
                if (t.HasTag(tag)) return true;
            }

            return false;
        }
        
        /// <summary>
        /// 检查当前集合是否包含另一个标签集合中的所有标签
        /// </summary>
        /// <param name="other">要检查的标签集合</param>
        /// <returns>如果当前集合包含other中的所有标签则返回true</returns>
        /// <remarks>
        /// 空集合总是返回true（逻辑上，包含所有的"无"）。
        /// 使用层次化匹配，每个标签都必须匹配才返回true。
        /// 常用于检查是否满足所有前置条件。
        /// </remarks>
        /// <example>
        /// var playerTags = new GameplayTagSet(
        ///     new GameplayTag("Class.Warrior"),
        ///     new GameplayTag("Status.Alive"),
        ///     new GameplayTag("Equipment.Sword")
        /// );
        /// 
        /// var requirements = new GameplayTagSet(
        ///     new GameplayTag("Class.Warrior"),
        ///     new GameplayTag("Status.Alive")
        /// );
        /// 
        /// bool canUse = playerTags.HasAllTags(requirements); // true
        /// </example>
        public bool HasAllTags(GameplayTagSet other)
        {
            return HasAllTags(other.Tags);
        }
        
        /// <summary>
        /// 检查当前集合是否包含指定标签数组中的所有标签
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果当前集合包含数组中的所有标签则返回true</returns>
        /// <remarks>
        /// HasAllTags方法的可变参数版本，方便直接传递多个标签。
        /// 空数组总是返回true。
        /// </remarks>
        /// <example>
        /// bool canCast = playerTags.HasAllTags(
        ///     new GameplayTag("Class.Mage"),
        ///     new GameplayTag("Resource.Mana")
        /// );
        /// </example>
        public bool HasAllTags(params GameplayTag[] tags)
        {
            foreach (var tag in tags)
            {
                if (!HasTag(tag)) return false;
            }

            return true;
        }
        
        /// <summary>
        /// 检查当前集合是否包含另一个标签集合中的任意一个标签
        /// </summary>
        /// <param name="other">要检查的标签集合</param>
        /// <returns>如果当前集合包含other中的任意一个标签则返回true</returns>
        /// <remarks>
        /// 空集合总是返回false（没有任何标签可以匹配）。
        /// 只要有一个标签匹配就返回true，使用层次化匹配。
        /// 常用于检查是否有任意一种状态或能力。
        /// </remarks>
        /// <example>
        /// var playerTags = new GameplayTagSet(
        ///     new GameplayTag("Status.Buff.Strength")
        /// );
        /// 
        /// var buffTags = new GameplayTagSet(
        ///     new GameplayTag("Status.Buff.Strength"),
        ///     new GameplayTag("Status.Buff.Speed")
        /// );
        /// 
        /// bool hasBuff = playerTags.HasAnyTags(buffTags); // true
        /// </example>
        public bool HasAnyTags(GameplayTagSet other)
        {
            return HasAnyTags(other.Tags);
        }

        /// <summary>
        /// 检查当前集合是否包含指定标签数组中的任意一个标签
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果当前集合包含数组中的任意一个标签则返回true</returns>
        /// <remarks>
        /// HasAnyTags方法的可变参数版本，方便直接传递多个标签。
        /// 空数组总是返回false。
        /// </remarks>
        /// <example>
        /// bool hasWeapon = playerTags.HasAnyTags(
        ///     new GameplayTag("Equipment.Sword"),
        ///     new GameplayTag("Equipment.Bow")
        /// );
        /// </example>
        public bool HasAnyTags(params GameplayTag[] tags)
        {
            foreach (var tag in tags)
            {
                if (HasTag(tag)) return true;
            }

            return false;
        }
        
        /// <summary>
        /// 检查当前集合是否不包含另一个标签集合中的任何标签
        /// </summary>
        /// <param name="other">要检查的标签集合</param>
        /// <returns>如果当前集合不包含other中的任何标签则返回true</returns>
        /// <remarks>
        /// 这是HasAnyTags的逻辑反操作。
        /// 空集合总是返回true（逻辑上，不包含任何"无"）。
        /// 常用于检查是否没有某些限制条件或负面状态。
        /// </remarks>
        /// <example>
        /// var playerTags = new GameplayTagSet(
        ///     new GameplayTag("Status.Healthy")
        /// );
        /// 
        /// var debuffTags = new GameplayTagSet(
        ///     new GameplayTag("Status.Debuff.Poison"),
        ///     new GameplayTag("Status.Debuff.Slow")
        /// );
        /// 
        /// bool noDebuffs = playerTags.HasNoneTags(debuffTags); // true
        /// </example>
        public bool HasNoneTags(GameplayTagSet other)
        {
            return HasNoneTags(other.Tags);
        }
        
        /// <summary>
        /// 检查当前集合是否不包含指定标签数组中的任何标签
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果当前集合不包含数组中的任何标签则返回true</returns>
        /// <remarks>
        /// HasNoneTags方法的可变参数版本，方便直接传递多个标签。
        /// 空数组总是返回true。
        /// </remarks>
        /// <example>
        /// bool canMove = playerTags.HasNoneTags(
        ///     new GameplayTag("Status.Stun"),
        ///     new GameplayTag("Status.Root")
        /// );
        /// </example>
        public bool HasNoneTags(params GameplayTag[] tags)
        {
            foreach (var tag in tags)
            {
                if (HasTag(tag)) return false;
            }

            return true;
        }
    }
}