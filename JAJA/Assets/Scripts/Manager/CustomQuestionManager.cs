using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;

public class CustomQuestionManager : MonoBehaviour
{
    [Header("Inputs de Création")]
    public TMP_Dropdown gameTypeDropdown;
    public TMP_Dropdown difficultyDropdown;
    public Slider sipsSlider;
    public TMP_Text sipsValueText; // Glisse le texte qui affiche le nombre ici
    public TMP_InputField singleInputField; // Champ unique
    public TMP_InputField option1Field;      // Champ Tu préfères 1
    public TMP_InputField option2Field;      // Champ Tu préfères 2

    [Header("Conteneurs UI")]
    public GameObject gameChosenGroup;      // Groupe visuel champ unique
    public GameObject singleInputGroup;      // Groupe visuel champ unique
    public GameObject doubleInputGroup;      // Groupe visuel Tu préfères

    [Header("Historique")]
    public GameObject questionPrefab;
    public Transform listContent;

    private CustomQuestionList customData = new CustomQuestionList();
    private string filePath;

    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "custom_questions.json");
        LoadQuestions();
        UpdateUIForGameMode(); // Initialise l'affichage
    }

    // --- LOGIQUE D'AFFICHAGE DYNAMIQUE ---
    // Lie cette fonction à l'événement OnValueChanged de ton gameTypeDropdown
    public void UpdateUIForGameMode()
    {
        string selected = gameTypeDropdown.options[gameTypeDropdown.value].text;

        // 1. Si l'option sélectionnée est le titre par défaut, on cache tout
        if (selected == "Choix du jeu" || gameTypeDropdown.value == 0)
        {
            gameChosenGroup.SetActive(false);
            return; // On arrête la fonction ici
        }

        // 2. Sinon, on affiche le groupe principal et on ajuste les inputs
        gameChosenGroup.SetActive(true);

        bool isTuPreferes = selected.ToLower().Contains("préfère");
        singleInputGroup.SetActive(!isTuPreferes);
        doubleInputGroup.SetActive(isTuPreferes);

        Debug.Log("Mode sélectionné : " + selected);
    }


    // Appelle cette fonction via l'événement OnValueChanged de ton Slider
    public void UpdateSipsDisplay(float value)
    {
        // On arrondit la valeur pour ne pas avoir de virgules
        int sips = Mathf.RoundToInt(value);

        // On met à jour le texte (ex: "3")
        if (sipsValueText != null)
        {
            sipsValueText.text = "Gorgées : " + sips.ToString();
        }
    }
    // --- SAUVEGARDE ---
    public void SaveNewQuestion()
    {
        CustomQuestion newQ = new CustomQuestion();
        newQ.gameType = gameTypeDropdown.options[gameTypeDropdown.value].text;
        newQ.difficulty = difficultyDropdown.options[difficultyDropdown.value].text;
        newQ.sips = (int)sipsSlider.value;

        if (doubleInputGroup.activeSelf)
            newQ.text = option1Field.text + " | " + option2Field.text;
        else
            newQ.text = singleInputField.text;

        // Sécurité : ne pas enregistrer si vide
        if (string.IsNullOrEmpty(newQ.text) || newQ.text == " | ") return;

        customData.questions.Add(newQ);
        SaveToFile();

        if (GoogleSheetLoader.Instance != null)
        {
            GoogleSheetLoader.Instance.LoadLocalCustomQuestions();
        }
        // Reset des champs
        singleInputField.text = "";
        option1Field.text = "";
        option2Field.text = "";

        RefreshListView();
    }

    // --- GESTION DE LA LISTE (HISTORIQUE) ---
    public void RefreshListView()
    {
        foreach (Transform child in listContent) Destroy(child.gameObject);

        foreach (CustomQuestion q in customData.questions)
        {
            CustomQuestion currentQ = q; // Copie pour le bouton
            GameObject item = Instantiate(questionPrefab, listContent);

            // Assigne les textes dans le prefab (ajuste les noms selon tes enfants)
            TMP_Text[] allTexts = item.GetComponentsInChildren<TMP_Text>();
            foreach (var t in allTexts)
            {
                if (t.name == "QuestionText") t.text = currentQ.text;
                if (t.name == "GameTypeText") t.text = "Jeu : " + currentQ.gameType;
            }

            // Bouton supprimer
            Button delBtn = item.GetComponentInChildren<Button>();
            delBtn.onClick.AddListener(() => RemoveQuestion(currentQ));
        }
    }

    public void RemoveQuestion(CustomQuestion q)
    {
        customData.questions.Remove(q);
        SaveToFile();
        RefreshListView();
    }

    private void SaveToFile()
    {
        File.WriteAllText(filePath, JsonUtility.ToJson(customData, true));
    }

    private void LoadQuestions()
    {
        if (File.Exists(filePath))
        {
            customData = JsonUtility.FromJson<CustomQuestionList>(File.ReadAllText(filePath));
            RefreshListView();
        }
    }

    [System.Serializable]
    public class CustomQuestion
    {
        public string gameType; public string difficulty; public int sips; public string text;
    }
    [System.Serializable]
    public class CustomQuestionList
    {
        public List<CustomQuestion> questions = new List<CustomQuestion>();
    }
}