using Ens.ProtoFormat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// NOMBehaviour提供了函数调用同步的功能
/// 调用CallFuncServerRpc即可实现同步调用<br></br>
/// 客户端主动调用的方法传入一个string类型的param，被调用时传入原来的string
/// 初始存在的物体需要enabled=true，销毁物体使用DestroyRpc或DestroyLocal
/// </summary>
public abstract class EnsBehaviour : MonoBehaviour
{
    protected internal int UpdatePriority => 0;
    // <0为玩家设置的初始化场景时制造的物体  >0为游戏过程中制造的物体的Id  =0为未分配的
    public short ObjectId=0;
    public bool nomEnabled = true;

    internal EnsBehaviourCollection collection;

    internal bool IdAutoAllocated=false;
    private bool startInvoked=false;

    protected void Start()
    {
        Regist();
        _Start();
    }
    internal void Regist()
    {
        if(startInvoked) return;
        startInvoked = true;
        if (ObjectId == 0)
        {
            ObjectId = EnsNetworkObjectManager.GetAutoAllocatedId();
            IdAutoAllocated = true;
        }
        else
        {
            if (!IdAutoAllocated)
            {
                short id = (short)(ObjectId % 2000+30000);
                if (EnsNetworkObjectManager.ManualAssignedId.Contains(id))
                {
                    if(EnsNetworkObjectManager.HasObject(id))
                        Debug.LogError("手动分配的id发生冲突");
                }
                else
                {
                    EnsNetworkObjectManager.ManualAssignedId.Add(id);
                }
            }
        }
        EnsNetworkObjectManager.AddObject(this);
    }
    protected virtual void _Start()
    {
        
    }
    /// <summary>
    /// 需要发送数据时，在此Update中使用，减少调用到传输的延迟<br></br>
    /// </summary>
    public virtual void ManagedUpdate()
    {

    }
    /// <summary>
    /// 需要发送数据时，在此FixedUpdate中使用，减少调用到传输的延迟<br></br>
    /// </summary>
    public virtual void FixedManagedUpdate()
    {

    }


    //使用泛型方法，用户编写代码时调用，实际编译时加入了生成的代码，不调用泛型方法
    public void CallFuncRpc(Action func, SendTo sendto, Delivery delivery)
    {
        LogUnknownFunc();
    }
    public void CallFuncRpc<T>(Action<T> func, SendTo sendto, Delivery delivery, T param1)
    {
        LogUnknownFunc();
    }
    public void CallFuncRpc<T1, T2>(Action<T1, T2> func, SendTo sendto, Delivery delivery, T1 param1, T2 param2)
    {
        LogUnknownFunc();
    }
    public void CallFuncRpc<T1, T2, T3>(Action<T1, T2, T3> func, SendTo sendto, Delivery delivery, T1 param1, T2 param2, T3 param3)
    {
        LogUnknownFunc();
    }
    public void CallFuncRpc<T1, T2, T3, T4>(Action<T1, T2, T3, T4> func, SendTo sendto, Delivery delivery, T1 param1, T2 param2, T3 param3, T4 param4)
    {
        LogUnknownFunc();
    }
    public void CallFuncRpc<T1, T2, T3, T4,T5>(Action<T1, T2, T3, T4,T5> func, SendTo sendto, Delivery delivery, T1 param1, T2 param2, T3 param3, T4 param4,T5 param5)
    {
        LogUnknownFunc();
    }
    public void CallFuncRpc<T1, T2, T3, T4, T5,T6>(Action<T1, T2, T3, T4, T5,T6> func, SendTo sendto, Delivery delivery, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5,T6 param6)
    {
        LogUnknownFunc();
    }
    public void CallFuncRpc<T1, T2, T3, T4, T5, T6,T7>(Action<T1, T2, T3, T4, T5, T6,T7> func, SendTo sendto, Delivery delivery, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6,T7 param7)
    {
        LogUnknownFunc();
    }
    private void LogUnknownFunc()
    {
        Debug.LogError("调用未注册的代码，请检查调用堆栈");
    }
    public void Send(Delivery delivery, SendTo sendto)
    {
        //EnsTemporaryBuffer 首位是函数标识，其后是参数字节
        var body = new MessageBody_F_CS
        {
            NOMObjectId = ObjectId,
            func = EnsTemporaryBuffer.bytes[0],
            param = (EnsTemporaryBuffer.bytes, new Segment(1, EnsTemporaryBuffer.length - 1)),
            target = sendto.Target,
        };
        EnsInstance.Corr.Client.Send(delivery, body, Pkt.csF);
    }
    /// <summary>
    /// 传入的s的片段仅包含[bytes param]部分，格式由func决定
    /// </summary>
    public virtual bool InvokeFunc(byte func, byte[] bytes, Segment s)
    {
        return false;
    }
}
