using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
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

    [Header("UI Filtres")]
    public TMP_Text questionCountText;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Image arrowIcon;

    [Header("Configurations des Difficultés")]
    // Groupe 1: Action ou Vérité, Je n'ai jamais, Le meilleur, Qui pourrait, Tu préfères, On mixe ?
    [SerializeField] private List<string> diffsStandard = new List<string> { "Aléatoire", "Facile", "Difficile", "Hot" };

    // Groupe 2: Enchères, Culture G, Petit bac
    [SerializeField] private List<string> diffsAvancees = new List<string> { "Aléatoire", "Facile", "Moyen", "Difficile" };

    // Groupe 3: Mytho ou réalité, Qui est qui ?
    [SerializeField] private List<string> diffsUnique = new List<string> { "Unique" };

    private void Awake()
    {
        Application.targetFrameRate = 60;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- ÉCRAN 1 : GESTION DES JOUEURS ---
    public void AddPlayer(string name)
    {
        // SettingsManager.Instance.TriggerVibration(); // Commenté si non présent
        if (playerNames.Count < 10 && !string.IsNullOrEmpty(name))
        {
            playerNames.Add(name);
            Debug.Log("Joueur ajouté : " + name);
        }
    }

    public void RemovePlayer(int index)
    {
        if (index >= 0 && index < playerNames.Count)
        {
            playerNames.RemoveAt(index);
        }
    }

    // --- ÉCRAN 2 : SÉLECTION DU JEU ---
    public void SelectGame(string gameName)
    {
        selectedGameMode = gameName;
        Debug.Log("Jeu sélectionné : " + gameName);
        UpdateDifficultyDropdown(gameName);
    }

    // --- ÉCRAN 3 : FILTRES ---
    public void UpdateDifficultyDropdown(string gameName)
    {
        if (difficultyDropdown == null) return;

        difficultyDropdown.ClearOptions();
        List<string> optionsAAfficher;
        bool isUnique = false;

        // Logique d'attribution des listes de difficultés
        if (gameName == "Enchères" || gameName == "Culture G" || gameName == "Petit bac")
        {
            optionsAAfficher = diffsAvancees;
        }
        else if (gameName == "Mytho ou réalité" || gameName == "Qui est qui ?")
        {
            optionsAAfficher = diffsUnique;
            isUnique = true;
        }
        else
        {
            // Par défaut pour Action/Vérité, Je n'ai jamais, etc. ET "On mixe ?"
            optionsAAfficher = diffsStandard;
        }

        difficultyDropdown.AddOptions(optionsAAfficher);

        difficultyDropdown.value = 0;
        selectedDifficulty = optionsAAfficher[0];
        difficultyDropdown.RefreshShownValue();

        difficultyDropdown.interactable = !isUnique;

        // On cache ou affiche la flèche
        if (arrowIcon != null)
        {
            arrowIcon.enabled = !isUnique;
        }
    }

    public void SetDifficulty(int index)
    {
        if (difficultyDropdown != null)
        {
            selectedDifficulty = difficultyDropdown.options[index].text;
            Debug.Log("Difficulté choisie : " + selectedDifficulty);
        }
    }

    public void SetQuestionCount(float value)
    {
        int snappedValue = Mathf.RoundToInt(value) * 5;
        questionCount = snappedValue;

        if (questionCountText != null)
        {
            questionCountText.text = snappedValue.ToString();
        }
    }

    // ASTUCE : Utilise cette fonction quand tu chargeras tes questions CSV
    public string GetDifficultyMapping(string gameInsideMix)
    {
        // Si on est dans "On mixe ?", on adapte la difficulté sélectionnée pour les jeux spécifiques
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