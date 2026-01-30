using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Données de jeu")]
    public List<QuestionData> gameSessionDeck = new List<QuestionData>();

    void Awake() { Instance = this; }

    public void PrepareGame()
    {
        gameSessionDeck.Clear();
        // --- 1. SÉLECTION ET PRÉPARATION DU POOL ---
        string selectedGame = GameManager.Instance.selectedGameMode.Trim();
        string selectedGameLower = selectedGame.ToLower();

        string targetDiff = GameManager.Instance.selectedDifficulty.ToLower().Trim();
        int targetCount = GameManager.Instance.questionCount;

        List<QuestionData> masterPool = new List<QuestionData>();

        if (selectedGameLower.Contains("mixe"))
        {
            foreach (var gameEntry in GoogleSheetLoader.Instance.gameDatabase)
            {
                string categoryNameLower = gameEntry.Key.ToLower();
                if (categoryNameLower.Contains("mixe")) continue;
                if (categoryNameLower.Contains("événement") || categoryNameLower.Contains("évènement")) continue;
                masterPool.AddRange(gameEntry.Value);
            }
        }
        else
        {
            // Recherche du deck spécifique
            foreach (var key in GoogleSheetLoader.Instance.gameDatabase.Keys)
            {
                if (key.ToLower() == selectedGameLower)
                {
                    masterPool = new List<QuestionData>(GoogleSheetLoader.Instance.gameDatabase[key]);
                    break;
                }
            }
        }

        if (masterPool.Count == 0)
        {
            return;
        }

        // --- 2. FILTRAGE PAR DIFFICULTÉ ---
        List<QuestionData> filteredList = new List<QuestionData>();
        if (targetDiff == "aléatoire" || string.IsNullOrEmpty(targetDiff))
        {
            filteredList = new List<QuestionData>(masterPool);
        }
        else
        {
            filteredList = masterPool.FindAll(q => q.difficulty.ToLower().Trim() == targetDiff);
            if (filteredList.Count == 0) filteredList = new List<QuestionData>(masterPool);
        }

        Shuffle(filteredList);

        // --- 3. CRÉATION DU DECK FINAL ---
        gameSessionDeck = new List<QuestionData>();
        for (int k = 0; k < targetCount; k++)
        {
            gameSessionDeck.Add(filteredList[k % filteredList.Count]);
        }

        AddEventsToDeck();
        GameplayManager.Instance.StartGameSession(gameSessionDeck);
        NavigationManager.Instance.OpenGamePanel();
    }

    private void AddEventsToDeck()
    {
        List<QuestionData> eventList = null;
        foreach (var key in GoogleSheetLoader.Instance.gameDatabase.Keys)
        {
            string kLower = key.ToLower();
            if (kLower.Contains("événement") || kLower.Contains("évènement"))
            {
                eventList = new List<QuestionData>(GoogleSheetLoader.Instance.gameDatabase[key]);
                break;
            }
        }

        if (eventList != null && eventList.Count > 0)
        {
            Shuffle(eventList);
            int eventIdx = 0;
            int currentInsertIndex = Random.Range(4, 6);

            while (currentInsertIndex < gameSessionDeck.Count)
            {
                gameSessionDeck.Insert(currentInsertIndex, eventList[eventIdx % eventList.Count]);
                eventIdx++;
                currentInsertIndex += Random.Range(5, 7);
            }
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