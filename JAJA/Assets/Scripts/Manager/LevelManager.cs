using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Données de jeu")]
    public List<QuestionData> gameSessionDeck = new List<QuestionData>();

    [Header("UI References")]
    public GameObject filterMenu;
    public GameObject gamePanel;

    void Awake() { Instance = this; }

    public void PrepareGame()
    {
        // On nettoie les chaînes (enlève les espaces invisibles et met en minuscule)
        string selectedGame = GameManager.Instance.selectedGameMode.Trim();
        string selectedGameLower = selectedGame.ToLower(); // ex: "on mixe ?"
        
        string targetDiff = GameManager.Instance.selectedDifficulty.ToLower().Trim();
        int targetCount = GameManager.Instance.questionCount;

        List<QuestionData> masterPool = new List<QuestionData>();
        
        Debug.Log($"--- DÉBUT PRÉPARATION : {selectedGame} ---");

        // --- 1. LOGIQUE DE SÉLECTION DU MODE ---

        // On utilise .Contains("mixe") pour être sûr de détecter le mode, même si c'est écrit "On Mixe" ou "On mixe ?"
        if (selectedGameLower.Contains("mixe"))
        {
            Debug.Log(">>> MODE MIXAGE DÉTECTÉ <<<");

            // On parcourt TOUTES les listes chargées dans le Loader
            foreach (var gameEntry in GoogleSheetLoader.Instance.gameDatabase)
            {
                string categoryName = gameEntry.Key;
                string categoryNameLower = categoryName.ToLower();

                // 1. IMPORTANT : On n'ajoute PAS la liste qui s'appelle "On Mixe ?"
                // Sinon on se retrouve avec des cartes dont le titre est "On Mixe ?"
                if (categoryNameLower.Contains("mixe"))
                    continue;

                // 2. On ignore les "Événements" (gérés plus tard)
                if (categoryNameLower.Contains("événement") || categoryNameLower.Contains("évènement"))
                    continue;

                // 3. On ajoute tout le reste (Culture G, Action, Petit Bac...)
                masterPool.AddRange(gameEntry.Value);
                Debug.Log($"[MIX] Ajout de la catégorie : {categoryName} ({gameEntry.Value.Count} cartes)");
            }
        }
        else
        {
            // --- MODE JEU UNIQUE (Culture G, Petit Bac, etc.) ---
            Debug.Log($">>> MODE JEU UNIQUE : {selectedGame} <<<");

            if (GoogleSheetLoader.Instance.gameDatabase.ContainsKey(selectedGame))
            {
                masterPool = new List<QuestionData>(GoogleSheetLoader.Instance.gameDatabase[selectedGame]);
            }
            else
            {
                Debug.LogError($"ERREUR CRITIQUE : La catégorie '{selectedGame}' n'existe pas dans le GoogleSheetLoader ! Vérifie l'orthographe exacte (Espaces, Majuscules).");
                // On tente une recherche "floue" pour sauver le coup
                foreach(var key in GoogleSheetLoader.Instance.gameDatabase.Keys)
                {
                    if(key.ToLower() == selectedGameLower)
                    {
                        masterPool = new List<QuestionData>(GoogleSheetLoader.Instance.gameDatabase[key]);
                        Debug.Log($"Sauvetage : Catégorie '{key}' trouvée malgré la différence de majuscule.");
                        break;
                    }
                }
            }
        }

        // --- 2. SÉCURITÉ : SI LE POOL EST VIDE ---
        if (masterPool.Count == 0)
        {
            Debug.LogError("STOP : Aucune question trouvée. Vérifie que le GoogleSheetLoader a bien fini de charger (Regarde la console).");
            return; 
        }

        // --- 3. FILTRAGE PAR DIFFICULTÉ ---

        List<QuestionData> filteredList = new List<QuestionData>();

        if (targetDiff == "aléatoire" || string.IsNullOrEmpty(targetDiff))
        {
            filteredList = new List<QuestionData>(masterPool);
        }
        else
        {
            filteredList = masterPool.FindAll(q => q.difficulty.ToLower().Trim() == targetDiff);
            
            // Si le filtre est trop strict, on prend tout pour ne pas bloquer
            if (filteredList.Count == 0)
            {
                Debug.LogWarning($"Pas de questions '{targetDiff}'. On prend tout.");
                filteredList = new List<QuestionData>(masterPool);
            }
        }

        Shuffle(filteredList);

        // --- 4. CRÉATION DU DECK FINAL ---

        gameSessionDeck = new List<QuestionData>();
        int i = 0;
        for (int k = 0; k < targetCount; k++)
        {
            gameSessionDeck.Add(filteredList[i % filteredList.Count]);
            i++;
        }

        // --- 5. INSERTION DES ÉVÉNEMENTS ---
        
        AddEventsToDeck();

        // --- 6. LANCEMENT ---
        
        // On s'assure que le menu se ferme bien
        if(filterMenu != null) filterMenu.SetActive(false);
        if(gamePanel != null) gamePanel.SetActive(true);
        
        GameplayManager.Instance.StartGameSession(gameSessionDeck);
    }

    private void AddEventsToDeck()
    {
        List<QuestionData> eventList = null;
        // Recherche la liste événement peu importe la casse
        foreach (var key in GoogleSheetLoader.Instance.gameDatabase.Keys)
        {
            if (key.ToLower().Contains("événement") || key.ToLower().Contains("évènement"))
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