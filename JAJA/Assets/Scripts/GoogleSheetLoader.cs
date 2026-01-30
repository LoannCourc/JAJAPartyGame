using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class QuestionData
{
    public string gameType;
    public string text;
    public string option1;    // Réponse, Option Tu Préfères, ou Mode Événement
    public string option2;    // Option 2 Tu Préfères
    public string difficulty;
    public string sips;
}

[System.Serializable]
public class GameCategory
{
    public string categoryName;
    public List<QuestionData> questions = new List<QuestionData>();
}

[System.Serializable]
public class SheetLink
{
    public string gameName;
    public string url;
    [TextArea(2, 5)]
    public string gameDescription;
    public Sprite gameIcon;
}

public class GoogleSheetLoader : MonoBehaviour
{
    public static GoogleSheetLoader Instance;

    [Header("Configuration")]
    public List<SheetLink> sheetConfigs;

    [Header("Visualisation")]
    public List<GameCategory> inspectorDatabase = new List<GameCategory>();
    public Dictionary<string, List<QuestionData>> gameDatabase = new Dictionary<string, List<QuestionData>>();
    public Dictionary<string, string> gameDescriptions = new Dictionary<string, string>();
    public Dictionary<string, Sprite> gameIcons = new Dictionary<string, Sprite>();
    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void Start()
    {
        if (sheetConfigs.Count > 0) StartCoroutine(LoadAllSheets());
    }

    IEnumerator LoadAllSheets()
    {
        gameDatabase.Clear();
        gameDescriptions.Clear();
        gameIcons.Clear();

        // 1. On pré-remplit les descriptions depuis l'inspecteur
        foreach (SheetLink config in sheetConfigs)
        {
            if (!gameDescriptions.ContainsKey(config.gameName))
                gameDescriptions.Add(config.gameName, config.gameDescription);

            if (!gameIcons.ContainsKey(config.gameName)) 
                gameIcons.Add(config.gameName, config.gameIcon);
        }

        // 2. Préparation du téléchargement
        List<Coroutine> activeCoroutines = new List<Coroutine>();

        foreach (SheetLink config in sheetConfigs)
        {
            activeCoroutines.Add(StartCoroutine(DownloadData(config.url, config.gameName)));
        }

        // 3. C'est ici que l'erreur se règle : on attend que tout soit fini
        foreach (var coroutine in activeCoroutines)
        {
            yield return coroutine;
        }

        UpdateInspectorList();
        Debug.Log("--- CHARGEMENT TERMINÉ ---");
    }

    IEnumerator DownloadData(string url, string targetGameName)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                ParseCSV(webRequest.downloadHandler.text, targetGameName);
            }
            else
            {
                Debug.LogError($"Erreur chargement {targetGameName} : {webRequest.error}");
            }
        }
    }

    void ParseCSV(string data, string targetGameName)
    {
        string[] lines = data.Replace("\r", "").Split('\n');

        if (!gameDatabase.ContainsKey(targetGameName))
            gameDatabase.Add(targetGameName, new List<QuestionData>());

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split(',');

            if (cols.Length >= 6)
            {
                QuestionData q = new QuestionData();
                q.gameType = targetGameName; // Le titre de la carte sera "targetGameName"
                q.difficulty = (cols.Length > 4) ? cols[4].Trim() : "Normal";

                string rawText = cols[5].Trim().Replace("|", "\n");
                string col6 = (cols.Length > 6) ? cols[6].Trim() : "";
                string col7 = (cols.Length > 7) ? cols[7].Trim() : "1";

                if (targetGameName.ToLower().Contains("préfère"))
                {
                    q.option1 = rawText;
                    q.option2 = col6;
                    q.text = "";
                    q.sips = col7;
                }
                else
                {
                    q.text = rawText;
                    q.option1 = col6; // Stocke la réponse ou le mode événement
                    q.sips = col7;
                }

                gameDatabase[targetGameName].Add(q);
            }
        }
    }

    void UpdateInspectorList()
    {
        inspectorDatabase.Clear();
        foreach (KeyValuePair<string, List<QuestionData>> entry in gameDatabase)
        {
            GameCategory newCat = new GameCategory();
            newCat.categoryName = entry.Key;
            newCat.questions = entry.Value;
            inspectorDatabase.Add(newCat);
        }
    }
}