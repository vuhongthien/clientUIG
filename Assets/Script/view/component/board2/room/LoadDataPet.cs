using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LoadDataPet : MonoBehaviour
{
    public GameObject boardPetPanel;
    public GameObject itemPrefab;
    public Texture2D fallbackTexture;

    public void LoadPet(ChoosePet[] listChoosePet)
{
    if (boardPetPanel == null)
    {
        Debug.LogError("BoardPetPanel is not assigned.");
        return;
    }

    Transform boardTransform = boardPetPanel.transform.Find("Board");
    if (boardTransform == null)
    {
        Debug.LogError("Board child not found under BoardPetPanel.");
        return;
    }

    // Destroy all existing children safely
    List<GameObject> childrenToDestroy = new List<GameObject>();
    foreach (Transform child in boardTransform)
    {
        childrenToDestroy.Add(child.gameObject);
    }
    foreach (var child in childrenToDestroy)
    {
        Destroy(child);
    }

    // Load new pets
    foreach (var pet in listChoosePet)
    {
        GameObject itemPet = Instantiate(itemPrefab, boardTransform);
        Image iconImage = itemPet.transform.Find("Image").GetComponent<Image>();
        TextMeshProUGUI countText = itemPet.transform.Find("SL").GetComponent<TextMeshProUGUI>();
        countText.text = "LV" + pet.lever.ToString();
        itemPet.name = pet.id.ToString();

        if (pet.thumbnailPet != null)
        {
            Debug.Log("--------------------------------pet" + pet.thumbnailPet);
            iconImage.name = pet.idPet.ToString();
            Debug.Log(pet.thumbnailPet);
        }
    }
}

}
