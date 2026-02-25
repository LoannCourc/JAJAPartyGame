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

        if (swipeCardController != null)
        {
            swipeCardController.ResetCardVisually();
        }

        DisplayQuestionData();
    }

    public void ReplayGame()
    {
        NavigationManager.Instance.ResetHistoryAndOpen(NavigationManager.Instance.gamePanel);
        LevelManager.Instance.PrepareGame();
    }

    public void ReturnToMenu()
    {
        NavigationManager.Instance.ResetHistoryAndOpen(NavigationManager.Instance.startMenu);
    }

    public void NextQuestion()
    {
        if (swipeCardController != null)
            swipeCardController.PerformFullSwipe(true);
    }

    public bool UpdateDataOnly()
    {
        currentIndex++;

        if (currentIndex < currentDeck.Count)
        {
            DisplayQuestionData();
            return true; 
        }
        else
        {
            NavigationManager.Instance.OpenEndMenu();
            return false; 
        }
    }

    private void DisplayQuestionData()
    {
        if (currentDeck == null || currentDeck.Count <= currentIndex) return;
        QuestionData q = currentDeck[currentIndex];

        UpdateDifficultyIcon(q.difficulty);
        
        // Utilisation de showPenalties (SettingsManager)
        if (SettingsManager.Instance != null && PenaltiesDisplay != null) 
            PenaltiesDisplay.gameObject.SetActive(SettingsManager.Instance.showPenalties);

        titleText.text = q.gameType;
        if (PenaltiesDisplay != null) PenaltiesDisplay.text = q.penalties; // Utilisation de q.penalties
        
        questionSlider.value = (float)(currentIndex + 1) / currentDeck.Count;

        revealButton.SetActive(false);
        extraInfoText.gameObject.SetActive(false);
        currentHiddenInfo = "";

        SetupTextByMode(q);
        UpdatePlayerTurn(q.gameType.ToLower(), q);
    }

    private void SetupTextByMode(QuestionData q)
    {
        string mode = q.gameType.ToLower();
        string cleanedQuestion = CleanText(q.text);

        if (mode.Contains("dilemme"))
        {
            questionText.text = $"{CleanText(q.option1)}\n<color=#780000><size=80%>— OU —</size></color>\n{CleanText(q.option2)}";
        }
        else if (mode.Contains("qui est qui"))
        {
            questionText.text = "<align=left>• " + cleanedQuestion.Replace("\n", "\n• ") + "</align>";
        }
        else if (mode.Contains("mytho"))
        {
            revealButton.SetActive(true);
            revealButton.GetComponentInChildren<TMP_Text>().text = "Afficher la réponse";
            questionText.text = cleanedQuestion;
            currentHiddenInfo = (Random.Range(0, 2) == 0) ? CleanText(q.option1) : "<color=#780000>Invente une réponse !</color>";
        }
        else if (mode.Contains("culture"))
        {
            revealButton.SetActive(true);
            revealButton.GetComponentInChildren<TMP_Text>().text = "Voir la réponse";
            questionText.text = cleanedQuestion;
        }
        else if (mode.Contains("bac"))
        {
            questionText.text = cleanedQuestion;
            revealButton.SetActive(true);
            revealButton.GetComponentInChildren<TMP_Text>().text = "Afficher la lettre";
            currentHiddenInfo = GetRandomLetter();
        }
        else
        {
            questionText.text = cleanedQuestion;
        }
    }

    public void OnClickReveal()
    {
        string mode = titleText.text.ToLower();
        TMP_Text btnText = revealButton.GetComponentInChildren<TMP_Text>();
        QuestionData q = currentDeck[currentIndex];

        if (mode.Contains("culture"))
        {
            if (btnText.text == "Voir la réponse")
            {
                questionText.text = "<color=#780000>RÉPONSE :</color>\n\n" + CleanText(q.option1);
                btnText.text = "Voir la question";
            }
            else
            {
                questionText.text = CleanText(q.text);
                btnText.text = "Voir la réponse";
            }
        }
        else if (mode.Contains("mytho"))
        {
            if (btnText.text == "Afficher la réponse")
            {
                questionText.text = currentHiddenInfo;
                btnText.text = "Voir la question";
            }
            else
            {
                questionText.text = CleanText(q.text);
                btnText.text = "Afficher la réponse";
            }
        }
        else if (mode.Contains("bac"))
        {
            currentHiddenInfo = GetRandomLetter();
            extraInfoText.gameObject.SetActive(true);
            extraInfoText.text = "<size=150%>" + currentHiddenInfo + "</size>";
            btnText.text = "Nouvelle lettre";
        }
    }

    private void UpdateDifficultyIcon(string difficulty)
    {
        if (difficultyIconImage == null) return;
        DifficultyIcon iconData = difficultyIcons.Find(x => x.difficultyName.Trim().ToLower() == difficulty.Trim().ToLower());

        if (iconData != null && iconData.iconSprite != null)
        {
            difficultyIconImage.sprite = iconData.iconSprite;
            difficultyIconImage.gameObject.SetActive(true);
        }
        else if (defaultIcon != null)
        {
            difficultyIconImage.sprite = defaultIcon;
            difficultyIconImage.gameObject.SetActive(true);
        }
        else difficultyIconImage.gameObject.SetActive(false);
    }

    private void UpdatePlayerTurn(string mode, QuestionData q)
    {
        if (mode.Contains("événement") || mode.Contains("évènement"))
        {
            playerText.text = "<color=#780000>" + CleanText(q.option1) + "</color>";
            playerText.gameObject.SetActive(true);
        }
        else if (mode.Contains("interrogatoire") || mode.Contains("culture") || mode.Contains("mytho"))
        {
            string p = (GameManager.Instance.playerNames != null && GameManager.Instance.playerNames.Count > 0)
                ? GameManager.Instance.playerNames[Random.Range(0, GameManager.Instance.playerNames.Count)]
                : "Tout le monde";

            playerText.text = "C'est au tour de : <color=#780000>" + p + "</color>";
            playerText.gameObject.SetActive(true);
        }
         else if (mode.Contains("qui est qui"))
        {
            string p = (GameManager.Instance.playerNames != null && GameManager.Instance.playerNames.Count > 0)
                ? GameManager.Instance.playerNames[Random.Range(0, GameManager.Instance.playerNames.Count)]
                : "Tout le monde";

            playerText.text = "À qui ressemble : \n<color=#780000>" + p + "</color>";
            playerText.gameObject.SetActive(true);
        }
        else playerText.gameObject.SetActive(false);
    }

    private string CleanText(string input) => string.IsNullOrEmpty(input) ? "" : input.Replace("\"\"", "\"");
    private string GetRandomLetter() { return "ABCDEFGHIJKLMNOPRST"[Random.Range(0, 19)].ToString(); }
}