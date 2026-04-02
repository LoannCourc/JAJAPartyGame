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
    public float startDelay = 0.35f;
    public float popDuration = 0.4f;
    public float delayBetweenCards = 0.08f;
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

        container.DOKill();
        foreach (Transform child in container) Destroy(child.gameObject);

        StartCoroutine(SpawnCardsRoutine());
    }

    IEnumerator SpawnCardsRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        foreach (var config in GoogleSheetLoader.Instance.sheetConfigs)
        {
            if (config.isInterfaceSheet) continue;

            string gameKey = config.gameKey;
            if (!GoogleSheetLoader.Instance.gameDatabase.ContainsKey(gameKey)) continue;

            GameObject go = Instantiate(gameCardPrefab, container);
            go.transform.localScale = Vector3.zero;

            SetupCardData(go, gameKey, config);

            go.transform.DOScale(Vector3.one, popDuration).SetEase(popEase);
            yield return new WaitForSeconds(delayBetweenCards);
        }

        isAnimating = false;
    }

    private void SetupCardData(GameObject go, string gameKey, SheetLink config)
    {
        TMP_Text title = go.transform.Find("TitleGame")?.GetComponent<TMP_Text>();
        TMP_Text desc = go.transform.Find("GameDescription")?.GetComponent<TMP_Text>();
        Image iconImg = go.transform.Find("GameImage")?.GetComponent<Image>();
        Button btn = go.GetComponent<Button>();

        // Le titre prend le nom localisé via la GameKey
        if (title != null) title.text = LocalizationManager.Instance.GetText(gameKey);

        if (desc != null)
        {
            if (GoogleSheetLoader.Instance.gameDescriptions.ContainsKey(gameKey))
                desc.text = LocalizationManager.Instance.GetText("desc_" + config.gameKey);
            else
                desc.text = "Lance une partie !";
        }

        if (iconImg != null && GoogleSheetLoader.Instance.gameIcons.ContainsKey(gameKey))
            iconImg.sprite = GoogleSheetLoader.Instance.gameIcons[gameKey];

        btn.onClick.AddListener(() =>
        {
            GameManager.Instance.selectedGameMode = gameKey;
            GameManager.Instance.SelectGame(gameKey);
            if (selectedGameTitle != null) selectedGameTitle.text = LocalizationManager.Instance.GetText(gameKey);
            NavigationManager.Instance.OpenFilters();
        });
    }
}