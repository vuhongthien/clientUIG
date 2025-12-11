using UnityEngine;
using UnityEngine.UI;

namespace Poki.Assets.Script.Boss.xephang
{
    public class TopPlayerItem : MonoBehaviour
    {
        public Text txtTop;
        public Image imgPet;
        public Image imgbg;
        public Text txtName;
        public Text txtDame;

        public void SetupTopPlayer(BossRankingPlayerDTO player)
        {
            if (txtTop != null)
            {
                txtTop.text = "Top "+player.rank.ToString();
            }
            
            if (txtName != null)
            {
                txtName.text = player.userName;
            }
            
            if (txtDame != null)
            {
                txtDame.text = player.totalDamage.ToString();
            }
            
            // Đổi màu background theo rank
            if (imgbg != null)
            {
                if (player.rank == 1)
                {
                    imgbg.color = new Color(1f, 0.84f, 0f); // Vàng
                }
                else if (player.rank == 2)
                {
                    imgbg.color = new Color(0.75f, 0.75f, 0.75f); // Bạc
                }
                else if (player.rank == 3)
                {
                    imgbg.color = new Color(0.8f, 0.5f, 0.2f); // Đồng
                }
                else
                {
                    imgbg.color = Color.white;
                }
            }
            
            // Load avatar pet
            if (imgPet != null && player.petId > 0)
            {
                LoadPetAvatar(player.petId);
            }
        }

        void LoadPetAvatar(long petId)
        {
            if (imgPet == null)
            {
                Debug.LogWarning("[TopPlayerItem] imgPet is null!");
                return;
            }
            
            // Load sprite từ Resources/Image/IconsPet
            string spritePath = $"Image/IconsPet/{petId}";
            Sprite petSprite = Resources.Load<Sprite>(spritePath);
            
            if (petSprite != null)
            {
                imgPet.sprite = petSprite;
                Debug.Log($"[TopPlayerItem] Loaded pet avatar: {spritePath}");
            }
            else
            {
                Debug.LogWarning($"[TopPlayerItem] Cannot find pet sprite at: {spritePath}");
                // Load sprite mặc định
                Sprite defaultSprite = Resources.Load<Sprite>("Image/IconsPet/default");
                if (defaultSprite != null)
                {
                    imgPet.sprite = defaultSprite;
                }
            }
        }
    }
}
