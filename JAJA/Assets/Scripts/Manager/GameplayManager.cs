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

    [Header("UI References")]
    public TMP_Text titleText;
    public TMP_Text sipsDisplay;
    public Image difficultyIconImage;
    public Slider questionSlider;
    public TMP_Text questionText;
    public TMP_Text playerText;
    public GameObject revealButton;
    public TMP_Text extraInfoText;
    public TMP_Text nextButtonText;

    [Header("Configuration des Icônes")]
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
        DisplayQuestion();
    }

    private string CleanText(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        // Remplace les doubles guillemets du CSV par des guillemets simples
        return input.Replace("\"\"", "\"");
    }

    public void DisplayQuestion()
    {
        if (currentDeck == null || currentDeck.Count == 0) return;

        QuestionData q = currentDeck[currentIndex];

        UpdateDifficultyIcon(q.difficulty);

        if (SettingsManager.Instance != null)
        {
            sipsDisplay.gameObject.SetActive(SettingsManager.Instance.showSips);
        }

        titleText.text = q.gameType;
        sipsDisplay.text = q.sips;

        float progress = (float)(currentIndex + 1) / currentDeck.Count;
        questionSlider.value = progress;

        revealButton.SetActive(false);
        extraInfoText.gameObject.SetActive(false);
        currentHiddenInfo = "";

        string mode = q.gameType.ToLower();
        string cleanedQuestion = CleanText(q.text);

        if (mode.Contains("préfère"))
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

            int chance = Random.Range(0, 2);
            if (chance == 0) currentHiddenInfo = CleanText(q.option1);
            else currentHiddenInfo = "<color=#780000>Invente une réponse !</color>";
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

        UpdatePlayerTurn(mode, q);
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
        else
        {
            difficultyIconImage.gameObject.SetActive(false);
        }
    }

    void UpdatePlayerTurn(string mode, QuestionData q)
    {
        if (mode.Contains("événement") || mode.Contains("évènement"))
        {
            playerText.text = "<color=#780000>" + CleanText(q.option1) + "</color>";
            playerText.gameObject.SetActive(true);
        }
        else if (mode.Contains("action") || mode.Contains("vérité") || mode.Contains("culture") || mode.Contains("qui est qui") || mode.Contains("mytho"))
        {
            if (GameManager.Instance.playerNames != null && GameManager.Instance.playerNames.Count > 0)
            {
                string p = GameManager.Instance.playerNames[Random.Range(0, GameManager.Instance.playerNames.Count)];
                playerText.text = "C'est au tour de : <color=#780000>" + p + "</color>";
            }
            else
            {
                playerText.text = "C'est au tour de : <color=#780000>Tout le monde</color>";
            }
            playerText.gameObject.SetActive(true);
        }
        else
        {
            playerText.gameObject.SetActive(false);
        }
    }

    public void OnClickReveal()
    {
        string mode = titleText.text.ToLower();
        TMP_Text buttonText = revealButton.GetComponentInChildren<TMP_Text>();

        if (mode.Contains("culture"))
        {
            QuestionData q = currentDeck[currentIndex];
            if (buttonText.text == "Voir la réponse")
            {
                questionText.text = "<color=#780000>RÉPONSE :</color>\n\n" + CleanText(q.option1);
                buttonText.text = "Voir la question";
            }
            else
            {
                questionText.text = CleanText(q.text);
                buttonText.text = "Voir la réponse";
            }
        }
        else if (mode.Contains("mytho"))
        {
            if (buttonText.text == "Afficher la réponse")
            {
                questionText.text = currentHiddenInfo;
                buttonText.text = "Voir la question";
            }
            else
            {
                questionText.text = CleanText(currentDeck[currentIndex].text);
                buttonText.text = "Afficher la réponse";
            }
        }
        else if (mode.Contains("bac"))
        {
            currentHiddenInfo = GetRandomLetter();
            extraInfoText.gameObject.SetActive(true);
            extraInfoText.text = "<size=150%>" + currentHiddenInfo + "</size>";
            buttonText.text = "Nouvelle lettre";
        }
    }

    private string GetRandomLetter()
    {
        string chars = "ABCDEFGHIJKLMNOPRST";
        return chars[Random.Range(0, chars.Length)].ToString();
    }

    public void NextQuestion()
    {
        currentIndex++;
        if (currentIndex < currentDeck.Count)
        {
            DisplayQuestion();
        }
        else
        {
            NavigationManager.Instance.OpenEndMenu();
        }
    }

    public void ReturnToMenu()
    {
        NavigationManager.Instance.OpenStartMenu();
    }
}