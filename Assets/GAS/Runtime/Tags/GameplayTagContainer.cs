using System.Collections.Generic;
using System.Linq;

namespace GAS.Runtime
{
    /// <summary>
    /// 动态游戏标签容器，用于管理可变的标签集合
    /// </summary>
    /// <remarks>
    /// 当标签集合需要频繁变化时使用此类。
    /// 与GameplayTagSet相比，此类支持运行时添加和移除标签，但性能略低。
    /// 
    /// 主要功能：
    /// - 动态添加和移除标签
    /// - 标签包含性检查（单个、全部、任意、无）
    /// - 支持标签集合的批量操作
    /// - 层次化标签匹配
    /// 
    /// 适用场景：
    /// - 角色状态标签管理
    /// - 动态效果标签
    /// - 临时标签集合
    /// </remarks>
    /// <example>
    /// // 创建动态标签容器
    /// var container = new GameplayTagContainer(
    ///     new GameplayTag("Status.Healthy"),
    ///     new GameplayTag("Class.Warrior")
    /// );
    /// 
    /// // 添加新状态
    /// container.AddTag(new GameplayTag("Status.Buff.Strength"));
    /// 
    /// // 检查状态
    /// if (container.HasTag(new GameplayTag("Status.Buff")))
    /// {
    ///     Debug.Log("角色有增益状态");
    /// }
    /// </example>
    public class GameplayTagContainer
    {
        /// <summary>
        /// 获取容器中的所有标签列表
        /// </summary>
        /// <remarks>
        /// 返回内部标签列表的直接引用，修改此列表会影响容器状态。
        /// 建议使用容器提供的方法来操作标签，而不是直接修改此列表。
        /// </remarks>
        public List<GameplayTag> Tags { get; }
        
        /// <summary>
        /// 创建游戏标签容器实例
        /// </summary>
        /// <param name="tags">初始标签数组</param>
        /// <remarks>
        /// 创建一个新的动态标签容器，可以传入初始标签。
        /// 重复的标签会被自动过滤，保证容器中标签的唯一性。
        /// </remarks>
        /// <example>
        /// // 创建包含初始标签的容器
        /// var container = new GameplayTagContainer(
        ///     new GameplayTag("Player.Alive"),
        ///     new GameplayTag("Player.InCombat")
        /// );
        /// </example>
        public GameplayTagContainer(params GameplayTag[] tags)
        {
            Tags = new List<GameplayTag>(tags);
        }

        /// <summary>
        /// 添加单个标签到容器中
        /// </summary>
        /// <param name="tag">要添加的标签</param>
        /// <remarks>
        /// 如果标签已存在，此操作会被忽略，确保容器中标签的唯一性。
        /// 添加操作是O(n)复杂度，因为需要检查重复。
        /// </remarks>
        /// <example>
        /// // 添加状态标签
        /// container.AddTag(new GameplayTag("Status.Poisoned"));
        /// container.AddTag(new GameplayTag("Status.Poisoned")); // 重复添加会被忽略
        /// </example>
        public void AddTag(GameplayTag tag)
        {
            if (Tags.Contains(tag)) return;
            Tags.Add(tag);
        }

        /// <summary>
        /// 从容器中移除指定标签
        /// </summary>
        /// <param name="tag">要移除的标签</param>
        /// <remarks>
        /// 如果标签不存在，此操作不会产生任何效果。
        /// 移除操作是O(n)复杂度。
        /// </remarks>
        /// <example>
        /// // 移除状态标签
        /// container.RemoveTag(new GameplayTag("Status.Poisoned"));
        /// </example>
        public void RemoveTag(GameplayTag tag)
        {
            Tags.Remove(tag);
        }

        /// <summary>
        /// 批量添加标签集合中的所有标签
        /// </summary>
        /// <param name="tagSet">要添加的标签集合</param>
        /// <remarks>
        /// 将标签集合中的所有标签逐一添加到容器中。
        /// 重复标签会被自动忽略。空集合不会执行任何操作。
        /// </remarks>
        /// <example>
        /// // 批量添加状态标签
        /// var buffTags = new GameplayTagSet(
        ///     new GameplayTag("Buff.Strength"),
        ///     new GameplayTag("Buff.Speed")
        /// );
        /// container.AddTag(buffTags);
        /// </example>
        public void AddTag(GameplayTagSet tagSet)
        {
            if(tagSet.Empty) return;
            foreach (var tag in tagSet.Tags) AddTag(tag);
        }

        /// <summary>
        /// 批量移除标签集合中的所有标签
        /// </summary>
        /// <param name="tagSet">要移除的标签集合</param>
        /// <remarks>
        /// 将标签集合中的所有标签逐一从容器中移除。
        /// 不存在的标签会被自动忽略。空集合不会执行任何操作。
        /// </remarks>
        /// <example>
        /// // 批量移除增益标签
        /// var buffTags = new GameplayTagSet(
        ///     new GameplayTag("Buff.Strength"),
        ///     new GameplayTag("Buff.Speed")
        /// );
        /// container.RemoveTag(buffTags);
        /// </example>
        public void RemoveTag(GameplayTagSet tagSet)
        {
            if(tagSet.Empty) return;
            foreach (var tag in tagSet.Tags) RemoveTag(tag);
        }

        /// <summary>
        /// 检查容器是否包含指定标签（支持层次匹配）
        /// </summary>
        /// <param name="tag">要检查的标签</param>
        /// <returns>如果容器包含指定标签或其后代标签则返回true</returns>
        /// <remarks>
        /// 使用层次化匹配逻辑：
        /// - 如果容器中有完全相同的标签，返回true
        /// - 如果容器中有指定标签的后代标签，返回true
        /// - 例如：容器中有"Combat.Attack.Sword"，查询"Combat"会返回true
        /// </remarks>
        /// <example>
        /// var container = new GameplayTagContainer(
        ///     new GameplayTag("Combat.Attack.Sword")
        /// );
        /// 
        /// bool exact = container.HasTag(new GameplayTag("Combat.Attack.Sword")); // true
        /// bool parent = container.HasTag(new GameplayTag("Combat.Attack"));      // true
        /// bool root = container.HasTag(new GameplayTag("Combat"));              // true
        /// bool other = container.HasTag(new GameplayTag("Magic"));              // false
        /// </example>
        public bool HasTag(GameplayTag tag)
        {
            return Tags.Any(t => t.HasTag(tag));
        }

        /// <summary>
        /// 检查容器是否包含标签集合中的所有标签
        /// </summary>
        /// <param name="other">要检查的标签集合</param>
        /// <returns>如果容器包含集合中的所有标签则返回true</returns>
        /// <remarks>
        /// 空标签集合总是返回true（逻辑上，包含所有的"无"）。
        /// 使用层次化匹配，每个标签都必须匹配才返回true。
        /// </remarks>
        /// <example>
        /// var container = new GameplayTagContainer(
        ///     new GameplayTag("Combat.Attack"),
        ///     new GameplayTag("Status.Alive")
        /// );
        /// 
        /// var requiredTags = new GameplayTagSet(
        ///     new GameplayTag("Combat"),
        ///     new GameplayTag("Status.Alive")
        /// );
        /// 
        /// bool hasAll = container.HasAllTags(requiredTags); // true
        /// </example>
        public bool HasAllTags(GameplayTagSet other)
        {
            return other.Empty || other.Tags.All(HasTag);
        }

        /// <summary>
        /// 检查容器是否包含指定标签数组中的所有标签
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果容器包含数组中的所有标签则返回true</returns>
        /// <remarks>
        /// HasAllTags方法的可变参数版本，方便直接传递多个标签。
        /// 空数组总是返回true。
        /// </remarks>
        /// <example>
        /// bool hasAll = container.HasAllTags(
        ///     new GameplayTag("Combat"),
        ///     new GameplayTag("Status.Alive")
        /// );
        /// </example>
        public bool HasAllTags(params GameplayTag[] tags)
        {
            return tags.All(HasTag);
        }

        /// <summary>
        /// 检查容器是否包含标签集合中的任意一个标签
        /// </summary>
        /// <param name="other">要检查的标签集合</param>
        /// <returns>如果容器包含集合中的任意一个标签则返回true</returns>
        /// <remarks>
        /// 空标签集合总是返回true（逻辑上的"任意"包括"无"）。
        /// 只要有一个标签匹配就返回true，使用层次化匹配。
        /// 常用于检查是否有任意一种状态或能力。
        /// </remarks>
        /// <example>
        /// var container = new GameplayTagContainer(
        ///     new GameplayTag("Status.Debuff.Poison")
        /// );
        /// 
        /// var debuffTags = new GameplayTagSet(
        ///     new GameplayTag("Status.Debuff.Poison"),
        ///     new GameplayTag("Status.Debuff.Slow")
        /// );
        /// 
        /// bool hasAnyDebuff = container.HasAnyTags(debuffTags); // true
        /// </example>
        public bool HasAnyTags(GameplayTagSet other)
        {
            return other.Empty || other.Tags.Any(HasTag);
        }

        /// <summary>
        /// 检查容器是否包含指定标签数组中的任意一个标签
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果容器包含数组中的任意一个标签则返回true</returns>
        /// <remarks>
        /// HasAnyTags方法的可变参数版本，方便直接传递多个标签。
        /// 空数组总是返回false（与集合版本不同）。
        /// </remarks>
        /// <example>
        /// bool hasAnyBuff = container.HasAnyTags(
        ///     new GameplayTag("Buff.Strength"),
        ///     new GameplayTag("Buff.Speed")
        /// );
        /// </example>
        public bool HasAnyTags(params GameplayTag[] tags)
        {
            return tags.Any(HasTag);
        }

        /// <summary>
        /// 检查容器是否不包含标签集合中的任何标签
        /// </summary>
        /// <param name="other">要检查的标签集合</param>
        /// <returns>如果容器不包含集合中的任何标签则返回true</returns>
        /// <remarks>
        /// 空标签集合总是返回true（逻辑上，不包含任何"无"）。
        /// 这是HasAnyTags的逻辑反操作，常用于检查是否没有某些限制条件。
        /// 例如：检查角色是否没有任何负面状态。
        /// </remarks>
        /// <example>
        /// var container = new GameplayTagContainer(
        ///     new GameplayTag("Status.Healthy")
        /// );
        /// 
        /// var debuffTags = new GameplayTagSet(
        ///     new GameplayTag("Status.Debuff.Poison"),
        ///     new GameplayTag("Status.Debuff.Slow")
        /// );
        /// 
        /// bool noDebuffs = container.HasNoneTags(debuffTags); // true
        /// </example>
        public bool HasNoneTags(GameplayTagSet other)
        {
            return other.Empty || !other.Tags.Any(HasTag);
        }

        /// <summary>
        /// 检查容器是否不包含指定标签数组中的任何标签
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果容器不包含数组中的任何标签则返回true</returns>
        /// <remarks>
        /// HasNoneTags方法的可变参数版本，方便直接传递多个标签。
        /// 空数组总是返回true。
        /// </remarks>
        /// <example>
        /// bool noCrowdControl = container.HasNoneTags(
        ///     new GameplayTag("Status.Stun"),
        ///     new GameplayTag("Status.Silence")
        /// );
        /// </example>
        public bool HasNoneTags(params GameplayTag[] tags)
        {
            return !tags.Any(HasTag);
        }
    }
}