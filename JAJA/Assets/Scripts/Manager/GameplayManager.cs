using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance;

    [Header("UI Textes")]
    public TMP_Text titleText;       // Titre du jeu (ex: Action ou Vérité)
    public TMP_Text progressText;    // Progression (ex: 1 / 20)
    public TMP_Text questionText;    // LA QUESTION (le texte, pas l'ID)
    public TMP_Text playerText;      // Nom du joueur
    public TMP_Text difficultyText;  // Haut à gauche (ex: Facile)
    public TMP_Text sipsDisplay;     // Haut à droite (ex: 3 GORGÉES)

    [Header("UI Objets")]
    public GameObject sipsContainer; // Le parent pour cacher/afficher les gorgées

    [Header("UI Boutons")]
    public TMP_Text nextButtonText; // Glisse le texte (TMP) du bouton Suivant ici
    public GameObject startMenu;   // Glisse ton objet FilterMenu ici
    public GameObject gamePanel;    // Glisse ton objet GamePanel ici
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
            // --- FIN DE PARTIE ---
            questionText.text = "Partie terminée !";
            playerText.text = "Merci d'avoir joué !";
            sipsDisplay.text = "";
            difficultyText.text = "";

            // On change le texte du bouton
            if (nextButtonText != null)
                nextButtonText.text = "MENU";

            // On change la fonction du bouton pour qu'au prochain clic il quitte
            // (On peut faire ça simplement en vérifiant l'index)
            if (currentIndex > currentDeck.Count)
            {
                ReturnToMenu();
            }
        }
    }

    public void ReturnToMenu()
    {
        // Réinitialise le texte du bouton pour la prochaine partie
        if (nextButtonText != null) nextButtonText.text = "SUIVANT";

        // Alterne les panneaux
        gamePanel.SetActive(false);
        startMenu.SetActive(true);
    }
    void DisplayQuestion()
    {
        QuestionData q = currentDeck[currentIndex];

        // 1. Affichage de la proposition réelle (cols[1] du CSV)
        questionText.text = q.text;

        // 2. Progression et Titre
        titleText.text = GameManager.Instance.selectedGameMode;
        progressText.text = (currentIndex + 1) + " / " + currentDeck.Count;

        // 3. Difficulté (Haut à gauche)
        difficultyText.text = q.difficulty;

        // 4. Gorgées (Haut à droite)
        if (sipsContainer.activeSelf)
        {
            sipsDisplay.text = q.sips + " GORGÉES";
        }
        else
        {
            sipsDisplay.text = "";
        }

        // 5. Logique du Joueur : Uniquement pour "Action ou vérité"
        string gameMode = GameManager.Instance.selectedGameMode.ToLower();
        if (gameMode.Contains("action") || gameMode.Contains("vérité"))
        {
            string randomPlayer = GameManager.Instance.playerNames[Random.Range(0, GameManager.Instance.playerNames.Count)];
            playerText.text = "C'est au tour de : " + randomPlayer;
        }
        else
        {
            playerText.text = ""; // Cache le texte pour les autres jeux
        }
    }
}