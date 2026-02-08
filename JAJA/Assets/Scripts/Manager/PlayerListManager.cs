using UnityEngine;
using UnityEngine.UI;
using TMP_InputField = TMPro.TMP_InputField; 
using TMP_Text = TMPro.TMP_Text;
using DG.Tweening; 
using System.Collections.Generic;

public class PlayerListManager : MonoBehaviour
{
    [Header("Saisie Fixe")]
    public TMP_InputField mainInputField; 
    public Button addBtn; 

    [Header("Liste Dynamique")]
    public GameObject playerRowPrefab; 
    public Transform container; 
    public Button nextButton;
    // Référence au texte du bouton pour afficher "Chargement..."
    public TMP_Text nextButtonText; 

    public GameMenuManager gameMenuManager;

    private List<string> playerNames = new List<string>();
    private Dictionary<string, GameObject> activeRows = new Dictionary<string, GameObject>();
    private const int MAX_PLAYERS = 10;
    
    // Pour mémoriser le texte original du bouton (ex: "SUIVANT" ou "JOUER")
    private string originalButtonText;

    void Start()
    {
        // Sauvegarde du texte original
        if (nextButtonText != null) originalButtonText = nextButtonText.text;

        foreach (Transform child in container) Destroy(child.gameObject);
        activeRows.Clear();
        playerNames.Clear();

        if (GameManager.Instance != null && GameManager.Instance.playerNames.Count > 0)
        {
            foreach (string name in GameManager.Instance.playerNames)
            {
                AddPlayerVisually(name);
            }
        }

        mainInputField.text = "";
        RefreshUI();

        addBtn.onClick.RemoveAllListeners();
        addBtn.onClick.AddListener(AddPlayerFromInput);

        mainInputField.onSubmit.RemoveAllListeners();
        mainInputField.onSubmit.AddListener((val) => AddPlayerFromInput());
    }

    // --- NOUVEAU : VÉRIFICATION EN CONTINUE DE L'ÉTAT DU CHARGEMENT ---
    void Update()
    {
        // On met à jour l'interactivité du bouton en temps réel
        RefreshUI();
    }

    public void AddPlayerFromInput()
    {
        string nameToAdd = mainInputField.text.Trim();

        if (string.IsNullOrEmpty(nameToAdd) || playerNames.Contains(nameToAdd))
        {
            mainInputField.transform.DOShakePosition(0.4f, strength: new Vector3(10, 0, 0), vibrato: 10);
            return;
        }

        if (playerNames.Count >= MAX_PLAYERS) return;

        AddPlayerVisually(nameToAdd);
        GameManager.Instance.AddPlayer(nameToAdd); 

        mainInputField.text = ""; 
        mainInputField.ActivateInputField(); 
    }

    public void AddPlayerFromInput(string val) => AddPlayerFromInput();

    private void AddPlayerVisually(string name)
    {
        playerNames.Add(name);

        GameObject newRow = Instantiate(playerRowPrefab, container);
        activeRows.Add(name, newRow);

        newRow.transform.localScale = Vector3.zero; 
        newRow.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

        newRow.GetComponentInChildren<TMP_Text>().text = name;

        Button removeBtn = newRow.transform.Find("RemoveButton")?.GetComponent<Button>();
        if (removeBtn != null)
        {
            removeBtn.onClick.AddListener(() => RemovePlayer(name));
        }

        RefreshUI();
    }

    public void RemovePlayer(string name)
    {
        if (playerNames.Contains(name))
        {
            playerNames.Remove(name);
            GameManager.Instance.SyncFinalList(playerNames);

            GameObject rowToDestroy = activeRows[name];
            activeRows.Remove(name);

            if (rowToDestroy != null)
            {
                rowToDestroy.transform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => Destroy(rowToDestroy));
            }
        }
        RefreshUI();
    }

    private void RefreshUI()
    {
        // CONDITIONS POUR ACTIVER LE BOUTON :
        // 1. Il faut au moins 2 joueurs
        bool hasEnoughPlayers = (playerNames.Count >= 2);
        
        // 2. Il faut que les données Google Sheets soient chargées (si le loader existe)
        bool isDataLoaded = GoogleSheetLoader.Instance != null && GoogleSheetLoader.Instance.isDataLoaded;

        if (!isDataLoaded)
        {
            // Cas : Chargement en cours ou pas internet
            nextButton.interactable = false;
            if (nextButtonText != null) nextButtonText.text = "Chargement...";
        }
        else if (!hasEnoughPlayers)
        {
            // Cas : Pas assez de joueurs
            nextButton.interactable = false;
            if (nextButtonText != null) nextButtonText.text = originalButtonText; // Remet le texte normal
        }
        else
        {
            // Cas : Tout est bon !
            nextButton.interactable = true;
            if (nextButtonText != null) nextButtonText.text = originalButtonText;
        }
    }

    public void FinalizePlayers()
    {
        GameManager.Instance.SyncFinalList(playerNames);
        NavigationManager.Instance.OpenGameSelection();
        gameMenuManager.DisplayGames();
    }
}