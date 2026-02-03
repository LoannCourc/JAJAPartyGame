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

    public GameMenuManager gameMenuManager;

    private List<string> playerNames = new List<string>();
    private Dictionary<string, GameObject> activeRows = new Dictionary<string, GameObject>();
    private const int MAX_PLAYERS = 10;

    void Start()
    {
        // 1. Nettoyage visuel au lancement
        foreach (Transform child in container) Destroy(child.gameObject);
        activeRows.Clear();
        playerNames.Clear();

        // 2. CHARGEMENT DES DONNÉES SAUVEGARDÉES
        if (GameManager.Instance != null && GameManager.Instance.playerNames.Count > 0)
        {
            foreach (string name in GameManager.Instance.playerNames)
            {
                AddPlayerVisually(name);
            }
        }

        mainInputField.text = "";
        RefreshUI();

        // --- LISTENERS ---
        addBtn.onClick.RemoveAllListeners();
        addBtn.onClick.AddListener(AddPlayerFromInput);

        // AJOUT : Écoute de la touche "Entrée" du clavier
        mainInputField.onSubmit.RemoveAllListeners();
        mainInputField.onSubmit.AddListener((val) => AddPlayerFromInput());
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
        
        // On garde le focus sur l'input pour pouvoir enchaîner les joueurs rapidement
        mainInputField.ActivateInputField(); 
    }

    // Version surchargée pour l'évenement onSubmit si nécessaire (certaines versions de TMP demandent le string)
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
        nextButton.interactable = (playerNames.Count >= 2);
    }

    public void FinalizePlayers()
    {
        GameManager.Instance.SyncFinalList(playerNames);
        NavigationManager.Instance.OpenGameSelection();
        gameMenuManager.DisplayGames();
    }
}