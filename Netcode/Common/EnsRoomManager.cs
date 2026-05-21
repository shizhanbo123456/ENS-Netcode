using System.Collections.Generic;
using System.Linq;
using Utils;

public class EnsRoomManager
{
    public const int roomIdStart = 10000;
    public static EnsRoomManager Instance;
    public SortedDictionary<int,EnsRoom> rooms = new SortedDictionary<int, EnsRoom>();
    private int RoomId;

    public static bool PrintRoomData=false;

    internal EnsRoomManager()
    {
        RoomId = roomIdStart;
        Instance = this;
    }

    internal virtual bool CreateRoom(EnsConnection conn,out int code)
    {
        if (conn.room != null)
        {
            code = 0;
            return false;
        }
        rooms.Add(RoomId,
            EnsServer.RoomFactory==null?
            new EnsRoom(RoomId):
            EnsServer.RoomFactory.Invoke(RoomId));
        rooms[RoomId].Join(conn);
        RoomId += 1;
        code= conn.room.RoomId;
        if (PrintRoomData) Debug.Log(ToString());
        return true;
    }
    internal virtual bool JoinRoom(EnsConnection conn, int id,out int code)
    {
        if (conn.room != null)
        {
            code = 1;
            return false;
        }
        if (!rooms.ContainsKey(id))
        {
            code = 0;
            return false;
        }
        var room = rooms[id];

        room.Join(conn);
        code = room.RoomId;
        if (PrintRoomData) Debug.Log(ToString());
        return true;
    }
    internal virtual bool ExitRoom(EnsConnection conn,out int id)
    {
        if (conn.room == null)
        {
            id= 0;
            return false;
        }
        conn.room.Exit(conn);
        id = 0;
        if (PrintRoomData) Debug.Log(ToString());
        return true;
    }
    internal virtual void RecvEvent(int type,string content)
    {

    }
    public virtual void ShutDown()
    {
        foreach (var i in rooms.Values.ToList()) i.ShutDown();
        rooms.Clear();
        Instance = null;
        rooms = null;
    }
    internal virtual void Update()
    {

    }
    protected static void TrigClientEvent(Delivery delivery, int header, string content)
    {
        if (EnsInstance.Corr != null && EnsInstance.Corr.Client != null)
        {
            Writer.instance.t_type = header;
            Writer.instance.t_content = content;
            EnsInstance.Corr.Client.Send(Header.M, delivery, Writer.instance);
        }
        else
        {
            Utils.Debug.LogError("当前状态不能发送消息");
        }
    }
    private class Writer : MessageWriter
    {
        internal static Writer instance = new();
        internal int t_type;
        internal string t_content;

        public int GetLength()
        {
            return sizeof(int) + StringSerializer.GetLength(t_content);
        }
        public bool Write(SendBuffer b)
        {
            return IntSerializer.Serialize(t_type, b.bytes, ref b.indexStart) &&
                StringSerializer.Serialize(t_content, b.bytes, ref b.indexStart);
        }
        public MessageWriter Clone()
        {
            return new Writer() { t_type = t_type, t_content = t_content };
        }
        public void Dispose()
        {

        }
    }
    public override string ToString()
    {
        string r = "房间信息：";
        foreach (var i in rooms.Values)
        {
            r += i.ToString() + " ";
        }
        return r;
    }
}
