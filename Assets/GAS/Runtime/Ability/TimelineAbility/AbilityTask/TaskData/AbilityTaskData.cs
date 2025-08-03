using System;
using GAS.General;
using UnityEngine;

namespace GAS.Runtime
{
    [Serializable]
    public abstract class AbilityTaskData
    {
        /// <summary>
        /// 任务的JSON序列化数据，包含类型信息和实例数据
        /// </summary>
        public JsonData TaskData;
        
        public virtual AbilityTaskBase Create(AbilitySpec abilitySpec)
        {
            var task = Load();
            task.Init(abilitySpec);
            return task;
        }
        
        public void Save(AbilityTaskBase task)
        {
            var jsonData = JsonUtility.ToJson(task);
            var dataType = task.GetType().FullName;
            TaskData = new JsonData
            {
                Type = dataType,
                Data = jsonData
            };
        }

        public abstract AbilityTaskBase Load();
    }
}