using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LoadRoom : MonoBehaviour
{
    public GameObject animationBoss;
    public Image pet;
    public Image user;
    public TextMeshProUGUI nameEnemyPet;
    public TextMeshProUGUI nameUser;
    public TextMeshProUGUI energy;
    public TextMeshProUGUI countPass;
    public TextMeshProUGUI lever;
    public Text gold;
    public Text money;
    public Texture2D fallbackTexture;

    public int nguoiChoi;
    public int petUser;
    public int petEnemy;
    public void LoadRoomData(JoinRoomData joinRoomData)
    {
        user.name = "peticon"+joinRoomData.playerId;
        animationBoss.name = joinRoomData.enemyPet.parentId.ToString();
        animationBoss.SetActive(true);
        pet.name =joinRoomData.idPet.ToString();
        nameEnemyPet.text = joinRoomData.namePetEnemy;
        nameUser.text = joinRoomData.name;
        energy.text = joinRoomData.energy+"/"+joinRoomData.energyFull;
        countPass.text = joinRoomData.countPass+"/"+joinRoomData.enemyPet.requestPass;
        lever.text = "LV"+joinRoomData.lever;
        gold.text = joinRoomData.gold.ToString();
        money.text= joinRoomData.money.ToString();
        nguoiChoi = joinRoomData.id;
        petUser = joinRoomData.idPetUser;
        petEnemy = joinRoomData.enemyPet.id;
    }

    
}
