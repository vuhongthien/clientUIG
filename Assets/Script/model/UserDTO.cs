
using System;

[Serializable]
public class UserDTO
{
    public int id;
    public int petId;
    public int avtId;
    public int energy;
    public int energyFull;
    public int gold;
    public int ruby;
    public int requestAttack;
    public string name;
    public int lever;
    public int exp;
    public int expCurrent;
    public int wheel;
    public int starWhite;
    public int starBlue;
    public int starRed;
    public long secondsUntilNextRegen;
}
[System.Serializable]
public class EnergyInfoDTO
{
    public int currentEnergy;
    public int maxEnergy;
    public long secondsUntilNextRegen;
    public string lastUpdateTime;
}

[System.Serializable]
public class ConsumeEnergyResponse
{
    public bool success;
    public string message;
}