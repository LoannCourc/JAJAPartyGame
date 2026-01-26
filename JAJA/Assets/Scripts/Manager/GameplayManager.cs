using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance;

    [Header("UI References")]
    public TMP_Text titleText;
    public TMP_Text progressText;
    public TMP_Text questionText;
    public TMP_Text playerText;
    public GameObject sipsContainer; // Le parent du texte des gorgées
    public TMP_Text sipsText;

    private int currentIndex = 0;
    private List<QuestionData> currentDeck;

    void Awake() { Instance = this; }

    public void StartGameSession(List<QuestionData> deck)
    {
        currentDeck = deck;
        currentIndex = 0;
        DisplayQuestion();
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
            questionText.text = "Partie terminée !";
            // Ici, tu peux afficher un bouton "Retour au menu"
        }
    }

    void DisplayQuestion()
    {
        QuestionData q = currentDeck[currentIndex];

        // 1. Titre et Progression
        titleText.text = GameManager.Instance.selectedGameMode;
        progressText.text = (currentIndex + 1) + " / " + currentDeck.Count;

        // 2. Question
        questionText.text = q.text;

        // 3. Attribution d'un joueur aléatoire
        if (GameManager.Instance.playerNames.Count > 0)
        {
            string randomPlayer = GameManager.Instance.playerNames[Random.Range(0, GameManager.Instance.playerNames.Count)];
            playerText.text = "C'est au tour de : " + randomPlayer;
        }

        // 4. Gestion des gorgées (Optionnel via bouton paramètres)
        // Supposons que tu as une colonne 'gorgées' dans QuestionData
        // sipsText.text = q.sips + " GORGÉES !"; 
    }

    // Fonction pour ton bouton ON/OFF dans les options
    public void ToggleSipsVisibility(bool isVisible)
    {
        sipsContainer.SetActive(isVisible);
    }
}