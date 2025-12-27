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

        // ✅ KIỂM TRA OBJECT CÓ ACTIVE KHÔNG
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[DotDestroyEffect] Cannot play effect on inactive object: {gameObject.name}");
            onComplete?.Invoke();
            return;
        }
        
        isDestroying = true;
        StartCoroutine(FadeAndShrink(onComplete));
    }

    /// <summary>
    /// Mờ dần và thu nhỏ đồng thời
    /// </summary>
    private IEnumerator FadeAndShrink(System.Action onComplete)
    {
        // ✅ KIỂM TRA LƯỢT 1: Trước khi bắt đầu
        if (!gameObject.activeInHierarchy)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            // ✅ KIỂM TRA LƯỢT 2: Mỗi frame trong animation
            if (gameObject == null || !gameObject.activeInHierarchy)
            {
                onComplete?.Invoke();
                yield break;
            }

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

        // ✅ KIỂM TRA LƯỢT 3: Trước khi set trạng thái cuối
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            // Đảm bảo trạng thái cuối cùng
            transform.localScale = Vector3.zero;
            if (spriteRenderer != null)
            {
                Color finalColor = spriteRenderer.color;
                finalColor.a = 0f;
                spriteRenderer.color = finalColor;
            }
        }

        // Callback
        onComplete?.Invoke();
    }

    /// <summary>
    /// Phương thức tĩnh để gọi từ bên ngoài
    /// </summary>
    public static void PlayEffect(GameObject dotObject, System.Action onComplete = null)
    {
        // ✅ KIỂM TRA NULL
        if (dotObject == null)
        {
            Debug.LogWarning("[DotDestroyEffect] dotObject is null!");
            onComplete?.Invoke();
            return;
        }

        // ✅ KIỂM TRA ACTIVE TRƯỚC KHI THÊM COMPONENT
        if (!dotObject.activeInHierarchy)
        {
            Debug.LogWarning($"[DotDestroyEffect] Cannot play effect on inactive object: {dotObject.name}");
            onComplete?.Invoke();
            return;
        }

        DotDestroyEffect effect = dotObject.GetComponent<DotDestroyEffect>();
        if (effect == null)
        {
            effect = dotObject.AddComponent<DotDestroyEffect>();
        }

        effect.PlayDestroyEffect(onComplete);
    }
}