using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance;

    [Header("UI References")]
    public TMP_Text titleText;
    public TMP_Text sipsDisplay;
    public Slider questionSlider;
    public TMP_Text questionText;
    public TMP_Text playerText;
    public GameObject revealButton;
    public TMP_Text extraInfoText;
    public TMP_Text nextButtonText;

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

    public void DisplayQuestion()
    {
        QuestionData q = currentDeck[currentIndex];

        // --- GESTION DES OPTIONS (SettingsManager) ---
        if (SettingsManager.Instance != null)
        {
            // Cacher/Afficher les gorgées selon le Toggle
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

        // --- LOGIQUE D'AFFICHAGE ---
        if (mode.Contains("préfère"))
        {
            questionText.text = $"{q.option1}\n<color=#780000><size=80%>— OU —</size></color>\n{q.option2}";
        }
        else if (mode.Contains("qui est qui"))
        {
            questionText.text = "<align=left>• " + q.text.Replace("\n", "\n• ") + "</align>";
        }
        else if (mode.Contains("mytho"))
        {
            revealButton.SetActive(true);
            revealButton.GetComponentInChildren<TMP_Text>().text = "Afficher la réponse";
            questionText.text = q.text;

            int chance = Random.Range(0, 2);
            if (chance == 0) currentHiddenInfo = q.option1;
            else currentHiddenInfo = "<color=#780000>Invente une réponse !</color>";
        }
        else if (mode.Contains("culture"))
        {
            revealButton.SetActive(true);
            revealButton.GetComponentInChildren<TMP_Text>().text = "Voir la réponse";
            questionText.text = q.text;
        }
        else if (mode.Contains("bac"))
        {
            questionText.text = q.text;
            revealButton.SetActive(true);
            revealButton.GetComponentInChildren<TMP_Text>().text = "Afficher la lettre";
            currentHiddenInfo = GetRandomLetter();
        }
        else
        {
            questionText.text = q.text;
        }

        UpdatePlayerTurn(mode, q);
    }

    void UpdatePlayerTurn(string mode, QuestionData q)
    {
        if (mode.Contains("événement") || mode.Contains("évènement"))
        {
            playerText.text = "<color=#780000>" + q.option1 + "</color>";
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
                questionText.text = "<color=#780000>RÉPONSE :</color>\n\n" + q.option1;
                buttonText.text = "Voir la question";
            }
            else
            {
                questionText.text = q.text;
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
                questionText.text = currentDeck[currentIndex].text;
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