using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening; // Nécessaire pour l'animation de l'image

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
    
    [Header("UI Feedback Connexion")]
    public GameObject noConnectionIcon; // GLISSE TON IMAGE "PAS DE WIFI" ICI
    
    // Les dictionnaires
    public Dictionary<string, List<QuestionData>> gameDatabase = new Dictionary<string, List<QuestionData>>();
    public Dictionary<string, string> gameDescriptions = new Dictionary<string, string>();
    public Dictionary<string, Sprite> gameIcons = new Dictionary<string, Sprite>();

    // --- NOUVELLE VARIABLE D'ÉTAT ---
    public bool isDataLoaded { get; private set; } = false;

    void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else Destroy(gameObject); }
    
    void Start() 
    { 
        if (noConnectionIcon != null) noConnectionIcon.SetActive(false);
        StartCoroutine(CheckConnectionAndLoad()); 
    }

    // --- GESTION DE LA CONNEXION ---
    IEnumerator CheckConnectionAndLoad()
    {
        isDataLoaded = false;

        // 1. Vérification initiale
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("Pas de connexion internet. En attente...");
            yield return StartCoroutine(WaitForConnection());
        }

        // 2. Si on est ici, c'est qu'on a du réseau, on lance le téléchargement
        if (sheetConfigs.Count > 0) 
        {
            yield return StartCoroutine(LoadAllSheets());
        }
        else
        {
            isDataLoaded = true; // Pas de sheets à charger, donc on considère que c'est bon
        }
    }

    IEnumerator WaitForConnection()
    {
        // On affiche l'icône et on lance l'anim
        if (noConnectionIcon != null)
        {
            noConnectionIcon.SetActive(true);
            noConnectionIcon.transform.localScale = Vector3.one;
            // Animation : Grossit et rétrécit en boucle (PingPong)
            noConnectionIcon.transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        // On boucle tant qu'il n'y a pas internet
        while (Application.internetReachability == NetworkReachability.NotReachable)
        {
            yield return new WaitForSeconds(1f); // On vérifie chaque seconde
        }

        // Connexion revenue !
        if (noConnectionIcon != null)
        {
            noConnectionIcon.transform.DOKill(); // Stop l'anim
            noConnectionIcon.SetActive(false);
        }
    }

    // --- CHARGEMENT ---
    public IEnumerator LoadAllSheets()
    {
        isDataLoaded = false; // On verrouille le statut
        
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
        
        isDataLoaded = true; // --- C'EST FINI, ON DÉVERROUILLE ---
        Debug.Log("--- CHARGEMENT TERMINÉ (100%) ---");
    }

    // ... Le reste (DownloadData, ParseCSV, LoadLocalCustomQuestions) reste IDENTIQUE ...
    
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
            // Utilisation d'un parseur CSV simple (attention aux virgules dans le texte)
            // Pour faire simple ici on garde ton split, mais attention aux virgules dans tes phrases google sheet
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
                // Reconstitution du texte si des virgules l'ont coupé (méthode basique)
                // Idéalement il faudrait un vrai CSV Parser, mais gardons ta logique pour l'instant
                string rawText = cols[5].Trim().Replace("|", "\n");
                
                // Sécurisation des index
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
        // Assure-toi que CustomQuestionManager est accessible
        var localList = JsonUtility.FromJson<CustomQuestionManager.CustomQuestionList>(json);

        if (localList != null && localList.questions != null)
        {
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