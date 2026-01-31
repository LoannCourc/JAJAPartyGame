using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class QuestionData { public string gameType, text, option1, option2, difficulty, sips; }

[System.Serializable]
public class GameCategory { public string categoryName; public List<QuestionData> questions = new List<QuestionData>(); }

[System.Serializable]
public class SheetLink { public string gameName, url; [TextArea(2, 5)] public string gameDescription; public Sprite gameIcon; }

public class GoogleSheetLoader : MonoBehaviour
{
    public static GoogleSheetLoader Instance;
    
    [Header("Configuration")]
    public List<SheetLink> sheetConfigs;
    
    [Header("Visualisation")]
    public List<GameCategory> inspectorDatabase = new List<GameCategory>();
    
    // Les dictionnaires dont GameMenuManager a besoin
    public Dictionary<string, List<QuestionData>> gameDatabase = new Dictionary<string, List<QuestionData>>();
    public Dictionary<string, string> gameDescriptions = new Dictionary<string, string>();
    public Dictionary<string, Sprite> gameIcons = new Dictionary<string, Sprite>();

    void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else Destroy(gameObject); }
    
    void Start() { if (sheetConfigs.Count > 0) StartCoroutine(LoadAllSheets()); }

    public IEnumerator LoadAllSheets()
    {
        gameDatabase.Clear();
        gameDescriptions.Clear();
        gameIcons.Clear();

        // Remplissage des dictionnaires de visuels avant le téléchargement
        foreach (SheetLink config in sheetConfigs)
        {
            if (!gameDescriptions.ContainsKey(config.gameName)) gameDescriptions.Add(config.gameName, config.gameDescription);
            if (!gameIcons.ContainsKey(config.gameName)) gameIcons.Add(config.gameName, config.gameIcon);
        }

        List<Coroutine> activeCoroutines = new List<Coroutine>();
        foreach (SheetLink config in sheetConfigs) activeCoroutines.Add(StartCoroutine(DownloadData(config.url, config.gameName)));
        foreach (var coroutine in activeCoroutines) yield return coroutine;
        
        LoadLocalCustomQuestions();
        UpdateInspectorList();
        Debug.Log("--- CHARGEMENT TERMINÉ ---");
    }

    IEnumerator DownloadData(string url, string targetGameName)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.Success) ParseCSV(webRequest.downloadHandler.text, targetGameName);
        }
    }

    void ParseCSV(string data, string targetGameName)
    {
        string[] lines = data.Replace("\r", "").Split('\n');
        if (!gameDatabase.ContainsKey(targetGameName)) gameDatabase.Add(targetGameName, new List<QuestionData>());

        Dictionary<string, int> difficultyCounter = new Dictionary<string, int>();
        bool isPremium = PremiumManager.Instance.IsUserPremium;
        int limit = PremiumManager.Instance.maxFreeQuestionsCap;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');

            if (cols.Length >= 6)
            {
                string diffRaw = cols[4].Trim();
                string diffKey = diffRaw.ToLower();

                if (!isPremium)
                {
                    if (!difficultyCounter.ContainsKey(diffKey)) difficultyCounter.Add(diffKey, 0);
                    if (difficultyCounter[diffKey] >= limit) continue;
                    difficultyCounter[diffKey]++;
                }

                QuestionData q = new QuestionData { gameType = targetGameName, difficulty = diffRaw };
                string rawText = cols[5].Trim().Replace("|", "\n");
                string col6 = (cols.Length > 6) ? cols[6].Trim() : "";
                string col7 = (cols.Length > 7) ? cols[7].Trim() : "1";

                if (targetGameName.ToLower().Contains("préfère")) { q.option1 = rawText; q.option2 = col6; q.sips = col7; }
                else { q.text = rawText; q.option1 = col6; q.sips = col7; }

                gameDatabase[targetGameName].Add(q);
            }
        }
    }

    public void LoadLocalCustomQuestions()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "custom_questions.json");
        if (!File.Exists(filePath)) return;

        string json = File.ReadAllText(filePath);
        var localList = JsonUtility.FromJson<CustomQuestionManager.CustomQuestionList>(json);

        foreach (var q in localList.questions)
        {
            QuestionData convertedQ = new QuestionData { 
                gameType = q.gameType, 
                sips = q.sips.ToString(), 
                difficulty = q.difficulty + " (custom)" 
            };

            if (q.gameType.ToLower().Contains("préfère") && q.text.Contains(" | "))
            {
                string[] parts = q.text.Split(new string[] { " | " }, System.StringSplitOptions.None);
                convertedQ.option1 = parts[0]; convertedQ.option2 = parts[1];
            }
            else convertedQ.text = q.text;

            if (!gameDatabase.ContainsKey(q.gameType)) gameDatabase.Add(q.gameType, new List<QuestionData>());
            gameDatabase[q.gameType].Add(convertedQ);
        }
    }

    public IEnumerator ReloadForPremium() { yield return StartCoroutine(LoadAllSheets()); }

    void UpdateInspectorList()
    {
        inspectorDatabase.Clear();
        foreach (var entry in gameDatabase) inspectorDatabase.Add(new GameCategory { categoryName = entry.Key, questions = entry.Value });
    }
}