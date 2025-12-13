using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;
using System.Collections.Generic;
using Unity.Android.Gradle.Manifest;

namespace EasyUI.PickerWheelUI
{
    public class PickerWheel : MonoBehaviour
    {
        [Header("References :")]
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private Transform linesParent;

        [Space]
        [SerializeField] private Transform PickerWheelTransform;
        [SerializeField] private Transform wheelCircle;
        [SerializeField] private GameObject wheelPiecePrefab;
        [SerializeField] private Transform wheelPiecesParent;

        private GameObject wheelPiecePrefabInstance; // Instance tạm để generate

        [Space]
        [Header("Sounds :")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip tickAudioClip;
        [SerializeField][Range(0f, 1f)] private float volume = .5f;
        [SerializeField][Range(-3f, 3f)] private float pitch = 1f;

        [Space]
        [Header("Picker wheel settings :")]
        [Range(1, 20)] public int spinDuration = 8;
        [SerializeField][Range(.2f, 2f)] private float wheelSize = 1f;

        [Space]
        [Header("Picker wheel pieces :")]
        public WheelPiece[] wheelPieces;

        // Events
        private UnityAction onSpinStartEvent;
        private UnityAction<WheelPiece> onSpinEndEvent;

        private bool _isSpinning = false;
        public bool IsSpinning { get { return _isSpinning; } }

        private Vector2 pieceMinSize = new Vector2(81f, 146f);
        private Vector2 pieceMaxSize = new Vector2(144f, 213f);
        private int piecesMin = 2;
        private int piecesMax = 12;

        private float pieceAngle;
        private float halfPieceAngle;
        private float halfPieceAngleWithPaddings;

        private double accumulatedWeight;
        private System.Random rand = new System.Random();
        private List<int> nonZeroChancesIndices = new List<int>();

        private bool isGenerated = false;

        private void Start()
        {
            // ManagerWheelDay will call SetupWheel() after loading data from API
        }

        private bool needsRegenerate = false;

public void SetupWheel()
{
    if (wheelPieces == null || wheelPieces.Length == 0)
    {
        Debug.LogError("[PickerWheel] wheelPieces is empty!");
        return;
    }

    _isSpinning = false;

    if (isGenerated)
    {
        // Đánh dấu cần regenerate
        isGenerated = false;
        ClearWheel();
        needsRegenerate = true;
    }
    else
    {
        SetupWheelImmediate();
    }
}

private void LateUpdate()
{
    if (needsRegenerate)
    {
        needsRegenerate = false;
        SetupWheelImmediate();
    }
}

        private System.Collections.IEnumerator SetupWheelCoroutine()
        {
            isGenerated = false;

            // Clear wheel
            ClearWheel();

            // Đợi 1 frame để Destroy hoàn tất
            yield return null;

            // Setup lại
            SetupWheelImmediate();
        }

        private void SetupWheelImmediate()
        {
            pieceAngle = 360f / wheelPieces.Length;
            halfPieceAngle = pieceAngle / 2f;
            halfPieceAngleWithPaddings = halfPieceAngle - (halfPieceAngle / 4f);

            Generate();
            CalculateWeightsAndIndices();

            if (nonZeroChancesIndices.Count == 0)
                Debug.LogError("[PickerWheel] All pieces have zero chance!");

            SetupAudio();
            isGenerated = true;

            Debug.Log($"[PickerWheel] Setup complete with {wheelPieces.Length} pieces");
        }

        public void RecalculateWeights()
        {
            accumulatedWeight = 0;
            nonZeroChancesIndices.Clear();

            for (int i = 0; i < wheelPieces.Length; i++)
            {
                WheelPiece piece = wheelPieces[i];
                accumulatedWeight += piece.Chance;
                piece._weight = accumulatedWeight;
                piece.Index = i;

                if (piece.Chance > 0)
                    nonZeroChancesIndices.Add(i);
            }

            Debug.Log($"[PickerWheel] Weights recalculated. Total: {accumulatedWeight}");
        }

        private void SetupAudio()
        {
            if (audioSource != null && tickAudioClip != null)
            {
                audioSource.clip = tickAudioClip;
                audioSource.volume = volume;
                audioSource.pitch = pitch;
            }
        }

        private void Generate()
        {
            ClearWheel();

            // Reset góc quay của wheel về 0
            if (wheelCircle != null)
            {
                wheelCircle.rotation = Quaternion.identity;
                wheelCircle.localRotation = Quaternion.identity;
                Debug.Log("[PickerWheel] Wheel rotation reset to 0");
            }

            // Reset parent position/rotation
            if (wheelPiecesParent != null)
            {
                wheelPiecesParent.localRotation = Quaternion.identity;
            }

            if (linesParent != null)
            {
                linesParent.localRotation = Quaternion.identity;
            }

            // Tạo instance tạm từ prefab gốc
            wheelPiecePrefabInstance = InstantiatePiece();

            RectTransform rt = wheelPiecePrefabInstance.transform.GetChild(0).GetComponent<RectTransform>();
            float pieceWidth = Mathf.Lerp(pieceMinSize.x, pieceMaxSize.x, 1f - Mathf.InverseLerp(piecesMin, piecesMax, wheelPieces.Length));
            float pieceHeight = Mathf.Lerp(pieceMinSize.y, pieceMaxSize.y, 1f - Mathf.InverseLerp(piecesMin, piecesMax, wheelPieces.Length));
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pieceWidth);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pieceHeight);

            Debug.Log($"[PickerWheel] Starting to draw {wheelPieces.Length} pieces");

            for (int i = 0; i < wheelPieces.Length; i++)
                DrawPiece(i);

            // Destroy instance tạm, KHÔNG destroy prefab gốc
            if (wheelPiecePrefabInstance != null)
            {
                Destroy(wheelPiecePrefabInstance);
                wheelPiecePrefabInstance = null;
            }

            Debug.Log("[PickerWheel] Generate complete");
        }

        private void ClearWheel()
        {
            // Kill tất cả DOTween animation trên wheelCircle
            if (wheelCircle != null)
            {
                wheelCircle.DOKill();
            }

            if (wheelPiecesParent != null)
            {
                int childCount = wheelPiecesParent.childCount;
                Debug.Log($"[PickerWheel] Clearing {childCount} pieces");

                for (int i = childCount - 1; i >= 0; i--)
                {
                    Transform child = wheelPiecesParent.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            if (linesParent != null)
            {
                int lineCount = linesParent.childCount;
                Debug.Log($"[PickerWheel] Clearing {lineCount} lines");

                for (int i = lineCount - 1; i >= 0; i--)
                {
                    Transform child = linesParent.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }

        private void DrawPiece(int index)
        {
            WheelPiece piece = wheelPieces[index];
            // Sử dụng instance tạm thay vì prefab gốc
            GameObject pieceObj = Instantiate(wheelPiecePrefabInstance, wheelPiecesParent.position, Quaternion.identity, wheelPiecesParent);
            Transform pieceTrns = pieceObj.transform.GetChild(0);

            // Set name để dễ debug
            pieceObj.name = $"Piece_{index}_{piece.Label}";

            Image iconImage = pieceTrns.GetChild(0).GetComponent<Image>();
            if (iconImage != null && piece.Icon != null)
            {
                iconImage.sprite = piece.Icon;
            }

            Text labelText = pieceTrns.GetChild(1).GetComponent<Text>();
            if (labelText != null)
            {
                labelText.text = piece.Label;
            }

            Text amountText = pieceTrns.GetChild(2).GetComponent<Text>();
            if (amountText != null)
            {
                amountText.text = piece.Amount > 1 ? FormatVND(piece.Amount) : "";
            }

            // Xoay piece quanh tâm wheel
            float rotationAngle = pieceAngle * index;
            pieceTrns.RotateAround(wheelPiecesParent.position, Vector3.back, rotationAngle);

            // Vẽ line
            if (linePrefab != null && linesParent != null)
            {
                GameObject lineObj = Instantiate(linePrefab, linesParent.position, Quaternion.identity, linesParent);
                lineObj.name = $"Line_{index}";
                Transform lineTrns = lineObj.transform;
                lineTrns.RotateAround(wheelPiecesParent.position, Vector3.back, rotationAngle + halfPieceAngle);
            }

            Debug.Log($"[PickerWheel] Drew piece {index}: {piece.Label} at angle {rotationAngle}°");
        }

        public static string FormatVND(long amount)
    {
        return amount.ToString("#,##0").Replace(",", ".");
    }

        private GameObject InstantiatePiece()
        {
            return Instantiate(wheelPiecePrefab, wheelPiecesParent.position, Quaternion.identity, wheelPiecesParent);
        }

        public void Spin()
        {
            if (_isSpinning)
            {
                Debug.LogWarning("[PickerWheel] Already spinning!");
                return;
            }

            if (!isGenerated)
            {
                Debug.LogError("[PickerWheel] Wheel not generated! Call SetupWheel() first.");
                return;
            }

            _isSpinning = true;

            if (onSpinStartEvent != null)
                onSpinStartEvent.Invoke();

            int index = GetRandomPieceIndex();
            WheelPiece piece = wheelPieces[index];

            if (piece.Chance == 0 && nonZeroChancesIndices.Count != 0)
            {
                index = nonZeroChancesIndices[Random.Range(0, nonZeroChancesIndices.Count)];
                piece = wheelPieces[index];
            }

            // Tính góc đích (vị trí piece sẽ dừng)
            float targetAngle = -(pieceAngle * index);

            // Random offset nhỏ trong khoảng piece để tự nhiên hơn
            float randomOffset = Random.Range(-halfPieceAngle * 0.3f, halfPieceAngle * 0.3f);
            targetAngle += randomOffset;

            // Số vòng quay: 5 vòng = 1800 độ (quay 1 chiều)
            float totalRotation = 1800f + targetAngle;

            // Rotation target
            Vector3 targetRotation = Vector3.back * totalRotation;

            float prevAngle = wheelCircle.eulerAngles.z;
            float currentAngle = wheelCircle.eulerAngles.z;
            bool isIndicatorOnTheLine = false;

            wheelCircle
                .DORotate(targetRotation, spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear) // Quay đều 1 tốc độ
                .OnUpdate(() =>
                {
                    currentAngle = wheelCircle.eulerAngles.z;
                    float diff = Mathf.Abs(Mathf.DeltaAngle(prevAngle, currentAngle));

                    if (diff >= halfPieceAngle)
                    {
                        if (isIndicatorOnTheLine && audioSource != null && audioSource.clip != null)
                        {
                            audioSource.PlayOneShot(audioSource.clip);
                        }
                        prevAngle = currentAngle;
                        isIndicatorOnTheLine = !isIndicatorOnTheLine;
                    }
                })
                .OnComplete(() =>
                {
                    _isSpinning = false;

                    if (onSpinEndEvent != null)
                        onSpinEndEvent.Invoke(piece);

                    onSpinStartEvent = null;
                    onSpinEndEvent = null;
                });
        }

        public void SpinToIndex(int targetIndex)
        {
            if (_isSpinning)
            {
                Debug.LogWarning("[PickerWheel] Already spinning!");
                return;
            }

            if (targetIndex < 0 || targetIndex >= wheelPieces.Length)
            {
                Debug.LogError($"[PickerWheel] Invalid target index: {targetIndex}");
                return;
            }

            _isSpinning = true;

            if (onSpinStartEvent != null)
                onSpinStartEvent.Invoke();

            WheelPiece piece = wheelPieces[targetIndex];

            float angle = -(pieceAngle * targetIndex);
            float randomOffset = Random.Range(-halfPieceAngle * 0.3f, halfPieceAngle * 0.3f);
            float finalAngle = angle + randomOffset;

            float currentRotation = wheelCircle.eulerAngles.z;
            float targetAngle = -(360f * spinDuration) + finalAngle;
            Vector3 targetRotation = new Vector3(0, 0, currentRotation + targetAngle);

            float prevAngle = currentRotation;
            bool isIndicatorOnTheLine = false;

            wheelCircle
                .DORotate(targetRotation, spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.InOutCubic)
                .OnUpdate(() =>
                {
                    float currentAngle = wheelCircle.eulerAngles.z;
                    float diff = Mathf.Abs(Mathf.DeltaAngle(prevAngle, currentAngle));

                    if (diff >= halfPieceAngle)
                    {
                        if (isIndicatorOnTheLine && audioSource != null && audioSource.clip != null)
                        {
                            audioSource.PlayOneShot(audioSource.clip);
                        }
                        prevAngle = currentAngle;
                        isIndicatorOnTheLine = !isIndicatorOnTheLine;
                    }
                })
                .OnComplete(() =>
                {
                    _isSpinning = false;

                    if (onSpinEndEvent != null)
                        onSpinEndEvent.Invoke(piece);

                    onSpinStartEvent = null;
                    onSpinEndEvent = null;
                });
        }

        public void OnSpinStart(UnityAction action)
        {
            onSpinStartEvent = action;
        }

        public void OnSpinEnd(UnityAction<WheelPiece> action)
        {
            onSpinEndEvent = action;
        }

        private int GetRandomPieceIndex()
        {
            double r = rand.NextDouble() * accumulatedWeight;

            for (int i = 0; i < wheelPieces.Length; i++)
                if (wheelPieces[i]._weight >= r)
                    return i;

            return 0;
        }

        private void CalculateWeightsAndIndices()
        {
            accumulatedWeight = 0;
            nonZeroChancesIndices.Clear();

            for (int i = 0; i < wheelPieces.Length; i++)
            {
                WheelPiece piece = wheelPieces[i];

                accumulatedWeight += piece.Chance;
                piece._weight = accumulatedWeight;
                piece.Index = i;

                if (piece.Chance > 0)
                    nonZeroChancesIndices.Add(i);
            }
        }

        private void OnValidate()
        {
            if (PickerWheelTransform != null)
                PickerWheelTransform.localScale = new Vector3(wheelSize, wheelSize, 1f);

            if (wheelPieces != null && (wheelPieces.Length > piecesMax || wheelPieces.Length < piecesMin))
                Debug.LogError("[PickerWheel] Pieces length must be between " + piecesMin + " and " + piecesMax);
        }
        public void ResetWheelRotation(float duration = 0.5f, Action onComplete = null)
        {
            wheelCircle.DOKill(); // Kill animation cũ
            wheelCircle.DORotate(Vector3.zero, duration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic); // Quay về 0 mượt mà
        }
    }

}