using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    [Header("Visualisation")]
    public List<GameCategory> inspectorDatabase = new List<GameCategory>();

    [Header("UI Feedback Connexion")]
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

        if (sheetConfigs.Count > 0) yield return StartCoroutine(LoadAllSheets());
        else isDataLoaded = true;
    }

    IEnumerator WaitForConnection()
    {
        if (noConnectionIcon != null)
        {
            noConnectionIcon.SetActive(true);
            noConnectionIcon.transform.localScale = Vector3.one;
            noConnectionIcon.transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
        while (Application.internetReachability == NetworkReachability.NotReachable) yield return new WaitForSeconds(1f);
        if (noConnectionIcon != null) { noConnectionIcon.transform.DOKill(); noConnectionIcon.SetActive(false); }
    }

    public IEnumerator LoadAllSheets()
    {
        isDataLoaded = false;
        gameDatabase.Clear();
        gameDescriptions.Clear();
        gameIcons.Clear();

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
        isDataLoaded = true;
        Debug.Log("--- CHARGEMENT TERMINÉ (100%) ---");
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
        bool isPremium = PremiumManager.Instance != null && PremiumManager.Instance.IsUserPremium;
        int limit = PremiumManager.Instance != null ? PremiumManager.Instance.maxFreeQuestionsCap : 30;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split(',');

            // On vérifie qu'on a bien nos 8 colonnes (0 à 7)
            if (cols.Length >= 8)
            {
                string diffRaw = cols[3].Trim();
                string diffKey = diffRaw.ToLower();

                if (!isPremium)
                {
                    if (!difficultyCounter.ContainsKey(diffKey)) difficultyCounter.Add(diffKey, 0);
                    if (difficultyCounter[diffKey] >= limit) continue;
                    difficultyCounter[diffKey]++;
                }

                QuestionData q = new QuestionData { gameType = targetGameName, difficulty = diffRaw };
                string lowGameName = targetGameName.ToLower();

                // --- CAS 1 : DILEMME (Structure : Opt1 en 5, Opt2 en 6, Malus en 7) ---
                if (lowGameName.Contains("dilemme"))
                {
                    q.option1 = cols[5].Trim();
                    q.option2 = cols[6].Trim();
                    q.penalties = cols[7].Trim();
                }
                // --- CAS 2 : CULTURE G ou MYTHO (Structure : Question en 5, Réponse en 6, Malus en 7) ---
                else if (lowGameName.Contains("culture") || lowGameName.Contains("mytho"))
                {
                    q.text = cols[5].Trim().Replace("|", "\n");
                    q.option1 = cols[6].Trim(); // La réponse s'affiche via option1
                    q.penalties = cols[7].Trim();
                }
                // --- CAS PAR DÉFAUT (Révélations, Désignations... : Texte en 5, Vide en 6, Malus en 7) ---
                else
                {
                    q.text = cols[5].Trim().Replace("|", "\n");
                    q.penalties = cols[7].Trim();
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

        if (localList != null && localList.questions != null)
        {
            foreach (var q in localList.questions)
            {
                QuestionData convertedQ = new QuestionData
                {
                    gameType = q.gameType,
                    penalties = q.penalties.ToString(),
                    difficulty = q.difficulty + " (custom)"
                };

                if (q.gameType.ToLower().Contains("dilemme") && q.text.Contains(" | "))
                {
                    string[] parts = q.text.Split(new string[] { " | " }, System.StringSplitOptions.None);
                    if (parts.Length > 1) { convertedQ.option1 = parts[0]; convertedQ.option2 = parts[1]; }
                    else { convertedQ.text = q.text; }
                }
                else convertedQ.text = q.text;

                if (!gameDatabase.ContainsKey(q.gameType)) gameDatabase.Add(q.gameType, new List<QuestionData>());
                gameDatabase[q.gameType].Add(convertedQ);
            }
        }
    }

    public IEnumerator ReloadForPremium() { yield return StartCoroutine(LoadAllSheets()); }
    void UpdateInspectorList()
    {
        inspectorDatabase.Clear();
        foreach (var entry in gameDatabase) inspectorDatabase.Add(new GameCategory { categoryName = entry.Key, questions = entry.Value });
    }
}