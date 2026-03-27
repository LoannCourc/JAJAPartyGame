using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions; // Ajouté pour le CSV complexe
using DG.Tweening;

[System.Serializable]
public class QuestionData { public string gameType, text, option1, option2, difficulty, penalties; }

[System.Serializable]
public class GameCategory { public string categoryName; public List<QuestionData> questions = new List<QuestionData>(); }

[System.Serializable]
public class SheetLink
{
    public string gameName, url;
    [TextArea(2, 5)] public string gameDescription;
    public Sprite gameIcon;
}

public class GoogleSheetLoader : MonoBehaviour
{
    public static GoogleSheetLoader Instance;

    [Header("Configuration")]
    public List<SheetLink> sheetConfigs;

    [Header("Visualisation (Editor Only)")]
    public List<GameCategory> inspectorDatabase = new List<GameCategory>();

    [Header("UI Feedback")]
    public GameObject noConnectionIcon;

    public Dictionary<string, List<QuestionData>> gameDatabase = new Dictionary<string, List<QuestionData>>();
    public Dictionary<string, string> gameDescriptions = new Dictionary<string, string>();
    public Dictionary<string, Sprite> gameIcons = new Dictionary<string, Sprite>();

    public bool isDataLoaded { get; private set; } = false;

    void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else Destroy(gameObject); }

    void Start()
    {
        if (noConnectionIcon != null) noConnectionIcon.SetActive(false);
        StartCoroutine(CheckConnectionAndLoad());
    }

    IEnumerator CheckConnectionAndLoad()
    {
        isDataLoaded = false;
        if (Application.internetReachability == NetworkReachability.NotReachable)
            yield return StartCoroutine(WaitForConnection());

        yield return StartCoroutine(LoadAllSheets());
    }

    IEnumerator WaitForConnection()
    {
        if (noConnectionIcon != null)
        {
            noConnectionIcon.SetActive(true);
            noConnectionIcon.transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo);
        }
        while (Application.internetReachability == NetworkReachability.NotReachable) yield return new WaitForSeconds(1f);
        if (noConnectionIcon != null) { noConnectionIcon.transform.DOKill(); noConnectionIcon.SetActive(false); }
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
            if (!gameDescriptions.ContainsKey(config.gameName)) gameDescriptions.Add(config.gameName, config.gameDescription);
            if (!gameIcons.ContainsKey(config.gameName)) gameIcons.Add(config.gameName, config.gameIcon);
        }

        int activeDownloads = sheetConfigs.Count;

        foreach (SheetLink config in sheetConfigs)
        {
            StartCoroutine(DownloadData(config.url, config.gameName, () => activeDownloads--));
        }

        // Attend que tous les téléchargements soient finis
        while (activeDownloads > 0) yield return null;

        LoadLocalCustomQuestions();
        
        #if UNITY_EDITOR
        UpdateInspectorList(); // On ne le fait que dans l'éditeur pour éviter de ramer sur mobile
        #endif

        Debug.Log($"Chargement terminé en {Time.realtimeSinceStartup - startTime:F2}s");
        isDataLoaded = true;
    }

    IEnumerator DownloadData(string url, string targetGameName, System.Action onComplete)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.Success) 
                ParseCSV(webRequest.downloadHandler.text, targetGameName);
            else
                Debug.LogError($"Erreur sur {targetGameName}: {webRequest.error}");
        }
        onComplete?.Invoke();
    }

    void ParseCSV(string data, string targetGameName)
    {
        // Split par ligne en gérant les retours chariots
        string[] lines = data.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        
        if (!gameDatabase.ContainsKey(targetGameName)) gameDatabase.Add(targetGameName, new List<QuestionData>());

        bool isPremium = PremiumManager.Instance != null && PremiumManager.Instance.IsUserPremium;
        int limit = PremiumManager.Instance != null ? PremiumManager.Instance.maxFreeQuestionsCap : 30;
        Dictionary<string, int> difficultyCounter = new Dictionary<string, int>();

        for (int i = 1; i < lines.Length; i++)
        {
            // Expression régulière pour splitter par virgule SAUF si la virgule est entre guillemets
            string[] cols = Regex.Split(lines[i], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            if (cols.Length >= 5) // On baisse à 5 minimum pour être plus souple
            {
                string diffRaw = cols[4].Trim().Replace("\"", "");
                string diffKey = diffRaw.ToLower();

                if (!isPremium)
                {
                    if (!difficultyCounter.ContainsKey(diffKey)) difficultyCounter.Add(diffKey, 0);
                    if (difficultyCounter[diffKey] >= limit) continue;
                    difficultyCounter[diffKey]++;
                }

                QuestionData q = new QuestionData { gameType = targetGameName, difficulty = diffRaw };
                string lowGameName = targetGameName.ToLower();

                // Nettoyage des guillemets éventuels sur les colonnes de texte
                string col5 = cols.Length > 5 ? cols[5].Trim().Replace("\"", "").Replace("|", "\n") : "";
                string col6 = cols.Length > 6 ? cols[6].Trim().Replace("\"", "") : "";
                string col7 = cols.Length > 7 ? cols[7].Trim().Replace("\"", "") : "";

                if (lowGameName.Contains("dilemme"))
                {
                    q.option1 = col5;
                    q.option2 = col6;
                    q.penalties = col7;
                }
                else if (lowGameName.Contains("culture") || lowGameName.Contains("mytho"))
                {
                    q.text = col5;
                    q.option1 = col6;
                    q.penalties = col7;
                }
                else
                {
                    q.text = col5;
                    q.penalties = col7;
                }

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
    public IEnumerator ReloadForPremium() 
    { 
        yield return StartCoroutine(LoadAllSheets()); 
    }
    void UpdateInspectorList()
    {
        inspectorDatabase.Clear();
        foreach (var entry in gameDatabase) inspectorDatabase.Add(new GameCategory { categoryName = entry.Key, questions = entry.Value });
    }
}