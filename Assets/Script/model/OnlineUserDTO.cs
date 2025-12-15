using System;
using System.Collections.Generic;

[Serializable]
public class OnlineUserDTO
{
    public long userId;
    public string username;
    public string avatarId;
    public int level;
    public bool inMatch;
    public long? roomId;
}

[Serializable]
public class RoomInviteDTO
{
    public long inviteId;
    public long roomId;
    public long fromUserId;
    public string fromUsername;
    public long toUserId;
    public string message;
    public long timestamp;
    public string status; // PENDING, ACCEPTED, DECLINED
}