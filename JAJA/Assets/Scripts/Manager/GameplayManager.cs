using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class DifficultyIcon
{
    public string difficultyName;
    public Sprite iconSprite;
}

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance;

    [Header("Configuration")]
    public SwipeCard swipeCardController;
    [SerializeField] private List<DifficultyIcon> difficultyIcons = new List<DifficultyIcon>();
    [SerializeField] private Sprite defaultIcon;

    [Header("UI References")]
    public TMP_Text titleText;
    [SerializeField] private TMP_Text penaltiesDisplay;
    public Image difficultyIconImage;
    public Slider questionSlider;
    public TMP_Text questionText;
    public TMP_Text playerText;
    public GameObject revealButton;
    public TMP_Text extraInfoText;

    private int currentIndex = 0;
    private List<QuestionData> currentDeck = new List<QuestionData>();
    private string currentHiddenInfo = "";

    public TMP_Text PenaltiesDisplay { get => penaltiesDisplay; set => penaltiesDisplay = value; }

    void Awake() { Instance = this; }

    public void StartGameSession(List<QuestionData> deck)
    {
        currentDeck = new List<QuestionData>(deck);
        currentIndex = 0;
        if (swipeCardController != null) swipeCardController.ResetCardVisually();
        DisplayQuestionData();
    }

    public void ReplayGame()
    {
        NavigationManager.Instance.ResetHistoryAndOpen(NavigationManager.Instance.gamePanel);
        LevelManager.Instance.PrepareGame();
    }

    public void ReturnToMenu() { NavigationManager.Instance.ResetHistoryAndOpen(NavigationManager.Instance.startMenu); }

    public void NextQuestion() { if (swipeCardController != null) swipeCardController.PerformFullSwipe(true); }

    public bool UpdateDataOnly()
    {
        currentIndex++;
        if (currentIndex < currentDeck.Count) { DisplayQuestionData(); return true; }
        else { NavigationManager.Instance.OpenEndMenu(); return false; }
    }

    public void RefreshCurrentQuestion() { DisplayQuestionData(); }

    public void DisplayQuestionData()
    {
        if (currentDeck == null || currentDeck.Count <= currentIndex) return;
        QuestionData q = currentDeck[currentIndex];

        UpdateDifficultyIcon(q.difficulty);

        if (SettingsManager.Instance != null && PenaltiesDisplay != null)
            PenaltiesDisplay.gameObject.SetActive(SettingsManager.Instance.showPenalties);

        string translatedTitle = LocalizationManager.Instance.GetText(q.gameType);
        titleText.text = translatedTitle.ToUpper();

        if (PenaltiesDisplay != null) PenaltiesDisplay.text = q.penalties;

        questionSlider.value = (float)(currentIndex + 1) / currentDeck.Count;

        revealButton.SetActive(false);
        extraInfoText.gameObject.SetActive(false);
        currentHiddenInfo = "";
        
        RefreshPenaltiesVisibility();
        SetupTextByMode(q);
        UpdatePlayerTurn(q.gameType.ToLower(), q);
    }

    private void SetupTextByMode(QuestionData q)
    {
        string mode = q.gameType.ToLower();
        string lang = LocalizationManager.Instance.currentLang;
        var loc = LocalizationManager.Instance;

        string questionTextVal = q.GetText(lang);
        string answerTextVal = q.GetAnswer(lang);

        if (mode.Contains("dilemme"))
        {
            string[] parts = questionTextVal.Split('|');
            string o1 = parts[0].Trim();
            string o2 = parts.Length > 1 ? parts[1].Trim() : "";
            questionText.text = $"{o1}\n<color=#780000><size=80%>— {loc.GetText("txt_ou")} —</size></color>\n{o2}";
        }
        else if (mode.Contains("game_qui") || mode.Contains("mytho") || mode.Contains("culture"))
        {
            if (mode.Contains("game_qui"))
            {
                // CORRECTION : On split par '|' et on aligne à gauche avec des puces
                string[] options = questionTextVal.Split('|');
                string formatted = "<align=left>";
                foreach (string opt in options)
                {
                    if (!string.IsNullOrWhiteSpace(opt))
                        formatted += "• " + opt.Trim() + "\n";
                }
                questionText.text = formatted + "</align>";
            }
            else
            {
                questionText.text = questionTextVal;
            }

            if (!string.IsNullOrEmpty(answerTextVal) || mode.Contains("mytho"))
            {
                revealButton.SetActive(true);
                revealButton.GetComponentInChildren<TMP_Text>().text = loc.GetText(mode.Contains("culture") ? "txt_voirreponse" : "txt_afficherreponse");

                if (mode.Contains("mytho"))
                    currentHiddenInfo = (Random.Range(0, 2) == 0) ? answerTextVal : $"<color=#780000>{loc.GetText("txt_inventereponse")}</color>";
                else
                    currentHiddenInfo = answerTextVal;
            }
        }
        else if (mode.Contains("bac"))
        {
            questionText.text = questionTextVal;
            revealButton.SetActive(true);
            revealButton.GetComponentInChildren<TMP_Text>().text = loc.GetText("txt_afficherlettre");
            currentHiddenInfo = GetRandomLetter();
        }
        else
        {
            questionText.text = questionTextVal;
        }
    }

    public void OnClickReveal()
    {
        TMP_Text btnText = revealButton.GetComponentInChildren<TMP_Text>();
        QuestionData q = currentDeck[currentIndex];
        string mode = q.gameType.ToLower();
        var loc = LocalizationManager.Instance;
        string lang = loc.currentLang;

        if (mode.Contains("culture") || mode.Contains("game_qui") || mode.Contains("mytho"))
        {
            string voirReponseTxt = loc.GetText(mode.Contains("culture") ? "txt_voirreponse" : "txt_afficherreponse");

            if (btnText.text == voirReponseTxt)
            {
                questionText.text = mode.Contains("culture") ? "<color=#780000>RÉPONSE :</color>\n\n" + currentHiddenInfo : currentHiddenInfo;
                btnText.text = loc.GetText("txt_voirquestion");
            }
            else
            {
                // RE-FORMATAGE lors du retour à la question pour "Qui est qui"
                if (mode.Contains("game_qui"))
                {
                    string[] options = q.GetText(lang).Split('|');
                    string formatted = "<align=left>";
                    foreach (string opt in options)
                    {
                        if (!string.IsNullOrWhiteSpace(opt))
                            formatted += "• " + opt.Trim() + "\n";
                    }
                    questionText.text = formatted + "</align>";
                }
                else
                {
                    questionText.text = q.GetText(lang);
                }
                btnText.text = voirReponseTxt;
            }
        }
        else if (mode.Contains("bac"))
        {
            currentHiddenInfo = GetRandomLetter();
            extraInfoText.gameObject.SetActive(true);
            extraInfoText.text = "<size=150%>" + currentHiddenInfo + "</size>";
            btnText.text = loc.GetText("txt_nouvellelettre");
        }
    }

    private void UpdateDifficultyIcon(string difficulty)
    {
        if (difficultyIconImage == null) return;
        DifficultyIcon icon = difficultyIcons.Find(x => x.difficultyName.ToLower() == difficulty.ToLower());
        difficultyIconImage.sprite = (icon != null) ? icon.iconSprite : defaultIcon;
    }

    private void UpdatePlayerTurn(string mode, QuestionData q)
    {
        var loc = LocalizationManager.Instance;
        string lang = loc.currentLang;

        if (mode.Contains("événement") || mode.Contains("évènement"))
        {
            playerText.text = "<color=#780000>" + q.GetAnswer(lang) + "</color>";
            playerText.gameObject.SetActive(true);
        }
        else if (mode.Contains("interrogatoire") || mode.Contains("culture") || mode.Contains("mytho") || mode.Contains("game_qui"))
        {
            string p = (GameManager.Instance.playerNames != null && GameManager.Instance.playerNames.Count > 0)
                ? GameManager.Instance.playerNames[Random.Range(0, GameManager.Instance.playerNames.Count)]
                : "Tout le monde";

            string prefix = mode.Contains("game_qui") ? loc.GetText("txt_aquiressemble") : loc.GetText("txt_autourde");
            playerText.text = $"{prefix} {(mode.Contains("game_qui") ? ": \n" : ": ")}<color=#780000>{p}</color>";
            playerText.gameObject.SetActive(true);
        }
        else playerText.gameObject.SetActive(false);
    }

    private string GetRandomLetter() { return "ABCDEFGHIJKLMNOPRST"[Random.Range(0, 19)].ToString(); }

    public void RefreshPenaltiesVisibility()
    {
        if (PenaltiesDisplay != null && SettingsManager.Instance != null)
        {
            PenaltiesDisplay.gameObject.SetActive(SettingsManager.Instance.showPenalties);
        }
    }
}