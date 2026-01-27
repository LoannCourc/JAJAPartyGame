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
    string selectedGame = GameManager.Instance.selectedGameMode;
    // On récupère le choix de l'utilisateur (ex: "easy", "hot"...)
    string targetDiff = GameManager.Instance.selectedDifficulty.ToLower().Trim();
    int targetCount = GameManager.Instance.questionCount;

    if (GoogleSheetLoader.Instance.gameDatabase.ContainsKey(selectedGame))
    {
        List<QuestionData> allQuestions = GoogleSheetLoader.Instance.gameDatabase[selectedGame];
        List<QuestionData> filteredList = new List<QuestionData>();

        // LOGIQUE DE FILTRAGE
        if (targetDiff == "aléatoire" || string.IsNullOrEmpty(targetDiff))
        {
            filteredList = new List<QuestionData>(allQuestions);
        }
        else
        {
            // On cherche les questions dont la difficulté correspond exactement
            filteredList = allQuestions.FindAll(q => q.difficulty.ToLower().Trim() == targetDiff);
        }

        // Sécurité : si aucune question ne correspond à la difficulté (ex: pas de "hot" dans ce jeu)
        if (filteredList.Count == 0)
        {
            Debug.LogWarning("Aucune question trouvée pour cette difficulté, on prend tout !");
            filteredList = new List<QuestionData>(allQuestions);
        }

        // Mélange
        Shuffle(filteredList);

        // Limitation au nombre choisi par le Slider
        int finalAmount = Mathf.Min(targetCount, filteredList.Count);
        gameSessionDeck = filteredList.GetRange(0, finalAmount);

        // Lancement
        GameplayManager.Instance.StartGameSession(gameSessionDeck);

         gamePanel.SetActive(true);
        filterMenu.SetActive(false);
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