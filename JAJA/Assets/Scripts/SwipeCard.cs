using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SwipeCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 startPointerPos;
    private Vector2 initialCardPos;
    private float swipeThreshold = 150f; 
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialCardPos = rectTransform.anchoredPosition;
    }

    // --- INTERACTION MANUELLE (DOIGT) ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        startPointerPos = eventData.position;
        rectTransform.DOKill(); 
    }

    public void OnDrag(PointerEventData eventData)
    {
        float offsetX = eventData.position.x - startPointerPos.x;
        rectTransform.anchoredPosition = new Vector2(initialCardPos.x + offsetX, initialCardPos.y);
        rectTransform.rotation = Quaternion.Euler(0, 0, -offsetX * 0.05f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float difference = eventData.position.x - startPointerPos.x;
        if (Mathf.Abs(difference) > swipeThreshold)
        {
            // Swipe manuel réussi
            PerformFullSwipe(difference > 0);
        }
        else
        {
            // Retour au centre
            rectTransform.DOAnchorPos(initialCardPos, 0.2f).SetEase(Ease.OutBack);
            rectTransform.DORotate(Vector3.zero, 0.2f);
        }
    }

    // --- L'ANIMATION UNIQUE (Utilisée par le doigt ET le bouton) ---
    public void PerformFullSwipe(bool toRight)
    {
        float targetX = toRight ? initialCardPos.x + 1500 : initialCardPos.x - 1500;
        float targetRot = toRight ? -20f : 20f;

        // 1. Sortie de la carte
        rectTransform.DOAnchorPosX(targetX, 0.3f).SetEase(Ease.InSine);
        rectTransform.DORotate(new Vector3(0, 0, targetRot), 0.3f).OnComplete(() => 
        {
            // 2. Changement des données (pendant que la carte est invisible)
            GameplayManager.Instance.UpdateDataOnly();

            // 3. Reset position (Scale 0)
            rectTransform.anchoredPosition = initialCardPos;
            rectTransform.rotation = Quaternion.identity;
            rectTransform.localScale = Vector3.zero;

            // 4. Apparition (Pop)
            rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        });
    }
}