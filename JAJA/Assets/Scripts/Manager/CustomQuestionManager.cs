using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;

public class CustomQuestionManager : MonoBehaviour
{
    [Header("Configuration Globale")]
    public TMP_Dropdown gameTypeDropdown;
    public GameObject mainFieldsContainer;

    [Header("Feedback UI")]
    public TMP_Text feedbackText;

    [Header("Conteneurs de Champs")]
    public GameObject groupSimple;
    public GameObject groupWithAnswer;
    public GameObject groupTwoOptions;
    public GameObject groupFourOptions;

    [Header("Inputs")]
    public TMP_InputField inputSimpleText;
    public TMP_InputField inputQuestionText, inputHiddenAnswer;
    public TMP_InputField inputPrefer1, inputPrefer2;
    public TMP_InputField inputWho1, inputWho2, inputWho3, inputWho4;

    [Header("Paramètres")]
    public TMP_Dropdown difficultyDropdown;
    public Slider sipsSlider;
    public TMP_Text sipsValueText;

    [Header("Liste & Prefabs")]
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
        if (string.IsNullOrEmpty(selected) || selected.Contains("Choisir"))
        {
            mainFieldsContainer.SetActive(false);
            return;
        }

        mainFieldsContainer.SetActive(true);
        groupSimple.SetActive(false); groupWithAnswer.SetActive(false);
        groupTwoOptions.SetActive(false); groupFourOptions.SetActive(false);

        string mode = selected.ToLower();
        if (mode.Contains("préfère")) groupTwoOptions.SetActive(true);
        else if (mode.Contains("qui est qui")) groupFourOptions.SetActive(true);
        else if (mode.Contains("culture") || mode.Contains("mytho")) groupWithAnswer.SetActive(true);
        else groupSimple.SetActive(true);
    }

    public void SaveNewQuestion()
    {
        if (!PremiumManager.Instance.IsUserPremium && customData.questions.Count >= PremiumManager.Instance.maxFreeCustomQuestions)
        {
            ShowPopFeedback("Limite atteinte (3) ! Passez Premium.", Color.red);
            return;
        }

        CustomQuestion newQ = new CustomQuestion {
            gameType = gameTypeDropdown.options[gameTypeDropdown.value].text,
            difficulty = difficultyDropdown.options[difficultyDropdown.value].text,
            sips = (int)sipsSlider.value
        };

        if (groupTwoOptions.activeSelf) newQ.text = $"{inputPrefer1.text} | {inputPrefer2.text}";
        else if (groupFourOptions.activeSelf) newQ.text = $"{inputWho1.text} | {inputWho2.text} | {inputWho3.text} | {inputWho4.text}";
        else if (groupWithAnswer.activeSelf) { newQ.text = inputQuestionText.text; newQ.option1 = inputHiddenAnswer.text; }
        else newQ.text = inputSimpleText.text;

        if (string.IsNullOrWhiteSpace(newQ.text) || newQ.text.Trim() == "|") return;

        customData.questions.Add(newQ);
        SaveToFile();
        GoogleSheetLoader.Instance?.LoadLocalCustomQuestions();
        
        ShowPopFeedback("Ajoutée !", Color.white);
        ClearAllFields();
        RefreshListView();
    }

    private void ShowPopFeedback(string message, Color color)
    {
        if (feedbackText == null) return;
        
        feedbackText.DOKill(); // Stop animations en cours
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.transform.localScale = Vector3.zero;

        feedbackText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).OnComplete(() => {
            feedbackText.transform.DOScale(0f, 0.3f).SetDelay(1.5f).OnComplete(() => {
                feedbackText.gameObject.SetActive(false);
            });
        });
    }

    private void ClearAllFields()
    {
        inputSimpleText.text = ""; inputQuestionText.text = ""; inputHiddenAnswer.text = "";
        inputPrefer1.text = ""; inputPrefer2.text = "";
        inputWho1.text = ""; inputWho2.text = ""; inputWho3.text = ""; inputWho4.text = "";
    }

    public void UpdateSipsDisplay(float value) => sipsValueText.text = Mathf.RoundToInt(value).ToString();
    private void SaveToFile() => File.WriteAllText(filePath, JsonUtility.ToJson(customData, true));
    private void LoadQuestions() { if (File.Exists(filePath)) { customData = JsonUtility.FromJson<CustomQuestionList>(File.ReadAllText(filePath)); RefreshListView(); } }

    public void RefreshListView()
    {
        foreach (Transform child in listContent) Destroy(child.gameObject);
        foreach (CustomQuestion q in customData.questions)
        {
            GameObject item = Instantiate(questionPrefab, listContent);
            TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>();
            foreach (var t in texts) {
                if (t.name == "QuestionText") t.text = q.text;
                if (t.name == "GameTypeText") t.text = $"{q.gameType} ({q.difficulty})";
            }
            item.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveQuestion(q));
        }
    }

    public void RemoveQuestion(CustomQuestion q) { customData.questions.Remove(q); SaveToFile(); RefreshListView(); }

    [System.Serializable] public class CustomQuestion { public string gameType, difficulty, text, option1; public int sips; }
    [System.Serializable] public class CustomQuestionList { public List<CustomQuestion> questions = new List<CustomQuestion>(); }
}