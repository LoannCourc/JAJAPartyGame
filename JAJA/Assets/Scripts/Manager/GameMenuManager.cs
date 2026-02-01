using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening; 

public class GameMenuManager : MonoBehaviour
{
    [Header("Références")]
    public GoogleSheetLoader sheetLoader;
    public GameObject gameCardPrefab;
    public Transform container;

    [Header("Navigation")]
    public TMP_Text selectedGameTitle;

    [Header("Animation Settings (DOTween)")]
    public float popDuration = 0.4f;
    public float delayBetweenCards = 0.08f;
    public Ease popEase = Ease.OutBack;

    private bool isAnimating = false; // Sécurité pour éviter les doublons

    void OnEnable()
    {
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.OnMenuOpened += CheckIfIDisplay;
        }
    }

    void OnDisable()
    {
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.OnMenuOpened -= CheckIfIDisplay;
        }
        // Nettoyage si on quitte brusquement le menu
        isAnimating = false;
        StopAllCoroutines();
    }

    private void CheckIfIDisplay(GameObject openedMenu)
    {
        // On vérifie le nom ou la référence de l'objet
        if (openedMenu == this.gameObject || openedMenu.name == "GameSelectionMenu")
        {
            DisplayGames();
        }
    }

    public void DisplayGames()
    {
        // Si une animation est déjà en cours, on l'arrête proprement pour recommencer
        if (isAnimating)
        {
            StopAllCoroutines();
        }

        // 1. Nettoyage immédiat
        isAnimating = true;
        DOTween.KillAll(); // Arrête les DOScale en cours
        
        foreach (Transform child in container) 
        {
            Destroy(child.gameObject);
        }

        // 2. Lancement de la routine de création
        StartCoroutine(SpawnCardsRoutine());
    }

    IEnumerator SpawnCardsRoutine()
    {
        // Petit délai de sécurité pour laisser le temps au moteur de détruire les anciens objets
        yield return new WaitForEndOfFrame();

        // On utilise l'ordre de sheetConfigs comme convenu
        foreach (var config in GoogleSheetLoader.Instance.sheetConfigs)
        {
            string gameName = config.gameName;
            
            // On vérifie que les données existent dans le dictionnaire
            if (!GoogleSheetLoader.Instance.gameDatabase.ContainsKey(gameName)) continue;

            GameObject go = Instantiate(gameCardPrefab, container);
            
            // Initialisation scale à 0
            go.transform.localScale = Vector3.zero;

            // Remplissage des données
            SetupCardData(go, gameName, config);

            // Animation DOTween
            go.transform.DOScale(Vector3.one, popDuration).SetEase(popEase);

            yield return new WaitForSeconds(delayBetweenCards);
        }

        isAnimating = false;
    }

    private void SetupCardData(GameObject go, string gameName, SheetLink config)
    {
        TMP_Text title = go.transform.Find("TitleGame")?.GetComponent<TMP_Text>();
        TMP_Text desc = go.transform.Find("GameDescription")?.GetComponent<TMP_Text>();
        Image iconImg = go.transform.Find("GameImage")?.GetComponent<Image>();
        Button btn = go.GetComponent<Button>();

        if (title != null) title.text = gameName;
        
        if (desc != null)
        {
            if (GoogleSheetLoader.Instance.gameDescriptions.ContainsKey(gameName))
                desc.text = GoogleSheetLoader.Instance.gameDescriptions[gameName];
            else
                desc.text = "Lance une partie !";
        }

        if (iconImg != null && GoogleSheetLoader.Instance.gameIcons.ContainsKey(gameName))
        {
            iconImg.sprite = GoogleSheetLoader.Instance.gameIcons[gameName];
        }

        btn.onClick.AddListener(() =>
        {
            GameManager.Instance.selectedGameMode = gameName;
            GameManager.Instance.SelectGame(gameName);
            if (selectedGameTitle != null) selectedGameTitle.text = gameName;
            NavigationManager.Instance.OpenFilters();
        });
    }
}