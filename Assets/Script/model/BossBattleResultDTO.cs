using System;

[Serializable]
public class BossBattleResultDTO
{
    public long userId;
    public long bossScheduleId;
    public int damageDealt;
    public bool victory;
    public int turnCount;
}

[Serializable]
public class BossRankingDTO
{
    public long userId;
    public string userName;
    public int totalDamage;
    public int rank;
}

[Serializable]
public class BossRewardDTO
{
    public int gold;
    public int exp;
    public int starWhite;
    public int starBlue;
    public int starRed;
}