using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SwipeCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 startPointerPos;     // Position du doigt au début
    private Vector2 initialCardPos;      // Position de la carte dans l'UI au début
    private float swipeThreshold = 150f; 
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // On sauvegarde la position exacte définie dans l'Inspector
        initialCardPos = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPointerPos = eventData.position;
        // On s'assure de ne pas avoir de tweens en cours qui traînent
        rectTransform.DOKill(); 
    }

    public void OnDrag(PointerEventData eventData)
    {
        float offsetX = eventData.position.x - startPointerPos.x;
        
        // On applique l'offset X par rapport à la position INITIALE
        // On garde initialCardPos.y pour que ça ne bouge JAMAIS en hauteur
        rectTransform.anchoredPosition = new Vector2(initialCardPos.x + offsetX, initialCardPos.y);
        
        // Rotation légère
        rectTransform.rotation = Quaternion.Euler(0, 0, -offsetX * 0.05f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float difference = eventData.position.x - startPointerPos.x;

        if (Mathf.Abs(difference) > swipeThreshold)
        {
            // SWIPE RÉUSSI
            float targetX = difference > 0 ? initialCardPos.x + 1500 : initialCardPos.x - 1500;
            
            rectTransform.DOAnchorPosX(targetX, 0.3f).SetEase(Ease.InSine).OnComplete(() => {
                ResetCardPosition();
                GameplayManager.Instance.NextQuestion();
            });
        }
        else
        {
            // RETOUR AU CENTRE (Position initiale exacte)
            rectTransform.DOAnchorPos(initialCardPos, 0.2f).SetEase(Ease.OutBack);
            rectTransform.DORotate(Vector3.zero, 0.2f);
        }
    }

    private void ResetCardPosition()
    {
        // On remet la carte à sa position initiale exacte mémorisée au Awake
        rectTransform.anchoredPosition = initialCardPos;
        rectTransform.rotation = Quaternion.identity;
        
        // Petit effet d'apparition fluide
        rectTransform.localScale = Vector3.zero;
        rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }
}