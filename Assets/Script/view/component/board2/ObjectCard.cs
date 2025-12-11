using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class CardInfo
{
    public int IdCard { get; set; }
    public string IdCardUser { get; set; }
    public string CardDetail { get; set; }
    public int Value { get; set; }
    public string Url { get; set; }
    public int Lever { get; set; }
        public int ConditionUse { get; set; }

    public CardInfo(int idCard, string idCardUser, string cardDetail, int value, string url, int lever,int conditionUse)
    {
        IdCard = idCard;
        IdCardUser = idCardUser;
        CardDetail = cardDetail;
        Value = value;
        Url = url;
        Lever = lever;
        ConditionUse = conditionUse;
    }

    // ToString method to represent the object as a string
    public override string ToString()
    {
        return $"IdCard: {IdCard}, IdCardUser: {IdCardUser}, CardDetail: {CardDetail}, Value: {Value}, Url: {Url}";
    }

}