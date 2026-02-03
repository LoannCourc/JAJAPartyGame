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

    [Header("Animation Settings")]
    public float startDelay = 0.35f;      // Temps d'attente après l'ouverture du menu
    public float popDuration = 0.4f;      // Durée du pop de chaque carte
    public float delayBetweenCards = 0.08f; // Délai entre chaque carte (effet cascade)
    public Ease popEase = Ease.OutBack;

    private bool isAnimating = false;

    void OnEnable()
    {
        if (NavigationManager.Instance != null)
            NavigationManager.Instance.OnMenuOpened += CheckIfIDisplay;
    }

    void OnDisable()
    {
        if (NavigationManager.Instance != null)
            NavigationManager.Instance.OnMenuOpened -= CheckIfIDisplay;
        
        isAnimating = false;
        StopAllCoroutines();
    }

    private void CheckIfIDisplay(GameObject openedMenu)
    {
        if (openedMenu == this.gameObject || openedMenu.name == "GameSelectionMenu")
        {
            DisplayGames();
        }
    }

    public void DisplayGames()
    {
        if (isAnimating) StopAllCoroutines();

        isAnimating = true;
        // On ne Kill pas TOUT le DOTween pour ne pas casser l'anim du NavigationManager
        // On Kill juste les anims sur le container
        container.DOKill(); 
        
        foreach (Transform child in container) Destroy(child.gameObject);

        StartCoroutine(SpawnCardsRoutine());
    }

    IEnumerator SpawnCardsRoutine()
    {
        // On attend que le panneau parent ait presque fini son animation
        yield return new WaitForSeconds(startDelay);

        foreach (var config in GoogleSheetLoader.Instance.sheetConfigs)
        {
            string gameName = config.gameName;
            if (!GoogleSheetLoader.Instance.gameDatabase.ContainsKey(gameName)) continue;

            GameObject go = Instantiate(gameCardPrefab, container);
            go.transform.localScale = Vector3.zero;

            SetupCardData(go, gameName, config);

            // Animation individuelle de la carte
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
            iconImg.sprite = GoogleSheetLoader.Instance.gameIcons[gameName];

        btn.onClick.AddListener(() =>
        {
            GameManager.Instance.selectedGameMode = gameName;
            GameManager.Instance.SelectGame(gameName);
            if (selectedGameTitle != null) selectedGameTitle.text = gameName;
            NavigationManager.Instance.OpenFilters();
        });
    }
}