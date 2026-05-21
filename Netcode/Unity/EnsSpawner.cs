using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 调用CreateServerRpc制造物体
/// 此类已经继承了NOMBehaviour来同步制造函数
/// </summary>
public sealed class EnsSpawner : MonoBehaviour
{
    [Space]
    public List<EnsBehaviourCollection> Prefabs = new List<EnsBehaviourCollection>();
    public Vector3 DefaultSpawnPosition = new Vector3(0, -10000, 0);

    private static short prefabid = 10000;

    private void Awake()
    {
        EnsInstance.EnsSpawner=this;

        for (int i = 0; i < Prefabs.Count; i++)
        {
            Prefabs[i].CollectionId = prefabid;
            prefabid++;
        }
    }
    private short GetBehaviourCount(short id)
    {
        foreach(var i in Prefabs)
        {
            if(i.CollectionId == id)
            {
                return (short)i.Count;
            }
        }
        throw new System.Exception("[E]无对应的物体");
    }
    private class Writer : MessageWriter
    {
        internal static Writer instance = new();
        internal short totarget;
        internal bool RespawnOrCreate;
        internal short t_id;
        internal string t_param;
        internal short t_indexStart;

        public int GetLength()
        {
            return 2 + 1 + 2 + StringSerializer.GetLength(t_param) + 2;
        }
        public bool Write(SendBuffer b)
        {
            return ShortSerializer.Serialize(totarget, b.bytes, ref b.indexStart)
                && BoolSerializer.Serialize(RespawnOrCreate, b.bytes, ref b.indexStart)
                && ShortSerializer.Serialize(t_id, b.bytes, ref b.indexStart)
                && StringSerializer.Serialize(t_param, b.bytes, ref b.indexStart)
                && ShortSerializer.Serialize(t_indexStart, b.bytes, ref b.indexStart);
        }
        public MessageWriter Clone()
        {
            return new Writer() { totarget=totarget,RespawnOrCreate=RespawnOrCreate,t_id=t_id,t_param=t_param,t_indexStart=t_indexStart};
        }
        public void Dispose()
        {

        }
    }
    public void CreateServerRpc(SendTo sendto, short id,string param)
    {
        Writer.instance.totarget = sendto.Target;
        Writer.instance.RespawnOrCreate = false;
        Writer.instance.t_id = id;
        Writer.instance.t_param = param;
        Writer.instance.t_indexStart =GetBehaviourCount(id);
        EnsInstance.Corr.Client.Send(Header.f, Delivery.Reliable,Writer.instance);
    }
    public void RespawnCheckServerRpc(SendTo sendto, EnsBehaviourCollection collection, string param)
    {
        Writer.instance.totarget = sendto.Target;
        Writer.instance.RespawnOrCreate = true;
        Writer.instance.t_id = collection.CollectionId;
        Writer.instance.t_param = param;
        Writer.instance.t_indexStart = collection.Behaviors[0].ObjectId;
        EnsInstance.Corr.Client.Send(Header.f, Delivery.Reliable, Writer.instance);
    }

    public void CreateLocal(short id,string param,short idStart)
    {
        for (int i = 0; i < Prefabs.Count; i++)
        {
            if (Prefabs[i].CollectionId == id)
            {
                GameObject obj = Instantiate(Prefabs[i].gameObject, DefaultSpawnPosition,Quaternion.identity);
                var c = obj.GetComponent<EnsBehaviourCollection>();
                c.AllocateId(idStart);
                c.PreInit(param);

                return;
            }
        }
        Debug.LogError("[N]未找到目标物体");
    }
    public void RespawnCheckLocal(short id, string param, short idStart)
    {
        if (EnsNetworkObjectManager.HasObject(idStart)) return;
        for (int i = 0; i < Prefabs.Count; i++)
        {
            if (Prefabs[i].CollectionId == id)
            {
                GameObject obj = Instantiate(Prefabs[i].gameObject, DefaultSpawnPosition, Quaternion.identity);
                var c = obj.GetComponent<EnsBehaviourCollection>();
                c.AllocateId(idStart);
                c.PreRespawn(param);

                return;
            }
        }
        Debug.LogError("[N]未找到目标物体");
    }
}
