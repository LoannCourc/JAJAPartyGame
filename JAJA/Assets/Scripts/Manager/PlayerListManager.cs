using UnityEngine;
using UnityEngine.UI;
using TMP_InputField = TMPro.TMP_InputField; // Sécurité pour TMP
using TMP_Text = TMPro.TMP_Text;
using DG.Tweening; // Importation obligatoire
using System.Collections.Generic;

public class PlayerListManager : MonoBehaviour
{
    [Header("Saisie Fixe")]
    public TMP_InputField mainInputField; // L'unique champ qui ne bouge pas
    public Button addBtn; // Ton bouton avec le "+"

    [Header("Liste Dynamique")]
    public GameObject playerRowPrefab; // Le prefab beige avec le texte et le bouton "-"
    public Transform container; // Le "Content" de ta ScrollView
    public Button nextButton;

    [Header("Navigation")]
    public GameObject startMenu;
    public GameObject gameSelectionMenu;
    public GameMenuManager gameMenuManager;

    private List<string> playerNames = new List<string>();
    private Dictionary<string, GameObject> activeRows = new Dictionary<string, GameObject>();
    private const int MAX_PLAYERS = 10;

    void Start()
    {
        // Nettoyage au lancement
        foreach (Transform child in container) Destroy(child.gameObject);

        mainInputField.text = "";
        RefreshUI();

        // Liaison du bouton "+" principal
        addBtn.onClick.RemoveAllListeners();
        addBtn.onClick.AddListener(AddPlayerFromInput);
    }

    // Fonction déclenchée par le bouton "+" principal
    public void AddPlayerFromInput()
    {
        string nameToAdd = mainInputField.text.Trim();

        if (string.IsNullOrEmpty(nameToAdd) || playerNames.Contains(nameToAdd))
        {
            // Petit tremblement horizontal (Shake)
            mainInputField.transform.DOShakePosition(0.4f, strength: new Vector3(10, 0, 0), vibrato: 10);
            return;
        }

        if (string.IsNullOrEmpty(nameToAdd)) return;
        if (playerNames.Count >= MAX_PLAYERS) return;
        if (playerNames.Contains(nameToAdd)) return; // Évite les doublons

        AddPlayer(nameToAdd);

        mainInputField.text = ""; // Vide le champ
        mainInputField.ActivateInputField(); // Garde le focus pour taper le suivant
    }

    private void AddPlayer(string name)
    {
        playerNames.Add(name);

        GameObject newRow = Instantiate(playerRowPrefab, container);
        activeRows.Add(name, newRow);

        // --- ANIMATION D'APPARITION ---
        newRow.transform.localScale = Vector3.zero; // Sécurité : repart de zéro
        newRow.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        // ------------------------------

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

            GameObject rowToDestroy = activeRows[name];

            // On retire l'entrée du dictionnaire avant de lancer l'anim
            activeRows.Remove(name);

            // --- ANIMATION DE SORTIE ---
            if (rowToDestroy != null)
            {
                rowToDestroy.transform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => Destroy(rowToDestroy));
            }
            // ---------------------------
        }
        RefreshUI();
    }

    private void RefreshUI()
    {


        // Le bouton "Suivant" s'active si au moins 2 joueurs sont dans la liste
        nextButton.interactable = (playerNames.Count >= 2);
    }

    public void FinalizePlayers()
    {
        GameManager.Instance.playerNames.Clear();
        foreach (string name in playerNames)
        {
            GameManager.Instance.AddPlayer(name);
        }

        startMenu.SetActive(false);
        gameSelectionMenu.SetActive(true);
        gameMenuManager.DisplayGames();
    }
}