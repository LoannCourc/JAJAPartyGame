using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Données de jeu")]
    public List<QuestionData> gameSessionDeck = new List<QuestionData>();

    void Awake() 
    { 
        if (Instance == null) Instance = this; 
    }

    public void PrepareGame()
    {
        gameSessionDeck.Clear();
        
        // 1. Récupération des réglages actuels
        string selectedGame = GameManager.Instance.selectedGameMode.Trim();
        string targetDiff = GameManager.Instance.selectedDifficulty.ToLower().Trim();
        int targetCount = GameManager.Instance.questionCount;
        
        // Vérifie si l'option "Mes questions uniquement" est cochée dans les paramètres
        bool onlyCustom = SettingsManager.Instance != null && SettingsManager.Instance.onlyCustomQuestions;

        // 2. Récupération du pool global (Google Sheets + JSON injecté)
        List<QuestionData> masterPool = GetMasterPool(selectedGame);

        if (masterPool == null || masterPool.Count == 0)
        {
            Debug.LogError("MasterPool vide pour le jeu : " + selectedGame);
            return;
        }

        // 3. FILTRAGE INTELLIGENT
        List<QuestionData> filteredList = masterPool.FindAll(q => 
        {
            // --- A. FILTRE "MES QUESTIONS UNIQUEMENT" ---
            // On vérifie si la difficulté contient le tag "custom" (ajouté par le Loader lors de l'injection du JSON)
            bool isCustom = q.difficulty.ToLower().Contains("custom");
            
            if (onlyCustom && !isCustom) return false;

            // --- B. FILTRE DE DIFFICULTÉ ---
            // Si le joueur a choisi "Aléatoire", on accepte tout (qui a passé le filtre A)
            if (targetDiff == "aléatoire" || string.IsNullOrEmpty(targetDiff)) 
                return true;

            // On compare la difficulté en ignorant le tag "(custom)" pour que les questions perso
            // soient filtrées comme les questions normales (ex: "facile (custom)" devient "facile")
            string cleanQDiff = q.difficulty.ToLower().Replace("(custom)", "").Trim();
            
            return cleanQDiff == targetDiff;
        });

        // 4. SÉCURITÉ : SI RIEN NE CORRESPOND (Pool vide après filtrage)
        if (filteredList.Count == 0)
        {
            Debug.LogWarning("Aucune question trouvée pour " + targetDiff + ". On prend le pool par défaut.");
            filteredList = new List<QuestionData>(masterPool);
        }

        // 5. CRÉATION DU DECK FINAL
        Shuffle(filteredList);
        
        // On remplit le deck selon le nombre de questions demandé
        for (int k = 0; k < targetCount; k++)
        {
            gameSessionDeck.Add(filteredList[k % filteredList.Count]);
        }

        // 6. AJOUT DES ÉVÉNEMENTS ET LANCEMENT
        AddEventsToDeck();
        GameplayManager.Instance.StartGameSession(gameSessionDeck);
        NavigationManager.Instance.OpenGamePanel();
    }

    private List<QuestionData> GetMasterPool(string gameName)
    {
        string lowerName = gameName.ToLower();
        List<QuestionData> pool = new List<QuestionData>();

        // Logique spéciale pour le mode "On Mixe"
        if (lowerName.Contains("mixe"))
        {
            foreach (var entry in GoogleSheetLoader.Instance.gameDatabase)
            {
                // On n'ajoute pas les événements dans le pool de questions de base
                if (entry.Key.ToLower().Contains("événement") || entry.Key.ToLower().Contains("évènement")) 
                    continue;
                
                pool.AddRange(entry.Value);
            }
        }
        else if (GoogleSheetLoader.Instance.gameDatabase.ContainsKey(gameName))
        {
            pool = new List<QuestionData>(GoogleSheetLoader.Instance.gameDatabase[gameName]);
        }
        return pool;
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
            // Premier événement entre la 4ème et 6ème position
            int currentInsertIndex = Random.Range(4, 6);

            while (currentInsertIndex < gameSessionDeck.Count)
            {
                gameSessionDeck.Insert(currentInsertIndex, eventList[eventIdx % eventList.Count]);
                eventIdx++;
                // Événement suivant toutes les 5 à 7 questions
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