using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Pour faciliter le tri

public class LevelManager : MonoBehaviour
{
    [Header("Données de jeu")]
    public List<QuestionData> gameSessionDeck = new List<QuestionData>();

    public GameObject filterMenu; // L'objet parent de ton écran de filtres
    public GameObject gamePanel; // L'objet parent de ton écran de jeu

    public void PrepareGame()
    {
        // 1. Récupérer les paramètres du GameManager
        string selectedGame = GameManager.Instance.selectedGameMode;
        string selectedDiff = GameManager.Instance.selectedDifficulty;
        int targetCount = GameManager.Instance.questionCount;

        // 2. Récupérer toutes les questions de l'onglet correspondant
        if (GoogleSheetLoader.Instance.gameDatabase.ContainsKey(selectedGame))
        {
            List<QuestionData> allQuestions = GoogleSheetLoader.Instance.gameDatabase[selectedGame];

            // 3. Filtrage par difficulté (sauf si "Aléatoire")
            List<QuestionData> filteredList = new List<QuestionData>();
            if (selectedDiff.ToLower() == "aléatoire") {
                filteredList = new List<QuestionData>(allQuestions);
            } else {
                filteredList = allQuestions.Where(q => q.difficulty.ToLower() == selectedDiff.ToLower()).ToList();
            }

            // 4. Mélange de la liste (Shuffle)
            Shuffle(filteredList);

            // 5. Sélection du nombre final de questions
            int finalAmount = Mathf.Min(targetCount, filteredList.Count);
            gameSessionDeck = filteredList.GetRange(0, finalAmount);

            Debug.Log($"Partie lancée ! {gameSessionDeck.Count} questions prêtes pour le jeu : {selectedGame}");
            
            filterMenu.SetActive(false);
            gamePanel.SetActive(true);
            GameplayManager.Instance.StartGameSession(gameSessionDeck);
            // 6. Ici : Charger ta scène de jeu réelle (Visualisation des cartes)
            // SceneManager.LoadScene("GamePlay");
        }
    }

    private void Shuffle(List<QuestionData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            QuestionData temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}