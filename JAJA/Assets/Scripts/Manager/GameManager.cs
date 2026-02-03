using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Données de Session")]
    public List<string> playerNames = new List<string>();
    public string selectedGameMode;
    public string selectedDifficulty = "Aléatoire";
    public int questionCount = 20;
    public int maxPlayers = 10; // Assure-toi que c'est bien à 10

    [Header("UI Filtres")]
    public TMP_Text questionCountText;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Image arrowIcon;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPlayersFromDisk(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- SAUVEGARDE PERSISTANTE ---
    public void SavePlayersToDisk()
    {
        string joinedNames = string.Join("|", playerNames);
        PlayerPrefs.SetString("SavedPlayerNames", joinedNames);
        PlayerPrefs.Save();
    }

    private void LoadPlayersFromDisk()
    {
        if (PlayerPrefs.HasKey("SavedPlayerNames"))
        {
            string savedData = PlayerPrefs.GetString("SavedPlayerNames");
            if (!string.IsNullOrEmpty(savedData))
            {
                string[] names = savedData.Split('|');
                playerNames = new List<string>(names);
            }
        }
    }

    // Permet de synchroniser la liste d'un coup (utile pour suppression ou fin de saisie)
    public void SyncFinalList(List<string> newList)
    {
        playerNames = new List<string>(newList);
        SavePlayersToDisk();
    }

    // --- GESTION DES JOUEURS ---
    public void AddPlayer(string name)
    {
        if (playerNames.Count < maxPlayers && !string.IsNullOrEmpty(name))
        {
            if(!playerNames.Contains(name)) // Sécurité doublon
            {
                playerNames.Add(name);
                SavePlayersToDisk();
            }
        }
    }

    // --- LOGIQUE DE JEU & UI (Le reste du code est identique) ---
    public void SelectGame(string gameName)
    {
        selectedGameMode = gameName;
        UpdateDifficultyDropdown(gameName);
    }

    public void UpdateDifficultyDropdown(string gameName)
    {
        if (difficultyDropdown == null) return;
        difficultyDropdown.ClearOptions();
        List<string> optionsAAfficher;
        bool isUnique = false;

        // Listes internes pour éviter les erreurs de référence
        if (gameName == "Enchères" || gameName == "Culture G" || gameName == "Petit bac")
            optionsAAfficher = new List<string> { "Aléatoire", "Facile", "Moyen", "Difficile" };
        else if (gameName == "Mytho ou réalité" || gameName == "Qui est qui ?")
        {
            optionsAAfficher = new List<string> { "Unique" };
            isUnique = true;
        }
        else
            optionsAAfficher = new List<string> { "Aléatoire", "Facile", "Difficile", "Hot" };

        difficultyDropdown.AddOptions(optionsAAfficher);
        difficultyDropdown.value = 0;
        selectedDifficulty = optionsAAfficher[0];
        difficultyDropdown.RefreshShownValue();
        difficultyDropdown.interactable = !isUnique;
        if (arrowIcon != null) arrowIcon.enabled = !isUnique;
    }

    public void SetDifficulty(int index)
    {
        if (difficultyDropdown != null)
            selectedDifficulty = difficultyDropdown.options[index].text;
    }

    public void SetQuestionCount(float value)
    {
        int snappedValue = Mathf.RoundToInt(value) * 5;
        questionCount = snappedValue;
        if (questionCountText != null) questionCountText.text = snappedValue.ToString();
    }

    public string GetDifficultyMapping(string gameInsideMix)
    {
        if (selectedGameMode == "On mixe ?")
        {
            if (gameInsideMix == "Enchères" || gameInsideMix == "Culture G" || gameInsideMix == "Petit bac")
            {
                if (selectedDifficulty == "Difficile") return "Moyen";
                if (selectedDifficulty == "Hot") return "Difficile";
            }
        }
        return selectedDifficulty;
    }
}