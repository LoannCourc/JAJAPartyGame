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
    // --- L'ANIMATION UNIQUE (Utilisée par le doigt ET le bouton) ---
    public void PerformFullSwipe(bool toRight)
    {
        float targetX = toRight ? initialCardPos.x + 1500 : initialCardPos.x - 1500;
        float targetRot = toRight ? -20f : 20f;

        // 1. Sortie de la carte
        rectTransform.DOAnchorPosX(targetX, 0.3f).SetEase(Ease.InSine);
        rectTransform.DORotate(new Vector3(0, 0, targetRot), 0.3f).OnComplete(() => 
        {
            // 2. Changement des données ET vérification s'il reste des cartes
            bool hasNextCard = GameplayManager.Instance.UpdateDataOnly();

            if (hasNextCard)
            {
                // 3. Reset position (Scale 0)
                rectTransform.anchoredPosition = initialCardPos;
                rectTransform.rotation = Quaternion.identity;
                rectTransform.localScale = Vector3.zero;

                // 4. Apparition (Pop) de la nouvelle carte
                rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
            else
            {
                // S'il n'y a plus de carte, on la remet au centre mais on la laisse invisible (Scale 0)
                // pour qu'elle soit prête pour la prochaine partie sans gêner le menu de fin.
                rectTransform.anchoredPosition = initialCardPos;
                rectTransform.rotation = Quaternion.identity;
                rectTransform.localScale = Vector3.zero;
            }
        });
    }
    // --- RESET VISUEL POUR LE REPLAY ---
    // --- RESET VISUEL POUR LE REPLAY ---
    public void ResetCardVisually()
    {
        // SÉCURITÉ : Si la carte n'a pas encore eu le temps de faire son Awake()
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            initialCardPos = rectTransform.anchoredPosition;
        }

        // On stoppe toute animation potentiellement bloquée
        rectTransform.DOKill(); 
        
        // On remet la carte bien au centre, droite, et surtout visible !
        rectTransform.anchoredPosition = initialCardPos;
        rectTransform.rotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one; 
    }
}