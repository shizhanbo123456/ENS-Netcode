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
        RoomId = 10000;
        Instance = this;
    }

    internal bool CreateRoom(EnsConnection conn,out int code)
    {
        if (conn.room != null)
        {
            code = 0;
            return false;
        }
        rooms.Add(RoomId,new EnsRoom(RoomId));
        rooms[RoomId].Join(conn);
        RoomId += 1;
        code= conn.room.RoomId;
        if (PrintRoomData) Debug.Log(ToString());
        return true;
    }
    internal bool JoinRoom(EnsConnection conn, int id,out int code)
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
    internal bool ExitRoom(EnsConnection conn,out int id)
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
    public void ShutDown()
    {
        foreach (var i in rooms.Values.ToList()) i.ShutDown();
        rooms.Clear();
        Instance = null;
        rooms = null;
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
