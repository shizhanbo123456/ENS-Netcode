//同一个消息的两个方向共用同一个字节值：cs=客户端→服务器，sc=服务器→客户端
public static class Header
{
    public const byte Acs = 65;
    public const byte Asc = 65;

    public const byte Csc = 67;

    public const byte Dcs = 68;
    public const byte Dsc = 68;

    public const byte Esc = 69;

    public const byte Fcs = 70;
    public const byte Fsc = 70;

    public const byte Hcs = 72;
    public const byte Hsc = 72;

    public const byte Mcs = 77;
    public const byte Msc = 77;

    public const byte Ncs = 78;
    public const byte Nsc = 78;

    public const byte Qcs = 81;
    public const byte Qsc = 81;

    public const byte Rsc = 82;

    public const byte fcs = 102;
    public const byte fsc = 102;
}
