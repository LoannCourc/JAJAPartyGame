using UnityEngine;
using TMPro;
using System.IO;
using System.Collections;
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
    public Image dropdownArrow; 
    public Slider penaltySlider; 
    public TMP_Text penaltyValueText;

    [Header("Liste & Prefabs")]
    public GameObject questionPrefab;
    public Transform listContent;

    // --- LISTE DES JEUX (game_mixed en premier pour être par défaut) ---
    private List<string> gameKeys = new List<string> { 
        "game_mixed", "game_dilemme", "game_culture", "game_mytho", 
        "game_enchere", "game_bac", "game_meilleur", "game_capable", 
        "game_dejafait", "game_interrogatoire", "game_qui" 
    };

    private List<string> currentDiffKeys = new List<string>();

    [System.Serializable]
    public class CustomQuestion
    {
        public string gameType;
        public string text;
        public int penalties; 
        public string difficulty;
    }

    [System.Serializable]
    public class CustomQuestionList { public List<CustomQuestion> questions = new List<CustomQuestion>(); }

    private CustomQuestionList customData = new CustomQuestionList();
    private string filePath;
    private bool isInitialized = false;

    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "custom_questions.json");
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        
        // Sécurité : on attend que les traductions soient prêtes
        StartCoroutine(InitWhenDataReady());
    }

    private IEnumerator InitWhenDataReady()
    {
        // Attente que le loader ait fini ET que le dictionnaire soit peuplé
        while (GoogleSheetLoader.Instance == null || !GoogleSheetLoader.Instance.isDataLoaded || 
               LocalizationManager.Instance.GetText("game_mixed") == "game_mixed")
        {
            yield return new WaitForSeconds(0.1f); 
        }

        LoadQuestions();
        SetupGameTypeDropdown();
        UpdatePenaltyDisplay(1);
        gameTypeDropdown.onValueChanged.AddListener(delegate { UpdateUIForGameMode(); });
        
        isInitialized = true;
    }

    private void SetupGameTypeDropdown()
    {
        gameTypeDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        for (int i = 0; i < gameKeys.Count; i++)
        {
            string translatedName = LocalizationManager.Instance.GetText(gameKeys[i]);
            options.Add(new TMP_Dropdown.OptionData(translatedName));
        }

        gameTypeDropdown.AddOptions(options);

        // Sélection par défaut sur game_mixed (index 0)
        gameTypeDropdown.value = 0;
        gameTypeDropdown.RefreshShownValue();
        
        UpdateUIForGameMode();
    }

    public void UpdateUIForGameMode()
    {
        if (gameTypeDropdown.options.Count == 0) return;

        string selectedKey = gameKeys[gameTypeDropdown.value];
        
        mainFieldsContainer.SetActive(true);
        groupSimple.SetActive(false); 
        groupWithAnswer.SetActive(false);
        groupTwoOptions.SetActive(false); 
        groupFourOptions.SetActive(false);

        // Logique de détection des groupes de champs selon la clé
        if (selectedKey == "game_dilemme") 
        {
            groupTwoOptions.SetActive(true);
        }
        else if (selectedKey == "game_qui") 
        {
            groupFourOptions.SetActive(true);
        }
        else if (selectedKey == "game_culture" || selectedKey == "game_mytho") 
        {
            groupWithAnswer.SetActive(true);
        }
        else 
        {
            groupSimple.SetActive(true);
        }

        UpdateDifficultyOptions(selectedKey);
    }

    private void UpdateDifficultyOptions(string gameKey)
    {
        if (difficultyDropdown == null) return;
        difficultyDropdown.ClearOptions();
        currentDiffKeys.Clear();
        
        bool isUnique = false;

        if (gameKey == "game_culture" || gameKey == "game_enchere")
            currentDiffKeys.AddRange(new[] { "diff_facile", "diff_moyen", "diff_difficile" }); 
        else if (gameKey == "game_qui" || gameKey == "game_mytho")
            { currentDiffKeys.Add("diff_unique"); isUnique = true; }
        else
            currentDiffKeys.AddRange(new[] { "diff_facile", "diff_difficile", "diff_hot" });

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach(string dk in currentDiffKeys) 
        {
            options.Add(new TMP_Dropdown.OptionData(LocalizationManager.Instance.GetText(dk)));
        }

        difficultyDropdown.AddOptions(options);
        difficultyDropdown.value = 0; 
        difficultyDropdown.RefreshShownValue();
        difficultyDropdown.interactable = !isUnique;
        if (dropdownArrow != null) dropdownArrow.enabled = !isUnique;
    }

    public void SaveNewQuestion()
    {
        var loc = LocalizationManager.Instance;
        Color myCustomColor;
        ColorUtility.TryParseHtmlString("#FDF0D5", out myCustomColor);
        
        if (PremiumManager.Instance != null && !PremiumManager.Instance.IsUserPremium && customData.questions.Count >= PremiumManager.Instance.maxFreeCustomQuestions)
        { 
            ShowPopFeedback(loc.GetText("txt_limite"), Color.red); 
            return; 
        }

        if (IsInputEmpty()) { ShowPopFeedback(loc.GetText("txt_ajouter"), myCustomColor); return; }

        string selectedGameKey = gameKeys[gameTypeDropdown.value];
        string selectedDiffKey = currentDiffKeys[difficultyDropdown.value];

        CustomQuestion newQ = new CustomQuestion {
            gameType = selectedGameKey,
            difficulty = selectedDiffKey,
            penalties = (int)penaltySlider.value
        };

        // Formatage avec le séparateur "|"
        if (groupTwoOptions.activeSelf) 
            newQ.text = $"{inputPrefer1.text} | {inputPrefer2.text}";
        else if (groupFourOptions.activeSelf) 
            newQ.text = $"{inputWho1.text} | {inputWho2.text} | {inputWho3.text} | {inputWho4.text}";
        else if (groupWithAnswer.activeSelf) 
            newQ.text = $"{inputQuestionText.text} | {inputHiddenAnswer.text}"; 
        else 
            newQ.text = inputSimpleText.text;

        customData.questions.Add(newQ);
        SaveToFile();
        
        if(GoogleSheetLoader.Instance != null) GoogleSheetLoader.Instance.LoadLocalCustomQuestions();

        ShowPopFeedback(loc.GetText("txt_ajouter"), myCustomColor);
        ClearAllFields();
        RefreshListView();
    }

    private bool IsInputEmpty()
    {
        if (groupSimple.activeSelf) return string.IsNullOrWhiteSpace(inputSimpleText.text);
        if (groupWithAnswer.activeSelf) return string.IsNullOrWhiteSpace(inputQuestionText.text);
        if (groupTwoOptions.activeSelf) return string.IsNullOrWhiteSpace(inputPrefer1.text) || string.IsNullOrWhiteSpace(inputPrefer2.text);
        if (groupFourOptions.activeSelf) return string.IsNullOrWhiteSpace(inputWho1.text);
        return true;
    }

    public void UpdatePenaltyDisplay(float value)
    {
        int p = Mathf.RoundToInt(value);
        penaltyValueText.text = LocalizationManager.Instance.GetText("txt_numbermalus") + " : " + p;
        penaltyValueText.transform.DOKill();
        penaltyValueText.transform.localScale = Vector3.one; 
        penaltyValueText.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.15f, 10, 1);
    }

    private void ShowPopFeedback(string message, Color color)
    {
        if (feedbackText == null) return;
        feedbackText.DOKill();
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.transform.localScale = Vector3.zero;
        feedbackText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            feedbackText.transform.DOScale(0f, 0.3f).SetDelay(1.5f).OnComplete(() => feedbackText.gameObject.SetActive(false));
        });
    }

    private void ClearAllFields() 
    {
        inputSimpleText.text = ""; inputQuestionText.text = ""; inputHiddenAnswer.text = "";
        inputPrefer1.text = ""; inputPrefer2.text = "";
        inputWho1.text = ""; inputWho2.text = ""; inputWho3.text = ""; inputWho4.text = "";
    }

    private void SaveToFile() => File.WriteAllText(filePath, JsonUtility.ToJson(customData, true));
    
    private void LoadQuestions() 
    { 
        if (File.Exists(filePath)) 
            customData = JsonUtility.FromJson<CustomQuestionList>(File.ReadAllText(filePath)); 
        RefreshListView(); 
    }
    
    public void RefreshListView()
    {
        foreach (Transform child in listContent) Destroy(child.gameObject);
        foreach (CustomQuestion q in customData.questions)
        {
            GameObject item = Instantiate(questionPrefab, listContent);
            TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>();
            foreach (var t in texts)
            {
                if (t.name == "QuestionText" || t.name.Contains("Content")) 
                    t.text = q.text.Replace("|", " / ");

                if (t.name == "GameTypeText" || t.name.Contains("Label")) 
                {
                    string tGame = LocalizationManager.Instance.GetText(q.gameType);
                    string tDiff = LocalizationManager.Instance.GetText(q.difficulty);
                    t.text = $"{tGame} - {tDiff}";
                }
            }
            item.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveQuestion(q));
        }
    }

    public void RemoveQuestion(CustomQuestion q) { customData.questions.Remove(q); SaveToFile(); RefreshListView(); }
}