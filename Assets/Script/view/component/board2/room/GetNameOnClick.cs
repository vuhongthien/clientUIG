using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.EventSystems;
using System;
using System.Text.RegularExpressions;

public class GetNameOnClick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private BoardController boardController;
    private LoadRoom loadRoom;
    private bool isPointerInside = false; // Cờ để kiểm tra chuột có ở trong Button không

    // Gọi khi chuột được nhấn xuống trên Button
    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerInside = true; // Đặt cờ khi chuột nhấn vào Button
    }

    // Gọi khi chuột nhả ra
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPointerInside) // Chỉ chạy nếu chuột được thả bên trong Button
        {
            boardController = FindFirstObjectByType<BoardController>();
            loadRoom = FindFirstObjectByType<LoadRoom>();

            if (loadRoom != null)
            {
                // Lấy Image từ đối tượng con
                Image childImageComponent = gameObject.transform.GetChild(0).GetComponent<Image>();
                if (childImageComponent != null && childImageComponent.sprite != null)
                {
                    // Lấy Sprite từ Image
                    Sprite newSprite = childImageComponent.sprite;

                    // Gán sprite cho Image trong LoadRoom
                    Image imageComponent = loadRoom.pet.GetComponent<Image>();
                    if (imageComponent != null)
                    {
                        imageComponent.sprite = newSprite;
                    }
                    else
                    {
                        Debug.LogError("Không tìm thấy Image trên LoadRoom.pet.");
                    }
                }
                else
                {
                    Debug.LogWarning("Image hoặc Sprite trên Image không hợp lệ.");
                }

                // Chuyển đổi ID Pet
                if (int.TryParse(gameObject.name, out int petUserId))
                {
                    loadRoom.petUser = petUserId;
                    Debug.Log($"Đã chuyển đổi ID pet thành số nguyên: {petUserId}");
                    StartCoroutine(CallRoomWaitAPI(loadRoom.nguoiChoi, loadRoom.petUser));
                }
                else
                {
                    Debug.LogError($"Tên GameObject không phải số nguyên hợp lệ: {gameObject.name}");
                }
            }
            else
            {
                Debug.LogError("Không tìm thấy LoadRoom hoặc BoardController.");
            }
        }
    }


    // Gọi khi chuột rời khỏi Button trong khi đang nhấn
    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false; // Đặt cờ thành false nếu chuột rời khỏi Button
    }

    private IEnumerator CallRoomWaitAPI(int userId, int petId)
    {
        string url = $"https://pokiwar70-production.up.railway.app/api/v1/roomWait/pet?userId={userId}&petId={petId}";
        UnityWebRequest request = UnityWebRequest.Get(url);

        // Gửi request và đợi phản hồi
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error: {request.error}");
        }
        else
        {
            Debug.Log($"Success: {request.downloadHandler.text}");
        }
    }
}
