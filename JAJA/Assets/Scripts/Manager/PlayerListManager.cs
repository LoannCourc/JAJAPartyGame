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
            // On récupère la liste du GameManager et on recrée les lignes
            foreach (string name in GameManager.Instance.playerNames)
            {
                AddPlayerVisually(name);
            }
        }

        mainInputField.text = "";
        RefreshUI();

        addBtn.onClick.RemoveAllListeners();
        addBtn.onClick.AddListener(AddPlayerFromInput);
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

        // On ajoute au visuel ET on demande au GameManager de sauvegarder
        AddPlayerVisually(nameToAdd);
        GameManager.Instance.AddPlayer(nameToAdd); 

        mainInputField.text = ""; 
        mainInputField.ActivateInputField(); 
    }

    // Cette fonction s'occupe uniquement de la partie visuelle (Prefab + Animation)
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
            
            // On prévient le GameManager pour qu'il mette à jour le PlayerPrefs
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
        // Synchronisation finale avant de lancer le jeu
        GameManager.Instance.SyncFinalList(playerNames);
        NavigationManager.Instance.OpenGameSelection();
        gameMenuManager.DisplayGames();
    }
}