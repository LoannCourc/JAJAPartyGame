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
        
        bool onlyCustom = SettingsManager.Instance != null && SettingsManager.Instance.onlyCustomQuestions;

        // 2. Récupération du pool (On passe la difficulté pour le filtrage intelligent)
        // MODIFICATION ICI : on envoie targetDiff à GetMasterPool
        List<QuestionData> masterPool = GetMasterPool(selectedGame, targetDiff);

        if (masterPool == null || masterPool.Count == 0)
        {
            Debug.LogError("MasterPool vide pour le jeu : " + selectedGame);
            return;
        }

        // 3. FILTRAGE
        List<QuestionData> filteredList = masterPool.FindAll(q => 
        {
            // --- A. FILTRE "MES QUESTIONS UNIQUEMENT" ---
            bool isCustom = q.difficulty.ToLower().Contains("custom");
            if (onlyCustom && !isCustom) return false;

            // --- B. FILTRE DE DIFFICULTÉ ---
            
            // MODIFICATION ICI : Si on est en mode Mixe + Hot, le tri a déjà été fait 
            // précisément dans GetMasterPool, donc on accepte tout ce qui arrive ici.
            if (selectedGame.ToLower().Contains("mixe") && targetDiff == "hot")
            {
                return true; 
            }

            // Comportement classique pour les autres modes
            if (targetDiff == "aléatoire" || string.IsNullOrEmpty(targetDiff)) 
                return true;

            string cleanQDiff = q.difficulty.ToLower().Replace("(custom)", "").Trim();
            return cleanQDiff == targetDiff;
        });

        // 4. SÉCURITÉ
        if (filteredList.Count == 0)
        {
            Debug.LogWarning("Aucune question trouvée pour " + targetDiff + ". On prend le pool par défaut.");
            // Attention : si GetMasterPool a déjà filtré sévèrement, masterPool est peut-être déjà restreint.
            // Dans le doute, on recharge tout sans filtre si c'est vide.
            if (selectedGame.ToLower().Contains("mixe") && targetDiff == "hot")
            {
                filteredList = GetMasterPool(selectedGame, "aléatoire"); // Recharge de secours
            }
            else
            {
                filteredList = new List<QuestionData>(masterPool);
            }
        }

        // 5. CRÉATION DU DECK FINAL
        Shuffle(filteredList);
        
        for (int k = 0; k < targetCount; k++)
        {
            gameSessionDeck.Add(filteredList[k % filteredList.Count]);
        }

        // 6. AJOUT DES ÉVÉNEMENTS
        AddEventsToDeck();
        GameplayManager.Instance.StartGameSession(gameSessionDeck);
        NavigationManager.Instance.OpenGamePanel();
    }

    // MODIFICATION DE LA SIGNATURE : ajout de string targetDifficulty
    private List<QuestionData> GetMasterPool(string gameName, string targetDifficulty)
    {
        string lowerName = gameName.ToLower();
        List<QuestionData> pool = new List<QuestionData>();

        // --- LOGIQUE SPÉCIALE "ON MIXE" ---
        if (lowerName.Contains("mixe"))
        {
            foreach (var entry in GoogleSheetLoader.Instance.gameDatabase)
            {
                string subGameName = entry.Key.ToLower();

                // Ignorer les événements
                if (subGameName.Contains("événement") || subGameName.Contains("évènement")) 
                    continue;

                // --- GESTION DU MODE HOT INTELLIGENT ---
                if (targetDifficulty == "hot")
                {
                    string requiredDiff = "hot"; // Par défaut, on cherche du Hot

                    // Règles spécifiques selon tes jeux :
                    if (subGameName.Contains("culture g") || subGameName.Contains("enchères"))
                    {
                        // Pour ces jeux, le max est "difficile"
                        requiredDiff = "difficile";
                    }
                    else if (subGameName.Contains("qui est qui") || subGameName.Contains("mytho"))
                    {
                        // Pour ces jeux, c'est "unique"
                        requiredDiff = "unique";
                    }
                    // Note: Petit Bac, Action Vérité, etc. restent sur "hot" par défaut.

                    // On ajoute seulement les questions qui correspondent à la difficulté "max" de ce sous-jeu
                    pool.AddRange(entry.Value.FindAll(q => 
                        q.difficulty.ToLower().Replace("(custom)", "").Trim() == requiredDiff
                    ));
                }
                else
                {
                    // Si ce n'est pas le mode Hot, on prend tout, le filtrage se fera dans PrepareGame
                    pool.AddRange(entry.Value);
                }
            }
        }
        // --- LOGIQUE NORMALE (Jeu unique) ---
        else if (GoogleSheetLoader.Instance.gameDatabase.ContainsKey(gameName))
        {
            pool = new List<QuestionData>(GoogleSheetLoader.Instance.gameDatabase[gameName]);
        }
        
        return pool;
    }

    // ... Le reste (AddEventsToDeck, Shuffle) reste identique ...
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