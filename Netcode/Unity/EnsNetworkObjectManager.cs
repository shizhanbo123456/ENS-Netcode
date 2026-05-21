using System.Collections.Generic;
using System.Reflection;
using Utils;

public static class EnsNetworkObjectManager
{
    public static HashSet<short> ManualAssignedId = new HashSet<short>();

    //<0为玩家设置的初始化场景时制造的物体
    private static short sceneobjid = -1;
    internal static short GetAutoAllocatedId()
    {
        return sceneobjid--;
    }




    private static readonly IComparer<int> DescendingComparer = Comparer<int>.Create((x, y) =>
    {
        // 核心逻辑：反转默认比较结果（x.CompareTo(y)是升序，y.CompareTo(x)是降序）
        return y.CompareTo(x);
    });
    private static SortedDictionary<int, Dictionary<int, EnsBehaviour>> prioritizedUpdate =
        new SortedDictionary<int, Dictionary<int, EnsBehaviour>>(DescendingComparer);
    private static SortedDictionary<int, Dictionary<int, EnsBehaviour>> prioritizedFixedUpdate =
        new SortedDictionary<int, Dictionary<int, EnsBehaviour>>(DescendingComparer);
    // 原始ID映射表（用于快速查找物体）
    private static Dictionary<int, EnsBehaviour> objectMap =
        new Dictionary<int, EnsBehaviour>();
    private static Dictionary<int, int> priorityMap =
        new Dictionary<int, int>();
    private static Dictionary<int, int> fixedPriorityMap =
        new Dictionary<int, int>();

    private static List<int>m_toRemove=new List<int>();

    internal static IEnumerable<int> GetPriority()
    {
        return prioritizedUpdate.Keys;
    }
    internal static IEnumerable<int> GetFixedPriority()
    {
        return prioritizedFixedUpdate.Keys;
    }
    /// <summary>
    /// 按优先级执行所有物体的Update
    /// </summary>
    internal static void Update(int priority)
    {
        if (prioritizedUpdate.TryGetValue(priority, out var group))
        {
            foreach (var id in group.Keys)
            {
                if (group[id] == null)
                {
                    m_toRemove.Add(id);
                    continue;
                }
                if (group[id].nomEnabled)
                {
                    group[id].ManagedUpdate();
                }
            }
            if(m_toRemove.Count > 0)
            {
                foreach (var i in m_toRemove) group.Remove(i);
            }
        }
    }
    /// <summary>
    /// 按优先级执行所有物体的FixedUpdate
    /// </summary>
    internal static void FixedUpdate(int priority)
    {
        if (prioritizedFixedUpdate.TryGetValue(priority, out var group))
        {
            foreach (var id in group.Keys)
            {
                if (group[id] == null)
                {
                    m_toRemove.Add(id);
                    continue;
                }
                if (group[id].nomEnabled)
                {
                    group[id].FixedManagedUpdate();
                }
            }
            if (m_toRemove.Count > 0)
            {
                foreach (var i in m_toRemove) group.Remove(i);
            }
        }
    }


    internal static void AddObject(EnsBehaviour behaviour)
    {
        //Debug.Log("添加id为" + behaviour.ObjectId + "的物体:"+behaviour.gameObject.name);
        if (behaviour == null)
        {
            Debug.LogWarning("尝试添加空的NOMBehaviour");
            return;
        }
        int objectId = behaviour.ObjectId;
        if (objectId == 0) Debug.Log(behaviour.name+"id未分配");
        if (objectMap.ContainsKey(objectId) && objectMap[objectId]!=null)
        {
            Debug.LogWarning($"id为{objectId}的物体{behaviour.gameObject.name}-{behaviour.GetType().ToString()}已经被添加");
            return;
        }

        objectMap[objectId] = behaviour;
        if (GetManagedUpdatePriority(behaviour, out int priority))
        {
            priorityMap[objectId] = priority;
            if (!prioritizedUpdate.ContainsKey(priority))
                prioritizedUpdate[priority] = new Dictionary<int, EnsBehaviour>();
            prioritizedUpdate[priority].Add(behaviour.ObjectId, behaviour);
        }
        if (GetFixedManagedUpdatePriority(behaviour, out priority))
        {
            fixedPriorityMap[objectId] = priority;
            if (!prioritizedFixedUpdate.ContainsKey(priority))
                prioritizedFixedUpdate[priority] = new Dictionary<int, EnsBehaviour>();
            prioritizedFixedUpdate[priority].Add(behaviour.ObjectId, behaviour);
        }
    }
    internal static bool HasObject(int id)
    {
        return objectMap.ContainsKey(id);
    }
    internal static EnsBehaviour GetObject(int id)
    {
        if (objectMap.TryGetValue(id, out var data))
            return data;
        return null;
    }
    internal static void RemoveObject(int id)
    {
        if (objectMap.ContainsKey(id))
        {
            objectMap.Remove(id);
            if (priorityMap.TryGetValue(id, out int updatePriority))
            {
                if (prioritizedUpdate.TryGetValue(updatePriority, out var updateGroup))
                {
                    updateGroup.Remove(id);
                    if (updateGroup.Count == 0)
                        prioritizedUpdate.Remove(updatePriority);
                }
                priorityMap.Remove(id);
            }
            if (fixedPriorityMap.TryGetValue(id, out int fixedPriority))
            {
                if (prioritizedFixedUpdate.TryGetValue(fixedPriority, out var fixedGroup))
                {
                    fixedGroup.Remove(id);
                    if (fixedGroup.Count == 0)
                        prioritizedFixedUpdate.Remove(fixedPriority);
                }
                fixedPriorityMap.Remove(id);
            }
        }
        else
        {
            if (EnsInstance.DevelopmentDebug) Debug.LogWarning($"移除时未找到id为{id}的物体");
            return;
        }
    }
    private static bool GetManagedUpdatePriority(EnsBehaviour behaviour, out int priority)
    {
        priority = behaviour.UpdatePriority;
        return priority>=0;
    }
    private static bool GetFixedManagedUpdatePriority(EnsBehaviour behaviour, out int priority)
    {
        priority = behaviour.UpdatePriority;
        return priority>=0;
    }
}