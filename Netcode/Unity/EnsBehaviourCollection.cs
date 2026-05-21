using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 用于管理预制体中的行为脚本
/// </summary>
public class EnsBehaviourCollection : MonoBehaviour
{
    [HideInInspector]public short CollectionId;
    [Space]
    public List<EnsBehaviour> Behaviors = new List<EnsBehaviour>();
    public int Count
    {
        get
        {
            return Behaviors.Count;
        }
    }

    internal void AllocateId(short idstart)
    {
        for(int i=0;i<Behaviors.Count;i++)
        {
            Behaviors[i].collection=this;
            Behaviors[i].ObjectId=idstart;
            Behaviors[i].IdAutoAllocated = true;
            idstart++;
            if (idstart >= 30000) idstart -= 29900;
            //EnsNetworkObjectManager.AddObject(Behaviors[i]);//NOMAwake注册物体
        }
    }
    public void PreInit(string data)
    {
        foreach (var i in Behaviors) i.Regist();
        Init(data);
    }
    protected virtual void Init(string data)
    {
        if (EnsInstance.DevelopmentDebug) Debug.LogWarning("[N]未重写：初始化信息" + data);
    }
    public void PreRespawn(string data)
    {
        foreach (var i in Behaviors) i.Regist();
        Respawn(data);
    }
    protected virtual void Respawn(string data)
    {
        Init(data);
    }
}
