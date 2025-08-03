using System;
using System.Collections.Generic;
using System.Linq;

namespace GAS.Runtime
{
    /// <summary>
    /// 技能容器，管理一个组件上的所有技能
    /// </summary>
    /// <remarks>
    /// 这个容器负责：
    /// - 技能的授予和移除
    /// - 技能的激活、结束和取消
    /// - 技能的生命周期管理
    /// - 技能间的互相作用和取消逻辑
    /// - 技能的Tick更新
    /// </remarks>
    public class AbilityContainer
    {
        /// <summary>
        /// 拥有此技能容器的AbilitySystemComponent组件
        /// 用于提供技能的上下文和执行环境
        /// </summary>
        private readonly AbilitySystemComponent _owner;
        
        /// <summary>
        /// 技能实例字典，以技能名称作为键存储所有已授予的技能
        /// 提供快速的技能查找和管理功能
        /// </summary>
        private readonly Dictionary<string, AbilitySpec> _abilities = new Dictionary<string, AbilitySpec>();
        
        /// <summary>
        /// 缓存的技能实例列表，用于避免频繁的字典遍历操作
        /// 当前未使用，保留供未来优化
        /// </summary>
        private readonly List<AbilitySpec> _cachedAbilities = new List<AbilitySpec>();

        public AbilityContainer(AbilitySystemComponent owner)
        {
            _owner = owner;
        }

        public void Tick()
        {
            var keys = _abilities.Keys.ToArray(); // 只复制key
            foreach (var key in keys)
            {
                if (_abilities.TryGetValue(key, out var spec)) // 防止key被删除
                {
                    spec.Tick();
                }
            }
        }

        /// <summary>
        /// 授予组件一个新技能
        /// </summary>
        /// <param name="ability">要授予的技能实例</param>
        /// <remarks>
        /// 如果同名技能已存在，此操作会被忽略。
        /// 授予后，技能将可以被激活和使用。
        /// </remarks>
        /// <example>
        /// // 授予攻击技能
        /// var attackAbility = new SwordAttackAbility(attackAbilityAsset);
        /// container.GrantAbility(attackAbility);
        /// </example>
        public void GrantAbility(AbstractAbility ability)
        {
            if (_abilities.ContainsKey(ability.Name)) return;
            var abilitySpec = ability.CreateSpec(_owner);
            _abilities.Add(ability.Name, abilitySpec);
        }

        public void RemoveAbility(AbstractAbility ability)
        {
            RemoveAbility(ability.Name);
        }

        /// <summary>
        /// 从容器中移除指定名称的技能
        /// </summary>
        /// <param name="abilityName">要移除的技能名称</param>
        /// <remarks>
        /// 移除技能会：
        /// 1. 立即结束该技能的所有实例
        /// 2. 释放技能相关资源
        /// 3. 从容器中删除技能记录
        /// 如果技能不存在，此操作不会产生效果。
        /// </remarks>
        /// <example>
        /// // 移除攻击技能
        /// container.RemoveAbility("SwordAttack");
        /// </example>
        public void RemoveAbility(string abilityName)
        {
            if (!_abilities.ContainsKey(abilityName)) return;

            EndAbility(abilityName);
            _abilities[abilityName].Dispose();
            _abilities.Remove(abilityName);
        }

        /// <summary>
        /// 尝试激活指定名称的技能
        /// </summary>
        /// <param name="abilityName">要激活的技能名称</param>
        /// <param name="args">传递给技能的参数</param>
        /// <returns>如果激活成功返回true</returns>
        /// <remarks>
        /// 激活流程：
        /// 1. 检查技能是否存在
        /// 2. 检查激活条件（冷却、资源、标签等）
        /// 3. 激活技能
        /// 4. 执行技能间的取消逻辑（根据CancelAbilitiesWithTags）
        /// 
        /// 激活失败的常见原因：
        /// - 技能不存在
        /// - 技能正在冷却中
        /// - 不满足激活条件
        /// - 技能已激活且不允许多重激活
        /// </remarks>
        /// <example>
        /// // 攻击指定目标
        /// bool success = container.TryActivateAbility("SwordAttack", enemy);
        /// if (!success)
        /// {
        ///     Debug.Log("攻击技能激活失败");
        /// }
        /// </example>
        public bool TryActivateAbility(string abilityName, params object[] args)
        {
            if (!_abilities.ContainsKey(abilityName))
            {
                // 开发指南:
                // 如果你的Preset里配置了固有技能却没该技能(甚至_abilities里一个技能都没有)
                // 可能是你忘记调用ASC::Init(), 请检查AbilitySystemComponent的初始化
                // 通常我们使用ASC::InitWithPreset()来间接调用ASC::Init()执行初始化
                // 这个输出可以删掉, 某些情况下确实会尝试激活不存在的技能(失败了也无所谓), 但是对开发期间的调试有帮助
                Console.WriteLine(
                    $"you are trying to activate an ability that does not exist: " +
                    $"abilityName=\"{abilityName}\", GameObject=\"{_owner.Name}\", " +
                    $"Preset={(_owner.Preset?.GetType().Name ?? "null")}");
                return false;
            }

            if (!_abilities[abilityName].TryActivateAbility(args)) return false;

            // 发布技能激活事件
            if (_owner?.EventBus != null)
            {
                _owner.PublishAbilityEvent(abilityName, GameplayEvents.OnAbilityActivated);
            }

            var tags = _abilities[abilityName].Ability.Tag.CancelAbilitiesWithTags;
            foreach (var kv in _abilities)
            {
                var abilityTag = kv.Value.Ability.Tag;
                if (abilityTag.AssetTag.HasAnyTags(tags))
                {
                    _abilities[kv.Key].TryCancelAbility();
                }
            }

            return true;
        }

        /// <summary>
        /// 正常结束指定名称的技能
        /// </summary>
        /// <param name="abilityName">要结束的技能名称</param>
        /// <remarks>
        /// 正常结束会触发技能的结束逻辑，包括清理状态、触发结束事件等。
        /// 如果技能不存在或没有激活，此操作不会产生效果。
        /// </remarks>
        /// <example>
        /// // 手动结束技能
        /// container.EndAbility("ChannelingSpell");
        /// </example>
        public void EndAbility(string abilityName)
        {
            if (!_abilities.ContainsKey(abilityName)) return;
            _abilities[abilityName].TryEndAbility();
            
            // 发布技能结束事件
            if (_owner?.EventBus != null)
            {
                _owner.PublishAbilityEvent(abilityName, GameplayEvents.OnAbilityEnded);
            }
        }

        /// <summary>
        /// 取消指定名称的技能
        /// </summary>
        /// <param name="abilityName">要取消的技能名称</param>
        /// <remarks>
        /// 取消与结束不同，取消是强制中断，可能不会触发正常的结束逻辑。
        /// 常用于打断、眩眤等情况下强制停止技能。
        /// 如果技能不存在或没有激活，此操作不会产生效果。
        /// </remarks>
        /// <example>
        /// // 当角色被眩眤时取消正在释放的技能
        /// if (IsStunned)
        /// {
        ///     container.CancelAbility("ChannelingSpell");
        /// }
        /// </example>
        public void CancelAbility(string abilityName)
        {
            if (!_abilities.ContainsKey(abilityName)) return;
            _abilities[abilityName].TryCancelAbility();
            
            // 发布技能取消事件
            if (_owner?.EventBus != null)
            {
                _owner.PublishAbilityEvent(abilityName, GameplayEvents.OnAbilityCancelled);
            }
        }

        void CancelAbilitiesByTag(GameplayTagSet tags)
        {
            foreach (var kv in _abilities)
            {
                var abilityTag = kv.Value.Ability.Tag;
                if (abilityTag.AssetTag.HasAnyTags(tags))
                {
                    _abilities[kv.Key].TryCancelAbility();
                }
            }
        }

        public Dictionary<string, AbilitySpec> AbilitySpecs() => _abilities;

        public void CancelAllAbilities()
        {
            foreach (var kv in _abilities)
                _abilities[kv.Key].TryCancelAbility();
        }

        public bool HasAbility(string abilityName) => _abilities.ContainsKey(abilityName);

        public bool HasAbility(AbstractAbility ability) => HasAbility(ability.Name);
    }
}