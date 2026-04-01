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
    public int maxPlayers = 10;

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
            if(!playerNames.Contains(name)) 
            {
                playerNames.Add(name);
                SavePlayersToDisk();
            }
        }
    }

    // --- LOGIQUE DE JEU & UI ---
    public void SelectGame(string gameKey)
    {
        selectedGameMode = gameKey;
        UpdateDifficultyDropdown(gameKey);
    }

    public void UpdateDifficultyDropdown(string gameKey)
    {
        if (difficultyDropdown == null) return;
        difficultyDropdown.ClearOptions();
        List<string> optionsAAfficher;
        bool isUnique = false;

        string lowerKey = gameKey.ToLower();

        if (lowerKey.Contains("enchère") || lowerKey.Contains("culture g") || lowerKey.Contains("petit bac"))
            optionsAAfficher = new List<string> { "Aléatoire", "Facile", "Moyen", "Difficile" };
        else if (lowerKey.Contains("mytho") || lowerKey.Contains("qui est qui"))
        {
            optionsAAfficher = new List<string> { "Unique" };
            isUnique = true;
        }
        else
            optionsAAfficher = new List<string> { "Aléatoire", "Facile", "Difficile", "Hot" };

        // Traduction des options dans le dropdown
        List<string> localizedOptions = new List<string>();
        foreach(string opt in optionsAAfficher) {
            localizedOptions.Add(LocalizationManager.Instance.GetText("diff_" + opt.ToLower()));
        }

        difficultyDropdown.AddOptions(localizedOptions);
        difficultyDropdown.value = 0;
        selectedDifficulty = optionsAAfficher[0];
        difficultyDropdown.RefreshShownValue();
        difficultyDropdown.interactable = !isUnique;

        if (arrowIcon != null) arrowIcon.enabled = !isUnique;
    }

    public void SetDifficulty(int index)
    {
        // On stocke la difficulté interne (pas la traduite) pour le tri
        string lowerKey = selectedGameMode.ToLower();
        List<string> optionsAAfficher;

        if (lowerKey.Contains("enchère") || lowerKey.Contains("culture g") || lowerKey.Contains("petit bac"))
            optionsAAfficher = new List<string> { "Aléatoire", "Facile", "Moyen", "Difficile" };
        else if (lowerKey.Contains("mytho") || lowerKey.Contains("qui est qui"))
            optionsAAfficher = new List<string> { "Unique" };
        else
            optionsAAfficher = new List<string> { "Aléatoire", "Facile", "Difficile", "Hot" };

        if (index >= 0 && index < optionsAAfficher.Count)
            selectedDifficulty = optionsAAfficher[index];
    }

    public void SetQuestionCount(float value)
    {
        int snappedValue = Mathf.RoundToInt(value) * 5;
        questionCount = snappedValue;
        if (questionCountText != null) questionCountText.text = snappedValue.ToString();
    }

    public string GetDifficultyMapping(string gameInsideMix)
    {
        string subGame = gameInsideMix.ToLower();
        if (selectedGameMode.ToLower().Contains("mixe"))
        {
            if (selectedDifficulty == "Hot")
            {
                if (subGame.Contains("culture") || subGame.Contains("enchère")) 
                    return "Difficile";
                if (subGame.Contains("qui est qui") || subGame.Contains("mytho")) 
                    return "Unique";
            }
            if (subGame.Contains("culture") || subGame.Contains("enchère") || subGame.Contains("petit bac"))
            {
                if (selectedDifficulty == "Difficile") return "Moyen";
            }
        }
        return selectedDifficulty;
    }
}