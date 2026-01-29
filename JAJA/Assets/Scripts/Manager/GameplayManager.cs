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
    public GameObject startMenu;
    public GameObject gamePanel;
    public TMP_Text nextButtonText;

    private int currentIndex = 0;
    private List<QuestionData> currentDeck = new List<QuestionData>();
    private string currentHiddenInfo = "";

    void Awake() { Instance = this; }

    public void StartGameSession(List<QuestionData> deck)
    {
        currentDeck = deck;
        currentIndex = 0;
        nextButtonText.text = "SUIVANT";
        
        // Sécurité : Si le deck est vide, on retourne au menu
        if (currentDeck == null || currentDeck.Count == 0)
        {
            Debug.LogError("StartGameSession appelé avec un deck vide !");
            ReturnToMenu();
            return;
        }

        DisplayQuestion();
    }

    public void DisplayQuestion()
    {
        QuestionData q = currentDeck[currentIndex];

        // 1. Mise à jour des Textes Fixes
        titleText.text = q.gameType; 
        sipsDisplay.text = q.sips;
        float progress = (float)(currentIndex + 1) / currentDeck.Count;
        questionSlider.value = progress;

        // 2. Reset UI
        revealButton.SetActive(false);
        extraInfoText.gameObject.SetActive(false);
        currentHiddenInfo = "";
        
        string mode = q.gameType.ToLower();

        // 3. LOGIQUE D'AFFICHAGE DU CONTENU
        
        // --- TU PRÉFÈRES ---
        if (mode.Contains("préfère"))
        {
            questionText.text = $"{q.option1}\n<color=#780000><size=80%>— OU —</size></color>\n{q.option2}";
        }
        // --- QUI EST QUI ---
        else if (mode.Contains("qui est qui"))
        {
            questionText.text = "<align=left>• " + q.text.Replace("\n", "\n• ") + "</align>";
        }
        // --- MYTHO OU RÉALITÉ ---
        else if (mode.Contains("mytho"))
        {
            revealButton.SetActive(true);
            revealButton.GetComponentInChildren<TMP_Text>().text = "Afficher la réponse";
            questionText.text = q.text;

            int chance = Random.Range(0, 2);
            if (chance == 0) currentHiddenInfo = q.option1; 
            else currentHiddenInfo = "<color=#780000>Invente une réponse !</color>";
        }
        // --- CULTURE G ---
        else if (mode.Contains("culture"))
        {
            revealButton.SetActive(true);
            revealButton.GetComponentInChildren<TMP_Text>().text = "Voir la réponse";
            questionText.text = q.text;
        }
        // --- PETIT BAC ---
        else if (mode.Contains("bac"))
        {
            questionText.text = q.text;
            revealButton.SetActive(true);
            revealButton.GetComponentInChildren<TMP_Text>().text = "Afficher la lettre";
            currentHiddenInfo = GetRandomLetter();
        }
        // --- CAS GÉNÉRAL & ÉVÉNEMENTS ---
        else
        {
            questionText.text = q.text;
        }

        // 4. GESTION DU JOUEUR (Tous ensemble vs Tour par tour)
        UpdatePlayerTurn(mode, q);
    }

    void UpdatePlayerTurn(string mode, QuestionData q)
    {
        // CAS SPÉCIAL : ÉVÉNEMENTS
        if (mode.Contains("événement") || mode.Contains("évènement"))
        {
            // On affiche ce qui est écrit dans la colonne 7 (option1) : "Tous ensemble", "Chacun son tour", etc.
            playerText.text = "<color=#780000>" + q.option1 + "</color>"; 
            playerText.gameObject.SetActive(true);
        }
        // CAS JEUX NOMINATIFS
        else if (mode.Contains("action") || mode.Contains("vérité") || mode.Contains("culture") || mode.Contains("qui est qui") || mode.Contains("mytho"))
        {
            string p = GameManager.Instance.playerNames[Random.Range(0, GameManager.Instance.playerNames.Count)];
            playerText.text = "C'est au tour de : <color=#780000>" + p + "</color>";
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
        if (currentIndex < currentDeck.Count) DisplayQuestion();
        else ReturnToMenu();
    }

    public void ReturnToMenu()
    {
        gamePanel.SetActive(false);
        startMenu.SetActive(true);
        // Important : On réactive le menu de filtre pour la prochaine partie
        LevelManager.Instance.filterMenu.SetActive(true);
    }
}