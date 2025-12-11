
using System;

[Serializable]
public class PetUserDTO
{
    public int id;
    public int userId;
    public int petId;
    public int skillCardId;
    public String name;
    public String des;
    public String elementType;
    public String elementOther;
    public int level;
     public int maxLevel;
    public int hp;
    public int attack;
    public int mana;
    public Double weaknessValue;
    public int manaSkillCard;
    public CardData cardDTO;
}
