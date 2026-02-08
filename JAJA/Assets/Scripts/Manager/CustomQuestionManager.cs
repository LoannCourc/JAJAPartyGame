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
    public Image dropdownArrow; // --- AJOUT : Glisse l'image de la flèche du Dropdown ici ---
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
        
        gameTypeDropdown.onValueChanged.AddListener(delegate { UpdateUIForGameMode(); });

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

        groupSimple.SetActive(false); 
        groupWithAnswer.SetActive(false);
        groupTwoOptions.SetActive(false); 
        groupFourOptions.SetActive(false);

        string mode = selected.ToLower();

        if (mode.Contains("préfère")) groupTwoOptions.SetActive(true);
        else if (mode.Contains("qui est qui")) groupFourOptions.SetActive(true);
        else if (mode.Contains("culture") || mode.Contains("mytho")) groupWithAnswer.SetActive(true);
        else groupSimple.SetActive(true);

        UpdateDifficultyOptions(selected);
    }

    private void UpdateDifficultyOptions(string gameName)
    {
        if (difficultyDropdown == null) return;

        difficultyDropdown.ClearOptions();
        List<string> options = new List<string>();
        string lowerName = gameName.ToLower().Trim();
        
        // Variable pour savoir si on doit verrouiller le menu
        bool isUnique = false;

        // CAS 1 : Culture G, Enchères
        if (lowerName.Contains("culture") || lowerName.Contains("enchères"))
        {
            options.Add("Facile");
            options.Add("Moyen");
            options.Add("Difficile");
        }
        // CAS 2 : Qui est qui, Mytho => UNIQUE (Verrouillé)
        else if (lowerName.Contains("qui est qui") || lowerName.Contains("mytho"))
        {
            options.Add("Unique");
            isUnique = true; // On marque comme unique
        }
        // CAS 3 : Autres (Action/Vérité, Petit Bac, Mixe...)
        else
        {
            options.Add("Facile");
            options.Add("Difficile");
            options.Add("Hot");
        }

        difficultyDropdown.AddOptions(options);
        difficultyDropdown.value = 0; 
        difficultyDropdown.RefreshShownValue();

        // --- GESTION DU VERROUILLAGE ---
        // Si c'est unique, on rend le dropdown non-cliquable
        difficultyDropdown.interactable = !isUnique;

        // Si c'est unique, on cache la flèche
        if (dropdownArrow != null)
        {
            dropdownArrow.enabled = !isUnique;
        }
    }

    // ... Le reste du script (SaveNewQuestion, etc.) reste identique ...
    
    public void SaveNewQuestion()
    {
        if (PremiumManager.Instance != null && !PremiumManager.Instance.IsUserPremium && customData.questions.Count >= PremiumManager.Instance.maxFreeCustomQuestions)
        {
            ShowPopFeedback("Limite atteinte (Version Gratuite)", Color.red);
            return;
        }

        if (IsInputEmpty())
        {
            ShowPopFeedback("Remplissez les champs !", Color.yellow);
            return;
        }

        CustomQuestion newQ = new CustomQuestion
        {
            gameType = gameTypeDropdown.options[gameTypeDropdown.value].text,
            difficulty = difficultyDropdown.options[difficultyDropdown.value].text,
            sips = (int)sipsSlider.value
        };

        if (groupTwoOptions.activeSelf) 
            newQ.text = $"{inputPrefer1.text} | {inputPrefer2.text}";
        else if (groupFourOptions.activeSelf) 
            newQ.text = $"{inputWho1.text} | {inputWho2.text} | {inputWho3.text} | {inputWho4.text}";
        else if (groupWithAnswer.activeSelf) 
        { 
            newQ.text = inputQuestionText.text; 
            newQ.option1 = inputHiddenAnswer.text; 
        }
        else 
            newQ.text = inputSimpleText.text;

        newQ.difficulty += " (custom)";

        customData.questions.Add(newQ);
        SaveToFile();
        
        if(GoogleSheetLoader.Instance != null) 
            GoogleSheetLoader.Instance.LoadLocalCustomQuestions();

        ShowPopFeedback("Question ajoutée !", Color.green);
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
            feedbackText.transform.DOScale(0f, 0.3f).SetDelay(1.5f).OnComplete(() =>
            {
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

    public void UpdateSipsDisplay(float value)
    {
        int sips = Mathf.RoundToInt(value);
        string label = sips > 1 ? "Gorgées : " : "Gorgée : ";
        sipsValueText.text = label + sips;

        sipsValueText.transform.DOKill();
        sipsValueText.transform.localScale = Vector3.one; 
        sipsValueText.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.15f, 10, 1);
    }

    private void SaveToFile() => File.WriteAllText(filePath, JsonUtility.ToJson(customData, true));
    
    private void LoadQuestions() 
    { 
        if (File.Exists(filePath)) 
        { 
            try {
                customData = JsonUtility.FromJson<CustomQuestionList>(File.ReadAllText(filePath)); 
                RefreshListView(); 
            }
            catch { Debug.LogWarning("Fichier JSON corrompu ou vide"); }
        } 
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
                if (t.name == "QuestionText" || t.name.Contains("Content")) t.text = q.text.Replace("|", " / ");
                if (t.name == "GameTypeText" || t.name.Contains("Label")) t.text = $"{q.gameType} - {q.difficulty.Replace(" (custom)", "")}";
            }
            item.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveQuestion(q));
        }
    }

    public void RemoveQuestion(CustomQuestion q) 
    { 
        customData.questions.Remove(q); 
        SaveToFile(); 
        if(GoogleSheetLoader.Instance != null) GoogleSheetLoader.Instance.LoadLocalCustomQuestions();
        RefreshListView(); 
    }

    [System.Serializable] public class CustomQuestion { public string gameType, difficulty, text, option1; public int sips; }
    [System.Serializable] public class CustomQuestionList { public List<CustomQuestion> questions = new List<CustomQuestion>(); }
}