
using System;
using System.Collections.Generic;

[Serializable]
public class RoomDTO
{
    public int id;
    public int energy;
    public int energyFull;
    public int count;
    public int requestPass;
    public int requestAttack;
    public string name;
    public int lever;
    public int petId;
    public int enemyPetId;
    public string nameEnemyPetId;
    public string elementType;
    public List<CardData> cards;
}
