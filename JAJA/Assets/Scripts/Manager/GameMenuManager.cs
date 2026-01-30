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
        // Nettoyage de sécurité pour éviter les doublons
        foreach (Transform child in container) Destroy(child.gameObject);

        // On vérifie si les données sont prêtes dans le dictionnaire
        if (sheetLoader.gameDatabase.Count == 0)
        {
            Debug.LogWarning("La base de données est vide au moment de l'affichage !");
            return;
        }

        foreach (var game in sheetLoader.gameDatabase)
        {
            GameObject card = Instantiate(gameCardPrefab, container);

            // 1. On vérifie le texte
            TMP_Text title = card.GetComponentInChildren<TMP_Text>();
            if (title != null) title.text = game.Key;

            // 2. On vérifie le bouton
            Button btn = card.GetComponent<Button>();

            // Si le bouton n'est pas sur la racine, on le cherche dans les enfants
            if (btn == null) btn = card.GetComponentInChildren<Button>();

            if (btn != null)
            {
                string currentGameName = game.Key; // Important pour la capture de variable
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnGameSelected(currentGameName));
            }
            else
            {
                Debug.LogError($"Erreur : Le Prefab '{card.name}' n'a pas de composant Button !");
            }
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