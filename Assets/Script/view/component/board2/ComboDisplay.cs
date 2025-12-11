using UnityEngine;
using UnityEngine.UI;

public class ComboDisplay : MonoBehaviour
{
    public Text comboText;
    public GameObject comboPanel;
    private int currentCombo = 0;
    
    public void ShowCombo(int combo)
    {
        if (combo <= 1)
        {
            HideCombo();
            return;
        }
        
        currentCombo = combo;
        comboPanel.SetActive(true);
        comboText.text = $"COMBO x{combo}!";
        
        // ✅ ANIMATION
        comboPanel.transform.localScale = Vector3.zero;
        LeanTween.scale(comboPanel, Vector3.one * 1.2f, 0.3f)
            .setEase(LeanTweenType.easeOutBack);
        
        // ✅ PULSE
        LeanTween.scale(comboPanel, Vector3.one, 0.2f)
            .setDelay(0.3f)
            .setLoopPingPong(1);
    }
    
    public void HideCombo()
    {
        LeanTween.scale(comboPanel, Vector3.zero, 0.2f)
            .setEase(LeanTweenType.easeInBack)
            .setOnComplete(() => comboPanel.SetActive(false));
    }
    
    public void ResetCombo()
    {
        currentCombo = 0;
        HideCombo();
    }
}