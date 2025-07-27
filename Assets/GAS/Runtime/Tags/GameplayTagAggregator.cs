using System;
using System.Collections.Generic;
using GAS.General;
using UnityEngine.Profiling;

namespace GAS.Runtime
{
    public class GameplayTagAggregator
    {
        private AbilitySystemComponent _owner;

        private Dictionary<GameplayTag, List<object>> _dynamicAddedTags =
            new Dictionary<GameplayTag, List<object>>();

        private Dictionary<GameplayTag, List<object>> _dynamicRemovedTags =
            new Dictionary<GameplayTag, List<object>>();

        private readonly List<GameplayTag> _fixedTags = new List<GameplayTag>();

        private static Pool _pool = new Pool(typeof(List<object>), 1024);

        public GameplayTagAggregator(AbilitySystemComponent owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 检查源对象是否为有效的标签源类型
        /// </summary>
        private static bool IsValidTagSource<T>(T source)
        {
            return source is GameplayEffectSpec || source is AbilitySpec;
        }

        /// <summary>
        /// 安全地从对象池获取List<object>
        /// </summary>
        private static List<object> GetPooledList()
        {
            var list = _pool.Get() as List<object>;
            if (list == null)
            {
                list = new List<object>();
            }
            else
            {
                list.Clear(); // 确保列表是干净的
            }
            return list;
        }

        private event Action OnTagIsDirty;

        private void TagIsDirty(GameplayTagSet tags)
        {
            Profiler.BeginSample($"{nameof(GameplayTagAggregator)}::TagIsDirty(GameplayTagSet)");
            if (!tags.Empty) OnTagIsDirty?.Invoke();
            Profiler.EndSample();
        }

        private void TagIsDirty(GameplayTag tag)
        {
            Profiler.BeginSample($"{nameof(GameplayTagAggregator)}::TagIsDirty(GameplayTag)");
            OnTagIsDirty?.Invoke();
            Profiler.EndSample();
        }

        /// <summary>
        /// 初始化标签聚合器，设置基础固定标签
        /// </summary>
        /// <param name="tags">初始固定标签数组，这些标签将作为基础标签持久存在</param>
        /// <example>
        /// // 为角色设置基础标签
        /// var baseTags = new GameplayTag[] { 
        ///     new GameplayTag("Character.Alive"), 
        ///     new GameplayTag("Combat.CanAttack") 
        /// };
        /// aggregator.Init(baseTags);
        /// </example>
        public void Init(GameplayTag[] tags)
        {
            _fixedTags.Clear();
            _fixedTags.AddRange(tags);
        }

        public void OnEnable()
        {
            Profiler.BeginSample($"[GC Mark] {nameof(GameplayTagAggregator)}::OnEnable()");
            // 有 GC, 无法避免
            OnTagIsDirty += _owner.GameplayEffectContainer.RefreshGameplayEffectState;
            Profiler.EndSample();
        }

        public void OnDisable()
        {
            OnTagIsDirty -= _owner.GameplayEffectContainer.RefreshGameplayEffectState;
        }


        private static bool IsTagInList(GameplayTag tag, List<GameplayTag> tags)
        {
            foreach (var t in tags)
            {
                if (t == tag)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryAddFixedTag(GameplayTag tag)
        {
            var dirty = !IsTagInList(tag, _fixedTags);
            if (dirty) _fixedTags.Add(tag);
            var dynamicRemovedTagsRemoved = _dynamicRemovedTags.Remove(tag);
            dirty = dirty || dynamicRemovedTagsRemoved;
            var dynamicAddedTagsRemoved = _dynamicAddedTags.Remove(tag);
            dirty = dirty || dynamicAddedTagsRemoved;
            return dirty;
        }

        /// <summary>
        /// 添加固定标签到聚合器中
        /// </summary>
        /// <param name="tag">要添加的标签</param>
        /// <remarks>
        /// 固定标签是持久性的，不会因为GameplayEffect或Ability的移除而消失。
        /// 如果标签已存在，此操作不会重复添加。
        /// </remarks>
        /// <example>
        /// // 给角色添加永久标签
        /// aggregator.AddFixedTag(new GameplayTag("Character.Immortal"));
        /// </example>
        public void AddFixedTag(GameplayTag tag)
        {
            var dirty = TryAddFixedTag(tag);
            if (dirty) TagIsDirty(tag);
        }

        /// <summary>
        /// 批量添加固定标签到聚合器中
        /// </summary>
        /// <param name="tagSet">要添加的标签集合</param>
        /// <remarks>
        /// 这是AddFixedTag(GameplayTag)的批量操作版本，用于一次性添加多个固定标签。
        /// 空的标签集合会被忽略。
        /// </remarks>
        /// <example>
        /// var tags = new GameplayTagSet(new GameplayTag[] {
        ///     new GameplayTag("Status.Buffed"),
        ///     new GameplayTag("Status.Protected")
        /// });
        /// aggregator.AddFixedTag(tags);
        /// </example>
        public void AddFixedTag(GameplayTagSet tagSet)
        {
            if (tagSet.Empty) return;
            var dirty = false;
            foreach (var tag in tagSet.Tags) dirty = dirty || TryAddFixedTag(tag);

            if (dirty) TagIsDirty(tagSet);
        }

        private bool TryRemoveFixedTag(GameplayTag tag)
        {
            var dirty = _fixedTags.Remove(tag);
            var dynamicAddedTagsRemoved = _dynamicAddedTags.Remove(tag);
            dirty = dirty || dynamicAddedTagsRemoved;
            var dynamicRemovedTagsRemoved = _dynamicRemovedTags.Remove(tag);
            dirty = dirty || dynamicRemovedTagsRemoved;
            return dirty;
        }

        /// <summary>
        /// 从聚合器中移除固定标签
        /// </summary>
        /// <param name="tag">要移除的标签</param>
        /// <remarks>
        /// 移除固定标签将永久删除该标签，同时清理所有相关的动态标签记录。
        /// 如果标签不存在，此操作不会产生任何效果。
        /// </remarks>
        /// <example>
        /// // 移除角色的某个固定标签
        /// aggregator.RemoveFixedTag(new GameplayTag("Character.Immortal"));
        /// </example>
        public void RemoveFixedTag(GameplayTag tag)
        {
            var dirty = TryRemoveFixedTag(tag);
            if (dirty) TagIsDirty(tag);
        }

        /// <summary>
        /// 批量从聚合器中移除固定标签
        /// </summary>
        /// <param name="tagSet">要移除的标签集合</param>
        /// <remarks>
        /// 这是RemoveFixedTag(GameplayTag)的批量操作版本。
        /// 空的标签集合会被忽略。
        /// </remarks>
        /// <example>
        /// var tagsToRemove = new GameplayTagSet(new GameplayTag[] {
        ///     new GameplayTag("Status.Buffed"),
        ///     new GameplayTag("Status.Protected")
        /// });
        /// aggregator.RemoveFixedTag(tagsToRemove);
        /// </example>
        public void RemoveFixedTag(GameplayTagSet tagSet)
        {
            if (tagSet.Empty) return;
            var dirty = false;
            foreach (var tag in tagSet.Tags) dirty = dirty || TryRemoveFixedTag(tag);

            if (dirty) TagIsDirty(tagSet);
        }

        private bool TryAddDynamicAddedTag<T>(T source, GameplayTag tag)
        {
            if (!IsValidTagSource(source))
            {
                return false;
            }

            var dirty = _dynamicRemovedTags.Remove(tag);
            foreach (var t in _fixedTags)
            {
                if (t == tag)
                {
                    return dirty;
                }
            }

            if (_dynamicAddedTags.TryGetValue(tag, out var addedTag))
            {
                foreach (object o in addedTag)
                {
                    if (source.Equals(o))
                    {
                        return false;
                    }
                }

                addedTag.Add(source);
            }
            else
            {  
                var list = GetPooledList();
                list.Add(source);
                _dynamicAddedTags.Add(tag, list);
            }

            return true;
        }

        private bool TryAddDynamicRemovedTag<T>(T source, GameplayTag tag)
        {
            if (!IsValidTagSource(source)) return false;
            var dirty = false;
            if (_dynamicAddedTags.TryGetValue(tag, out var addedTag))
            {
                // 安全地移除并回收列表
                _dynamicAddedTags.Remove(tag);
                addedTag.Clear();
                _pool.Return(addedTag);
                dirty = true;
            }

            if (!IsTagInList(tag, _fixedTags)) return dirty;

            if (_dynamicRemovedTags.TryGetValue(tag, out var removedTag))
                removedTag.Add(source);
            else
            {
                var list = GetPooledList();
                list.Add(source);
                _dynamicRemovedTags.Add(tag, list);
            }

            return true;
        }

        private bool TryRemoveDynamicTag<T>(ref Dictionary<GameplayTag, List<object>> dynamicTag, T source,
            GameplayTag tag)
        {
            var dirty = false;
            Profiler.BeginSample("TryRemoveDynamicTag");

            if (IsValidTagSource(source))
            {
                Profiler.BeginSample("[GC Mark]TryGetValue");
                var hasValue = dynamicTag.TryGetValue(tag, out var tagList);
                Profiler.EndSample();
                if (hasValue)
                {
                    Profiler.BeginSample("remove source from tag list");
                    tagList.Remove(source);
                    Profiler.EndSample();

                    dirty = tagList.Count == 0;
                    if (dirty)
                    {
                        _pool.Return(tagList);

                        Profiler.BeginSample("[GC Mark]remove dynamic tag");
                        dynamicTag.Remove(tag); // 有 GC
                        Profiler.EndSample();
                    }
                }
            }

            Profiler.EndSample();
            return dirty;
        }

        private bool TryRemoveDynamicAddedTag<T>(T source, GameplayTag tag)
        {
            return TryRemoveDynamicTag(ref _dynamicAddedTags, source, tag);
        }

        private bool TryRemoveDynamicRemovedTag<T>(T source, GameplayTag tag)
        {
            return TryRemoveDynamicTag(ref _dynamicRemovedTags, source, tag);
        }

        /// <summary>
        /// 应用GameplayEffect的动态标签到聚合器中
        /// </summary>
        /// <param name="source">作为标签来源的GameplayEffect实例</param>
        /// <remarks>
        /// 当GameplayEffect被应用时调用，将效果所授予的标签加入动态标签列表。
        /// 这些标签在效果持续期间会一直存在，效果结束时会自动移除。
        /// </remarks>
        /// <example>
        /// // 当一个增益效果被应用时
        /// var buffEffect = new GameplayEffectSpec(buffGameplayEffect);
        /// aggregator.ApplyGameplayEffectDynamicTag(buffEffect);
        /// // 现在聚合器包含了该效果所授予的所有标签
        /// </example>
        public void ApplyGameplayEffectDynamicTag(GameplayEffectSpec source)
        {
            var tagIsDirty = false;
            var grantedTagSet = source.GameplayEffect.TagContainer.GrantedTags;
            foreach (var tag in grantedTagSet.Tags)
            {
                var dirty = TryAddDynamicAddedTag(source, tag);
                tagIsDirty = tagIsDirty || dirty;
            }

            if (tagIsDirty) TagIsDirty(grantedTagSet);
        }

        /// <summary>
        /// 应用技能的动态标签到聚合器中
        /// </summary>
        /// <param name="source">作为标签来源的技能实例</param>
        /// <remarks>
        /// 当技能被激活时调用，将技能的激活所有标签加入动态标签列表。
        /// 这些标签在技能激活期间会一直存在，技能结束时会自动移除。
        /// </remarks>
        /// <example>
        /// // 当攻击技能被激活时
        /// var attackAbility = GetAbilitySpec("Attack");
        /// aggregator.ApplyGameplayAbilityDynamicTag(attackAbility);
        /// // 现在聚合器包含了"Combat.Attacking"标签
        /// </example>
        public void ApplyGameplayAbilityDynamicTag(AbilitySpec source)
        {
            var tagIsDirty = false;
            var activationOwnedTag = source.Ability.Tag.ActivationOwnedTag;
            foreach (var tag in activationOwnedTag.Tags)
            {
                var dirty = TryAddDynamicAddedTag(source, tag);
                tagIsDirty = tagIsDirty || dirty;
            }

            if (tagIsDirty) TagIsDirty(activationOwnedTag);
        }

        /// <summary>
        /// 恢复（移除）指定源对象的动态标签
        /// </summary>
        /// <typeparam name="T">源对象类型（通常是GameplayEffectSpec或AbilitySpec）</typeparam>
        /// <param name="source">标签的来源对象</param>
        /// <param name="tagSet">要移除的标签集合</param>
        /// <remarks>
        /// 用于清理特定源对象所添加的动态标签。
        /// 通常在GameplayEffect结束或Ability取消时调用。
        /// </remarks>
        /// <example>
        /// // 当增益效果结束时
        /// var buffTags = buffEffect.GameplayEffect.TagContainer.GrantedTags;
        /// aggregator.RestoreDynamicTags(buffEffect, buffTags);
        /// </example>
        public void RestoreDynamicTags<T>(T source, GameplayTagSet tagSet)
        {
            var tagIsDirty = false;
            foreach (var tag in tagSet.Tags)
            {
                var dirty = TryRemoveDynamicAddedTag(source, tag);
                tagIsDirty = tagIsDirty || dirty;
            }

            if (tagIsDirty) TagIsDirty(tagSet);
        }

        /// <summary>
        /// 恢复（移除）GameplayEffect的所有动态标签
        /// </summary>
        /// <param name="effectSpec">要恢复标签的效果实例</param>
        /// <remarks>
        /// 这是RestoreDynamicTags的便捷方法，自动获取效果的所有授予标签并移除。
        /// 通常在GameplayEffect过期或被手动移除时调用。
        /// </remarks>
        /// <example>
        /// // 当一个效果过期时
        /// aggregator.RestoreGameplayEffectDynamicTags(expiredEffect);
        /// </example>
        public void RestoreGameplayEffectDynamicTags(GameplayEffectSpec effectSpec)
        {
            RestoreDynamicTags(effectSpec, effectSpec.GameplayEffect.TagContainer.GrantedTags);
        }

        /// <summary>
        /// 恢复（移除）技能的所有动态标签
        /// </summary>
        /// <param name="abilitySpec">要恢复标签的技能实例</param>
        /// <remarks>
        /// 这是RestoreDynamicTags的便捷方法，自动获取技能的激活所有标签并移除。
        /// 通常在技能结束或被取消时调用。
        /// </remarks>
        /// <example>
        /// // 当攻击技能结束时
        /// aggregator.RestoreGameplayAbilityDynamicTags(attackAbility);
        /// </example>
        public void RestoreGameplayAbilityDynamicTags(AbilitySpec abilitySpec)
        {
            RestoreDynamicTags(abilitySpec, abilitySpec.Ability.Tag.ActivationOwnedTag);
        }

        /// <summary>
        /// 检查聚合器是否包含指定标签（包括父标签）
        /// </summary>
        /// <param name="tag">要检查的标签</param>
        /// <returns>如果包含该标签或其父标签则返回true</returns>
        /// <remarks>
        /// 检查逻辑：(固定标签 + 动态添加标签) - 动态移除标签
        /// 支持分层标签检查，如果查找"Combat.Damage"，则"Combat.Damage.Fire"也会匹配。
        /// 为了性能优化，避免GC，使用foreach而不LINQ。
        /// </remarks>
        /// <example>
        /// // 检查角色是否具有攻击能力
        /// bool canAttack = aggregator.HasTag(new GameplayTag("Combat.CanAttack"));
        /// 
        /// // 检查是否有任何伤害标签
        /// bool hasDamage = aggregator.HasTag(new GameplayTag("Combat.Damage"));
        /// // 如果有"Combat.Damage.Fire"或"Combat.Damage.Ice"都会返回true
        /// </example>
        public bool HasTag(GameplayTag tag)
        {
            // LINQ表达式存在GC，且HasTag调用频率很高，所以这里全都使用foreach
            var fixedTagsContainsTag = false;
            foreach (var t in _fixedTags)
            {
                if (t.HasTag(tag))
                {
                    fixedTagsContainsTag = true;
                    break;
                }
            }

            var dynamicAddedTagsContainsTag = false;
            foreach (var t in _dynamicAddedTags)
            {
                if (t.Key.HasTag(tag))
                {
                    dynamicAddedTagsContainsTag = true;
                    break;
                }
            }

            var dynamicRemovedTagsContainsTag = false;
            foreach (var t in _dynamicRemovedTags)
            {
                if (t.Key.HasTag(tag))
                {
                    dynamicRemovedTagsContainsTag = true;
                    break;
                }
            }

            return (fixedTagsContainsTag || dynamicAddedTagsContainsTag) && !dynamicRemovedTagsContainsTag;
        }

        /// <summary>
        /// 检查聚合器是否包含指定标签集合中的所有标签
        /// </summary>
        /// <param name="other">要检查的标签集合</param>
        /// <returns>如果包含所有指定标签则返回true，空集合返回true</returns>
        /// <remarks>
        /// 必须同时具备所有指定的标签才返回true。
        /// 空标签集合被视为“没有要求”，因此返回true。
        /// </remarks>
        /// <example>
        /// var requiredTags = new GameplayTagSet(new GameplayTag[] {
        ///     new GameplayTag("Combat.CanAttack"),
        ///     new GameplayTag("Character.Alive")
        /// });
        /// 
        /// // 检查是否同时具备攻击能力和存活状态
        /// bool canPerformAttack = aggregator.HasAllTags(requiredTags);
        /// </example>
        public bool HasAllTags(GameplayTagSet other)
        {
            if (other.Empty) return true;
            foreach (var tag in other.Tags)
                if (!HasTag(tag))
                    return false;

            return true;
        }

        /// <summary>
        /// 检查聚合器是否包含所有指定的标签（可变参数版本）
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果包含所有指定标签则返回true，空数组返回true</returns>
        /// <example>
        /// // 检查是否同时具备多个标签
        /// bool hasAll = aggregator.HasAllTags(
        ///     new GameplayTag("Combat.CanAttack"),
        ///     new GameplayTag("Character.Alive"),
        ///     new GameplayTag("Status.Ready")
        /// );
        /// </example>
        public bool HasAllTags(params GameplayTag[] tags)
        {
            foreach (var tag in tags)
                if (!HasTag(tag))
                    return false;

            return true;
        }

        /// <summary>
        /// 检查聚合器是否包含指定标签集合中的任意一个标签
        /// </summary>
        /// <param name="other">要检查的标签集合</param>
        /// <returns>如果包含任意一个指定标签则返回true，空集合返回false</returns>
        /// <remarks>
        /// 只要具备其中一个标签就返回true。
        /// 空标签集合被视为“无任何要求”，返回false。
        /// </remarks>
        /// <example>
        /// var damageTags = new GameplayTagSet(new GameplayTag[] {
        ///     new GameplayTag("Combat.Damage.Fire"),
        ///     new GameplayTag("Combat.Damage.Ice"),
        ///     new GameplayTag("Combat.Damage.Lightning")
        /// });
        /// 
        /// // 检查是否具备任意一种伤害类型
        /// bool hasDamageType = aggregator.HasAnyTags(damageTags);
        /// </example>
        public bool HasAnyTags(GameplayTagSet other)
        {
            if (other.Empty) return false;
            foreach (var tag in other.Tags)
            {
                if (HasTag(tag)) return true;
            }

            return false;
            //return !other.Empty && other.Tags.Any(HasTag);
        }

        /// <summary>
        /// 检查聚合器是否包含任意一个指定的标签（可变参数版本）
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果包含任意一个指定标签则返回true，空数组返回false</returns>
        /// <example>
        /// // 检查是否具备任意一种状态
        /// bool hasAnyStatus = aggregator.HasAnyTags(
        ///     new GameplayTag("Status.Stunned"),
        ///     new GameplayTag("Status.Frozen"),
        ///     new GameplayTag("Status.Sleeping")
        /// );
        /// </example>
        public bool HasAnyTags(params GameplayTag[] tags)
        {
            bool hasAny = false;
            foreach (var tag in tags)
                if (HasTag(tag))
                {
                    hasAny = true;
                    break;
                }

            return hasAny;
            //return tags.Any(HasTag);
        }

        /// <summary>
        /// 检查聚合器是否不包含指定标签集合中的任何标签
        /// </summary>
        /// <param name="other">要检查的标签集合</param>
        /// <returns>如果不包含任何指定标签则返回true，空集合返回true</returns>
        /// <remarks>
        /// 与 HasAnyTags 相反，用于检查“禁止标签”。
        /// 只有当所有指定标签都不存在时才返回true。
        /// 空标签集合被视为“无禁止要求”，返回true。
        /// </remarks>
        /// <example>
        /// var disabledTags = new GameplayTagSet(new GameplayTag[] {
        ///     new GameplayTag("Status.Stunned"),
        ///     new GameplayTag("Status.Silenced")
        /// });
        /// 
        /// // 检查是否不在任何禁用状态下
        /// bool canAct = aggregator.HasNoneTags(disabledTags);
        /// </example>
        public bool HasNoneTags(GameplayTagSet other)
        {
            if (other.Empty) return true;
            foreach (var tag in other.Tags)
            {
                if (HasTag(tag)) return false;
            }

            return true;
            //return other.Empty || !other.Tags.Any(HasTag);
        }

        /// <summary>
        /// 检查聚合器是否不包含任何指定的标签（可变参数版本）
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果不包含任何指定标签则返回true，空数组返回true</returns>
        /// <example>
        /// // 检查是否没有任何负面状态
        /// bool isHealthy = aggregator.HasNoneTags(
        ///     new GameplayTag("Status.Poisoned"),
        ///     new GameplayTag("Status.Diseased"),
        ///     new GameplayTag("Status.Cursed")
        /// );
        /// </example>
        public bool HasNoneTags(params GameplayTag[] tags)
        {
            if (tags.Length == 0) return true;
            foreach (var tag in tags)
            {
                if (HasTag(tag)) return false;
            }

            return true;
            //return !tags.Any(HasTag);
        }

#if UNITY_EDITOR
        public List<GameplayTag> FixedTags => _fixedTags;
        public Dictionary<GameplayTag, List<object>> DynamicAddedTags => _dynamicAddedTags;
#endif
    }
}