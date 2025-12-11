using System.Collections;
using UnityEngine;

/// <summary>
/// Hiệu ứng đơn giản khi viên bị phá hủy: Fade + Scale
/// </summary>
public class DotDestroyEffect : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Thời gian animation (fade + scale)")]
    public float duration = 0.2f;

    private SpriteRenderer spriteRenderer;
    private bool isDestroying = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Bắt đầu hiệu ứng phá hủy
    /// </summary>
    public void PlayDestroyEffect(System.Action onComplete = null)
    {
        if (isDestroying) return;
        
        isDestroying = true;
        StartCoroutine(FadeAndShrink(onComplete));
    }

    /// <summary>
    /// Mờ dần và thu nhỏ đồng thời
    /// </summary>
    private IEnumerator FadeAndShrink(System.Action onComplete)
    {
        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Thu nhỏ về 0
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);

            // Mờ dần alpha về 0
            if (spriteRenderer != null)
            {
                Color newColor = startColor;
                newColor.a = Mathf.Lerp(1f, 0f, progress);
                spriteRenderer.color = newColor;
            }

            yield return null;
        }

        // Đảm bảo trạng thái cuối cùng
        transform.localScale = Vector3.zero;
        if (spriteRenderer != null)
        {
            Color finalColor = spriteRenderer.color;
            finalColor.a = 0f;
            spriteRenderer.color = finalColor;
        }

        // Callback
        onComplete?.Invoke();
    }

    /// <summary>
    /// Phương thức tĩnh để gọi từ bên ngoài
    /// </summary>
    public static void PlayEffect(GameObject dotObject, System.Action onComplete = null)
    {
        if (dotObject == null) return;

        DotDestroyEffect effect = dotObject.GetComponent<DotDestroyEffect>();
        if (effect == null)
        {
            effect = dotObject.AddComponent<DotDestroyEffect>();
        }

        effect.PlayDestroyEffect(onComplete);
    }
}