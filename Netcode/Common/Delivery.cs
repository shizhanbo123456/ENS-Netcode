using System;
using Utils;

public class DeliverySource
{
    //Unreliable id=0
    //Strive id=1-20000
    //Reliable id=20001-30000 resp=30001-40000
    //Orderwise id=40001-50000 resp=50001-60000
    public static readonly byte Unreliable = 0;

    private int striveSource;
    private int reliableSource;
    private int orderWiseSource;
    private DeliverySource() { }
    private static readonly ObjectPool<DeliverySource> pool=new ObjectPool<DeliverySource>(
        () => new DeliverySource());
    public static DeliverySource Get()
    {
        var s=pool.Get();
        s.striveSource = 1;
        s.reliableSource = 20001;
        s.orderWiseSource = 40001;
        return s;
    }
    public static void Return(DeliverySource d)=> pool.Return(d);
    private ushort Strive
    {
        get
        {
            striveSource++;
            if (striveSource > 20000) striveSource = 1;
            return (ushort)striveSource;
        }
    }
    private ushort Reliable
    {
        get
        {
            reliableSource++;
            if (reliableSource > 30000) reliableSource = 20001;
            return (ushort)reliableSource;
        }
    }
    private ushort Orderwise
    {
        get
        {
            orderWiseSource++;
            if (orderWiseSource > 50000) orderWiseSource = 40001;
            return (ushort)orderWiseSource;
        }
    }
    public ushort DeliveryToId(Delivery delivery)
    {
        return delivery switch
        {
            Delivery.Unreliable => Unreliable,
            Delivery.Strive=>Strive,
            Delivery.Reliable => Reliable,
            Delivery.OrderWise => Orderwise,
            _ => throw new Exception("Delivery随机数产生器检测到未知类型")
        };
    }
    public static Delivery IdToDelivery(ushort b)
    {
        if (b == 0) return Delivery.Unreliable;
        else if (b >= 1 && b <= 20000) return Delivery.Strive;
        else if (b >= 20001 && b <= 40000) return Delivery.Reliable;
        else return Delivery.OrderWise;
    }
    public static bool IsResponse(ushort b)
    {
        return (b >= 30001 && b <= 40000) || (b >= 50001 && b <= 60000);
    }
    public static ushort MessageToResponse(ushort b)
    {
        if (b >= 20001 && b <= 30000) return (ushort)(b + 10000);
        else if (b >= 40001 && b <= 50000) return (ushort)(b + 10000);
        else throw new Exception($"{b}无对应回复");
    }
    public static ushort ResponseToMessage(ushort b)
    {
        if (b >= 30001 && b <= 40000) return (ushort)(b - 10000);
        else if (b >= 50001 && b <= 60000) return (ushort)(b - 10000);
        else throw new Exception($"{b}无对应原消息");
    }
}
public enum Delivery
{
    Unreliable,
    Strive,
    Reliable,
    OrderWise
}