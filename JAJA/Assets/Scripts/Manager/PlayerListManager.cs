using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerListManager : MonoBehaviour
{
    [Header("Configuration UI")]
    public GameObject playerRowPrefab; 
    public Transform container; // L'objet "Content" de la ScrollView
    public Button nextButton;

    public GameObject startMenu;

    public GameObject gameSelectionMenu;
    public GameMenuManager gameMenuManager;

    private List<GameObject> activeRows = new List<GameObject>();
    private const int MAX_PLAYERS = 10;

    void Start()
    {
        // Nettoyage au lancement
        foreach (Transform child in container) Destroy(child.gameObject);
        
        AddRow();
        AddRow();
        RefreshUI();
    }

    public void AddRow()
    {
        if (activeRows.Count >= MAX_PLAYERS) return;

        // Création de la ligne
        GameObject newRow = Instantiate(playerRowPrefab, container);
        activeRows.Add(newRow);

        // Liaison du bouton "+" à l'intérieur du Prefab
        // Assure-toi que ton bouton dans le Prefab s'appelle exactement "AddButton"
        Button plusButton = newRow.transform.Find("AddButton")?.GetComponent<Button>();
        if (plusButton != null)
        {
            plusButton.onClick.RemoveAllListeners(); // Sécurité
            plusButton.onClick.AddListener(() => OnPlusButtonClicked(newRow));
        }

        RefreshUI();
    }

    private void OnPlusButtonClicked(GameObject currentRow)
    {
        TMP_InputField input = currentRow.GetComponentInChildren<TMP_InputField>();

       // On n'ajoute une ligne que si le nom actuel n'est pas vide [cite: 17]
        if (!string.IsNullOrEmpty(input.text))
        {
            // Si on clique sur le "+" de la dernière ligne, on en crée une nouvelle
            if (activeRows.IndexOf(currentRow) == activeRows.Count - 1)
            {
                AddRow();
            }
        }
    }

    private void RefreshUI()
    {
       // Le bouton "Suivant" s'active si au moins 2 joueurs ont un nom [cite: 3, 17]
        int filledCount = 0;
        foreach (GameObject row in activeRows)
        {
            if (!string.IsNullOrEmpty(row.GetComponentInChildren<TMP_InputField>().text))
                filledCount++;
        }
        nextButton.interactable = (filledCount >= 2);
    }

   public void FinalizePlayers()
    {
        // 1. On enregistre les noms dans le GameManager
        GameManager.Instance.playerNames.Clear();
        foreach (GameObject row in activeRows)
        {
            string playerName = row.GetComponentInChildren<TMP_InputField>().text;
            if (!string.IsNullOrEmpty(playerName))
                GameManager.Instance.AddPlayer(playerName);
        }

        // 2. Transition visuelle
        startMenu.SetActive(false);
        gameSelectionMenu.SetActive(true);

        // 3. Génération dynamique des jeux
        gameMenuManager.DisplayGames();
    }
}