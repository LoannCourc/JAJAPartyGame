using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameMenuManager : MonoBehaviour
{
    [Header("Références")]
    public GoogleSheetLoader sheetLoader;
    public GameObject gameCardPrefab;
    public Transform container;


    [Header("Navigation")]
    public TMP_Text selectedGameTitle; // Le texte du titre dans l'écran filtres
                                       // On ne génère plus dans Start() pour éviter les erreurs de timing


    void OnEnable()
    {
        // Grâce au Script Execution Order, Instance ne sera plus nulle
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.OnMenuOpened += CheckIfIDisplay;
        }
    }

    private void DelayedSubscribe()
    {
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.OnMenuOpened += CheckIfIDisplay;
        }
    }

    void OnDisable()
    {
        // On vérifie toujours la nullité avant de se désabonner
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.OnMenuOpened -= CheckIfIDisplay;
        }
    }

    private void CheckIfIDisplay(GameObject openedMenu)
    {
        // On utilise gameObject car ce script est attaché au panel GameSelectionMenu
        if (openedMenu == this.gameObject || openedMenu.name == "GameSelectionMenu")
        {
            DisplayGames();
        }
    }
   public void DisplayGames()
{
    // 1. Nettoyage habituel
    foreach (Transform child in container) Destroy(child.gameObject);

    // 2. On boucle sur sheetConfigs au lieu de gameDatabase pour GARDER L'ORDRE
    foreach (var config in GoogleSheetLoader.Instance.sheetConfigs)
    {
        string gameName = config.gameName;

        // On vérifie si les données de ce jeu ont bien été téléchargées
        if (!GoogleSheetLoader.Instance.gameDatabase.ContainsKey(gameName)) continue;

        GameObject go = Instantiate(gameCardPrefab, container);

        // --- RÉFÉRENCES ---
        TMP_Text title = go.transform.Find("TitleGame").GetComponent<TMP_Text>();
        TMP_Text desc = go.transform.Find("GameDescription").GetComponent<TMP_Text>();
        Image iconImg = go.transform.Find("GameImage").GetComponent<Image>();
        Button btn = go.GetComponent<Button>();

        // --- ATTRIBUTION ---
        title.text = gameName;

        if (GoogleSheetLoader.Instance.gameDescriptions.ContainsKey(gameName))
            desc.text = GoogleSheetLoader.Instance.gameDescriptions[gameName];

        if (GoogleSheetLoader.Instance.gameIcons.ContainsKey(gameName))
            iconImg.sprite = GoogleSheetLoader.Instance.gameIcons[gameName];

        btn.onClick.AddListener(() =>
        {
            GameManager.Instance.selectedGameMode = gameName;
            NavigationManager.Instance.OpenFilters();
        });
    }
}

    void OnGameSelected(string name)
    {
        // 1. On enregistre le choix dans le GameManager
        GameManager.Instance.SelectGame(name);

        // 2. On met à jour le texte du titre dans l'écran de filtres
        if (selectedGameTitle != null)
            selectedGameTitle.text = name;

        NavigationManager.Instance.OpenFilters();
    }
}