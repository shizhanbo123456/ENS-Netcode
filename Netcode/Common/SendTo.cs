using System;

public struct SendTo
{
    public static SendTo Everyone=>new SendTo(-1);
    public static SendTo ExcludeSender=>new SendTo(-2);
    public static SendTo RoomOwner=>new SendTo(-3);
    public static SendTo Server=>new SendTo(-4);
    public static SendTo To(short id) => new SendTo(id);

    private static readonly byte[] buffer = new byte[2];
    public static SendTo To(byte b1,byte b2)
    {
        buffer[0] = b1;
        buffer[1] = b2;
        int index = 0;
        var s=ShortSerializer.Deserialize(buffer, ref index,2);
        return new SendTo(s);
    }

    private short target;
    public short Target => target;
    private SendTo(short target)
    {
        this.target=target;
    }
    public static bool operator ==(SendTo a,SendTo b)
    {
        return a.target == b.target;
    }
    public static bool operator !=(SendTo a, SendTo b)
    {
        return a.target != b.target;
    }
    public override bool Equals(object obj)
    {
        return obj is SendTo to &&
               target == to.target;
    }
    public override int GetHashCode()
    {
        return target;
    }
}