using System;

[Serializable]
public class WorldBossDTO
{
        public int id;
    public int petId;
    public String bossName;
    public int bossLevel;
    public int bossHp;
    public int bossAttack;
    public int bossMana;
    public string elementType;
    public string startTime;
    public string endTime;
    public string status; // UPCOMING, ACTIVE, ENDED
    public int remainingAttempts;
    public int maxAttempts;
    public int currentDamage;
    public int userRank;
}