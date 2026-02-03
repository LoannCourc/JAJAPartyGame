using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class DifficultyIcon {
    public string difficultyName;
    public Sprite iconSprite;
}

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance;

    public SwipeCard swipeCardController; // À glisser dans l'inspecteur

    [Header("UI References")]
    public TMP_Text titleText;
    public TMP_Text sipsDisplay;
    public Image difficultyIconImage;
    public Slider questionSlider;
    public TMP_Text questionText;
    public TMP_Text playerText;
    public GameObject revealButton;
    public TMP_Text extraInfoText;

    [SerializeField] private List<DifficultyIcon> difficultyIcons = new List<DifficultyIcon>();
    [SerializeField] private Sprite defaultIcon;

    private int currentIndex = 0;
    private List<QuestionData> currentDeck = new List<QuestionData>();
    private string currentHiddenInfo = "";

    void Awake() { Instance = this; }

    public void StartGameSession(List<QuestionData> deck)
    {
        currentDeck = new List<QuestionData>(deck);
        currentIndex = 0;
        DisplayQuestionData();
    }

    // --- CETTE FONCTION EST LIÉE AU BOUTON "SUIVANT" ---
    public void NextQuestion()
    {
        if (currentIndex + 1 < currentDeck.Count)
        {
            // On demande simplement au script de swipe de jouer son animation
            swipeCardController.PerformFullSwipe(true);
        }
        else
        {
            NavigationManager.Instance.OpenEndMenu();
        }
    }

    // --- APPELÉ PAR SWIPECARD UNE FOIS LA CARTE SORTIE ---
    public void UpdateDataOnly()
    {
        currentIndex++;
        DisplayQuestionData();
    }

    private void DisplayQuestionData()
    {
        if (currentDeck == null || currentDeck.Count <= currentIndex) return;
        QuestionData q = currentDeck[currentIndex];
        
        // Mise à jour visuelle classique
        UpdateDifficultyIcon(q.difficulty);
        if (SettingsManager.Instance != null) sipsDisplay.gameObject.SetActive(SettingsManager.Instance.showSips);
        
        titleText.text = q.gameType;
        sipsDisplay.text = q.sips;
        questionSlider.value = (float)(currentIndex + 1) / currentDeck.Count;
        revealButton.SetActive(false);
        extraInfoText.gameObject.SetActive(false);
        
        SetupTextByMode(q);
        UpdatePlayerTurn(q.gameType.ToLower(), q);
    }

    // --- COPIE TES AUTRES MÉTHODES ICI (SetupTextByMode, CleanText, etc.) ---
    private void SetupTextByMode(QuestionData q) {
        string mode = q.gameType.ToLower();
        string cleanedQuestion = CleanText(q.text);
        if (mode.Contains("préfère")) questionText.text = $"{CleanText(q.option1)}\n<color=#780000><size=80%>— OU —</size></color>\n{CleanText(q.option2)}";
        else if (mode.Contains("qui est qui")) questionText.text = "<align=left>• " + cleanedQuestion.Replace("\n", "\n• ") + "</align>";
        else if (mode.Contains("mytho")) {
            revealButton.SetActive(true); questionText.text = cleanedQuestion;
            currentHiddenInfo = (Random.Range(0, 2) == 0) ? CleanText(q.option1) : "<color=#780000>Invente une réponse !</color>";
        } else if (mode.Contains("culture")) {
            revealButton.SetActive(true); questionText.text = cleanedQuestion;
        } else if (mode.Contains("bac")) {
            questionText.text = cleanedQuestion; revealButton.SetActive(true);
            currentHiddenInfo = GetRandomLetter();
        } else questionText.text = cleanedQuestion;
    }

    private string CleanText(string input) => string.IsNullOrEmpty(input) ? "" : input.Replace("\"\"", "\"");
    private string GetRandomLetter() { string chars = "ABCDEFGHIJKLMNOPRST"; return chars[Random.Range(0, chars.Length)].ToString(); }

    private void UpdateDifficultyIcon(string difficulty) {
        if (difficultyIconImage == null) return;
        DifficultyIcon iconData = difficultyIcons.Find(x => x.difficultyName.Trim().ToLower() == difficulty.Trim().ToLower());
        if (iconData != null && iconData.iconSprite != null) { difficultyIconImage.sprite = iconData.iconSprite; difficultyIconImage.gameObject.SetActive(true); }
        else if (defaultIcon != null) { difficultyIconImage.sprite = defaultIcon; difficultyIconImage.gameObject.SetActive(true); }
        else difficultyIconImage.gameObject.SetActive(false);
    }

    private void UpdatePlayerTurn(string mode, QuestionData q) {
        if (mode.Contains("événement") || mode.Contains("évènement")) { playerText.text = "<color=#780000>" + CleanText(q.option1) + "</color>"; playerText.gameObject.SetActive(true); }
        else if (mode.Contains("action") || mode.Contains("vérité") || mode.Contains("culture") || mode.Contains("qui est qui") || mode.Contains("mytho")) {
            string p = (GameManager.Instance.playerNames != null && GameManager.Instance.playerNames.Count > 0) ? GameManager.Instance.playerNames[Random.Range(0, GameManager.Instance.playerNames.Count)] : "Tout le monde";
            playerText.text = "C'est au tour de : <color=#780000>" + p + "</color>"; playerText.gameObject.SetActive(true);
        } else playerText.gameObject.SetActive(false);
    }
}