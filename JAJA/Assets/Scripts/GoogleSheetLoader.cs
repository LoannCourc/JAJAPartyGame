using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DG.Tweening;

[System.Serializable]
public class QuestionData { public string gameType, text, option1, option2, difficulty, penalties; }

[System.Serializable]
public class GameCategory { public string categoryName; public List<QuestionData> questions = new List<QuestionData>(); }

[System.Serializable]
public class SheetLink
{
    public string gameName; 
    public string gameKey;  
    public string url;
    public bool isInterfaceSheet = false; 
    [TextArea(2, 5)] public string gameDescription;
    public Sprite gameIcon;
}

[System.Serializable]
public class DatabaseExport { public List<GameCategory> categories = new List<GameCategory>(); }

public class GoogleSheetLoader : MonoBehaviour
{
    public static GoogleSheetLoader Instance;

    [Header("Configuration")]
    public List<SheetLink> sheetConfigs;
    public string langColumnPrefix = "TEXT_"; 
    public string answerColumnPrefix = "ANSWER_"; // NOUVEAU : Préfixe pour les colonnes réponses

    [Header("Visualisation (Editor Only)")]
    public List<GameCategory> inspectorDatabase = new List<GameCategory>();

    [Header("UI Feedback")]
    public GameObject noConnectionIcon;

    public Dictionary<string, List<QuestionData>> gameDatabase = new Dictionary<string, List<QuestionData>>();
    public Dictionary<string, string> gameDescriptions = new Dictionary<string, string>();
    public Dictionary<string, Sprite> gameIcons = new Dictionary<string, Sprite>();

    public bool isDataLoaded { get; private set; } = false;
    private string cachePath;

    void Awake() 
    { 
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else Destroy(gameObject); 
        cachePath = Path.Combine(Application.persistentDataPath, "questions_cache.json");
    }

    void Start()
    {
        if (noConnectionIcon != null) noConnectionIcon.SetActive(false);
        StartCoroutine(CheckConnectionAndLoad());
    }

    IEnumerator CheckConnectionAndLoad()
    {
        isDataLoaded = false;
        LoadFromLocalCache(); 

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            yield return StartCoroutine(LoadAllSheets());
            SaveToLocalCache();
        }
        else { isDataLoaded = true; }
    }

    public IEnumerator LoadAllSheets()
    {
        float startTime = Time.realtimeSinceStartup;
        isDataLoaded = false;
        gameDatabase.Clear();
        gameDescriptions.Clear();
        gameIcons.Clear();

        foreach (SheetLink config in sheetConfigs)
        {
            if (string.IsNullOrEmpty(config.gameKey)) continue; 
            if (!gameDescriptions.ContainsKey(config.gameKey)) gameDescriptions.Add(config.gameKey, config.gameDescription);
            if (!gameIcons.ContainsKey(config.gameKey)) gameIcons.Add(config.gameKey, config.gameIcon);
        }

        int activeDownloads = sheetConfigs.Count;
        foreach (SheetLink config in sheetConfigs)
        {
            StartCoroutine(DownloadData(config, () => activeDownloads--));
        }

        while (activeDownloads > 0) yield return null;

        LoadLocalCustomQuestions();
        
        #if UNITY_EDITOR
        UpdateInspectorList();
        #endif

        Debug.Log($"Chargement terminé en {Time.realtimeSinceStartup - startTime:F2}s");
        isDataLoaded = true;
    }

    IEnumerator DownloadData(SheetLink config, System.Action onComplete)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(config.url))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.Success) 
                ParseCSV(webRequest.downloadHandler.text, config);
            else
                Debug.LogError($"Erreur sur {config.gameKey}: {webRequest.error}");
        }
        onComplete?.Invoke();
    }

    void ParseCSV(string data, SheetLink config)
    {
        string[] lines = data.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return;

        string[] headers = Regex.Split(lines[0], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
        
        // --- DÉTECTION DYNAMIQUE DES COLONNES ---
        string targetTextHeader = (langColumnPrefix + LocalizationManager.Instance.currentLang).ToUpper(); // ex: TEXT_FR
        string targetAnsHeader = (answerColumnPrefix + LocalizationManager.Instance.currentLang).ToUpper(); // ex: ANSWER_FR
        
        int textCol = -1;
        int ansCol = -1;
        int diffCol = 4; // Valeurs par défaut pour rétro-compatibilité
        int penCol = 7;

        for (int h = 0; h < headers.Length; h++)
        {
            string headerName = headers[h].Trim().ToUpper();
            if (headerName == targetTextHeader) textCol = h;
            else if (headerName == targetAnsHeader) ansCol = h;
            else if (headerName == "DIFFICULTY" || headerName == "DIFFICULTE") diffCol = h;
            else if (headerName == "PENALTIES" || headerName == "PENALITES" || headerName == "SIP") penCol = h;
        }

        if (textCol == -1) return; // Si pas de colonne texte, on ignore

        // CAS DE L'INTERFACE
        if (config.isInterfaceSheet)
        {
            Dictionary<string, string> uiDict = new Dictionary<string, string>();
            for (int i = 1; i < lines.Length; i++)
            {
                string[] cols = Regex.Split(lines[i], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                if (cols.Length > textCol) uiDict[cols[0].Trim()] = cols[textCol].Trim().Replace("\"", "");
            }
            LocalizationManager.Instance.LoadInterfaceTexts(uiDict);
            return;
        }

        if (!gameDatabase.ContainsKey(config.gameKey)) gameDatabase.Add(config.gameKey, new List<QuestionData>());

        // OPTIONNEL : Gestion des limites (Premium) si tu as toujours ce système
        bool isPremium = true; // Remplace par ta vraie vérification Premium si nécessaire
        int limit = 30; 
        Dictionary<string, int> difficultyCounter = new Dictionary<string, int>();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = Regex.Split(lines[i], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            if (cols.Length > textCol)
            {
                string diffRaw = cols.Length > diffCol ? cols[diffCol].Trim().Replace("\"", "") : "Normal";
                string diffKey = diffRaw.ToLower();

                if (!isPremium)
                {
                    if (!difficultyCounter.ContainsKey(diffKey)) difficultyCounter.Add(diffKey, 0);
                    if (difficultyCounter[diffKey] >= limit) continue;
                    difficultyCounter[diffKey]++;
                }

                QuestionData q = new QuestionData { gameType = config.gameKey, difficulty = diffRaw };
                string lowGameName = config.gameKey.ToLower();

                // On extrait les valeurs dynamiquement
                string colText = cols[textCol].Trim().Replace("\"", "").Replace("\\n", "\n");
                string colAns = (ansCol != -1 && cols.Length > ansCol) ? cols[ansCol].Trim().Replace("\"", "").Replace("\\n", "\n") : "";
                q.penalties = cols.Length > penCol ? cols[penCol].Trim().Replace("\"", "") : "";

                // LOGIQUE DILEMME (Option 1 | Option 2)
                if (lowGameName.Contains("dilemme"))
                {
                    if (colText.Contains("|"))
                    {
                        string[] p = colText.Split('|');
                        q.option1 = p[0].Trim(); 
                        q.option2 = p.Length > 1 ? p[1].Trim() : "";
                    }
                    else 
                    {
                        q.text = colText; // Au cas où tu as oublié le |
                    }
                }
                // LOGIQUE CULTURE G ET MYTHO (Question et Réponse)
                else if (lowGameName.Contains("culture") || lowGameName.Contains("mytho"))
                {
                    q.text = colText;
                    q.option1 = colAns; // On stocke la réponse dans option1
                }
                // LOGIQUE STANDARD
                else
                {
                    q.text = colText;
                }

                gameDatabase[config.gameKey].Add(q);
            }
        }
    }

    public void LoadLocalCustomQuestions()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "custom_questions.json");
        if (!File.Exists(filePath)) return;
        string json = File.ReadAllText(filePath);
        var localList = JsonUtility.FromJson<CustomQuestionManager.CustomQuestionList>(json);

        if (localList?.questions != null)
        {
            foreach (var q in localList.questions)
            {
                QuestionData convertedQ = new QuestionData { gameType = q.gameType, penalties = q.penalties.ToString(), difficulty = q.difficulty + " (custom)" };
                if (q.gameType.ToLower().Contains("dilemme") && q.text.Contains(" | "))
                {
                    string[] parts = q.text.Split(new[] { " | " }, System.StringSplitOptions.None);
                    if (parts.Length > 1) { convertedQ.option1 = parts[0]; convertedQ.option2 = parts[1]; }
                    else convertedQ.text = q.text;
                }
                else convertedQ.text = q.text;

                if (!gameDatabase.ContainsKey(q.gameType)) gameDatabase.Add(q.gameType, new List<QuestionData>());
                gameDatabase[q.gameType].Add(convertedQ);
            }
        }
    }

    private void SaveToLocalCache()
    {
        DatabaseExport export = new DatabaseExport();
        foreach (var entry in gameDatabase) export.categories.Add(new GameCategory { categoryName = entry.Key, questions = entry.Value });
        File.WriteAllText(cachePath, JsonUtility.ToJson(export));
    }

    private void LoadFromLocalCache()
    {
        if (File.Exists(cachePath))
        {
            DatabaseExport import = JsonUtility.FromJson<DatabaseExport>(File.ReadAllText(cachePath));
            foreach (var cat in import.categories) gameDatabase[cat.categoryName] = cat.questions;
            #if UNITY_EDITOR
            UpdateInspectorList();
            #endif
        }
    }

    public IEnumerator ReloadForPremium() { yield return StartCoroutine(LoadAllSheets()); SaveToLocalCache(); }

    void UpdateInspectorList()
    {
        inspectorDatabase.Clear();
        foreach (var entry in gameDatabase) inspectorDatabase.Add(new GameCategory { categoryName = entry.Key, questions = entry.Value });
    }
}