using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class CustomQuestionManager : MonoBehaviour
{
    [Header("Configuration Globale")]
    public TMP_Dropdown gameTypeDropdown;
    public GameObject mainFieldsContainer;

    [Header("Feedback")]
    public TMP_Text feedbackText; // Texte qui affiche "Question enregistrée !"

    [Header("Conteneur 1: Simple (Action, Vérité, On Mixe...)")]
    public GameObject groupSimple;
    public TMP_InputField inputSimpleText;

    [Header("Conteneur 2: Avec Réponse (Culture G, Mytho)")]
    public GameObject groupWithAnswer;
    public TMP_InputField inputQuestionText;
    public TMP_InputField inputHiddenAnswer;

    [Header("Conteneur 3: Tu préfères (2 Options)")]
    public GameObject groupTwoOptions;
    public TMP_InputField inputPrefer1;
    public TMP_InputField inputPrefer2;

    [Header("Conteneur 4: Qui est qui (4 Options)")]
    public GameObject groupFourOptions;
    public TMP_InputField inputWho1, inputWho2, inputWho3, inputWho4;

    [Header("Paramètres Communs")]
    public TMP_Dropdown difficultyDropdown;
    public Slider sipsSlider;
    public TMP_Text sipsValueText;

    [Header("Historique & Prefabs")]
    public GameObject questionPrefab;
    public Transform listContent;

    private CustomQuestionList customData = new CustomQuestionList();
    private string filePath;

    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "custom_questions.json");
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        LoadQuestions();
        UpdateUIForGameMode();
    }

    public void UpdateUIForGameMode()
    {
        string selected = gameTypeDropdown.options[gameTypeDropdown.value].text;
        string mode = selected.ToLower();

        // On ne cache que si l'option est un placeholder vide (ex: "-- Choisir --")
        // Si ton premier jeu est "On Mixe", assure-toi que son index n'est pas bloqué
        if (string.IsNullOrEmpty(selected) || selected.Contains("Choisir"))
        {
            mainFieldsContainer.SetActive(false);
            return;
        }

        mainFieldsContainer.SetActive(true);
        groupSimple.SetActive(false);
        groupWithAnswer.SetActive(false);
        groupTwoOptions.SetActive(false);
        groupFourOptions.SetActive(false);

        if (mode.Contains("préfère")) groupTwoOptions.SetActive(true);
        else if (mode.Contains("qui est qui")) groupFourOptions.SetActive(true);
        else if (mode.Contains("culture") || mode.Contains("mytho")) groupWithAnswer.SetActive(true);
        else groupSimple.SetActive(true);
    }

    public void SaveNewQuestion()
    {
        string selectedGame = gameTypeDropdown.options[gameTypeDropdown.value].text;
        string selectedDiff = difficultyDropdown.options[difficultyDropdown.value].text;

        CustomQuestion newQ = new CustomQuestion();
        newQ.gameType = selectedGame;
        newQ.difficulty = selectedDiff; // SAUVEGARDE DE LA DIFFICULTÉ
        newQ.sips = (int)sipsSlider.value;

        if (groupTwoOptions.activeSelf)
            newQ.text = inputPrefer1.text + " | " + inputPrefer2.text;
        else if (groupFourOptions.activeSelf)
            newQ.text = inputWho1.text + " | " + inputWho2.text + " | " + inputWho3.text + " | " + inputWho4.text;
        else if (groupWithAnswer.activeSelf)
        {
            newQ.text = inputQuestionText.text;
            newQ.option1 = inputHiddenAnswer.text;
        }
        else
            newQ.text = inputSimpleText.text;

        if (string.IsNullOrWhiteSpace(newQ.text) || newQ.text.Trim() == "|") return;

        customData.questions.Add(newQ);
        SaveToFile();

        if (GoogleSheetLoader.Instance != null)
            GoogleSheetLoader.Instance.LoadLocalCustomQuestions();

        ShowPopFeedback();
        ClearAllFields();
        RefreshListView();
    }

    private void ShowPopFeedback()
{
    if (feedbackText != null)
    {
        // 1. Reset l'état
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = "ajoutée !";

        // 2. Animation de POP
        // On fait grossir le texte au delà de sa taille puis on revient à 1 (effet rebond)
        feedbackText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).OnComplete(() => {
            
            // 3. Attente et disparition
            // On attend 1.5 seconde puis on réduit à zéro
            feedbackText.transform.DOScale(0f, 0.3f).SetDelay(1.5f).OnComplete(() => {
                feedbackText.gameObject.SetActive(false);
            });
        });
    }
}
    private void ClearAllFields()
    {
        inputSimpleText.text = ""; inputQuestionText.text = ""; inputHiddenAnswer.text = "";
        inputPrefer1.text = ""; inputPrefer2.text = "";
        inputWho1.text = ""; inputWho2.text = ""; inputWho3.text = ""; inputWho4.text = "";
    }

    // --- LOGIQUE TECHNIQUE (SIPS, FILE, LIST) ---
    public void UpdateSipsDisplay(float value)
    {
        if (sipsValueText != null) sipsValueText.text = Mathf.RoundToInt(value).ToString();
    }

    private void SaveToFile() => File.WriteAllText(filePath, JsonUtility.ToJson(customData, true));

    private void LoadQuestions()
    {
        if (File.Exists(filePath))
        {
            customData = JsonUtility.FromJson<CustomQuestionList>(File.ReadAllText(filePath));
            RefreshListView();
        }
    }

    public void RefreshListView()
    {
        foreach (Transform child in listContent) Destroy(child.gameObject);
        foreach (CustomQuestion q in customData.questions)
        {
            CustomQuestion currentQ = q;
            GameObject item = Instantiate(questionPrefab, listContent);
            TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>();
            foreach (var t in texts)
            {
                if (t.name == "QuestionText") t.text = currentQ.text;
                if (t.name == "GameTypeText") t.text = currentQ.gameType + " (" + currentQ.difficulty + ")";
            }
            item.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveQuestion(currentQ));
        }
    }

    public void RemoveQuestion(CustomQuestion q)
    {
        customData.questions.Remove(q);
        SaveToFile();
        RefreshListView();
    }

    [System.Serializable]
    public class CustomQuestion
    {
        public string gameType;
        public string difficulty;
        public int sips;
        public string text;
        public string option1;
    }

    [System.Serializable]
    public class CustomQuestionList
    {
        public List<CustomQuestion> questions = new List<CustomQuestion>();
    }
}