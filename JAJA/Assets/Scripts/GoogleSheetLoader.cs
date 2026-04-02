using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

// Structure pour stocker plusieurs langues proprement
[System.Serializable]
public class LocalizedString { public string lang; public string text; }

[System.Serializable]
public class QuestionData
{
    public string gameType, difficulty, penalties;
    public List<LocalizedString> texts = new List<LocalizedString>();
    public List<LocalizedString> answers = new List<LocalizedString>();

    public string GetText(string lang)
    {
        var loc = texts.Find(x => x.lang == lang);
        return loc != null ? loc.text : (texts.Count > 0 ? texts[0].text : "");
    }
    public string GetAnswer(string lang)
    {
        var loc = answers.Find(x => x.lang == lang);
        return loc != null ? loc.text : "";
    }
}

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

// --- Cache des traductions d'interface ---
[System.Serializable]
public class InterfaceLangEntry { public string lang; public List<InterfaceKVP> entries = new List<InterfaceKVP>(); }
[System.Serializable]
public class InterfaceKVP { public string key; public string value; }
[System.Serializable]
public class InterfaceTranslationsCache { public List<InterfaceLangEntry> langs = new List<InterfaceLangEntry>(); }


public class GoogleSheetLoader : MonoBehaviour
{
    public static GoogleSheetLoader Instance;

    public List<SheetLink> sheetConfigs;
    public List<GameCategory> inspectorDatabase = new List<GameCategory>();

    public Dictionary<string, List<QuestionData>> gameDatabase = new Dictionary<string, List<QuestionData>>();
    public Dictionary<string, string> gameDescriptions = new Dictionary<string, string>();
    public Dictionary<string, Sprite> gameIcons = new Dictionary<string, Sprite>();

    // true quand les questions sont prêtes, false pendant le chargement
    public bool isDataLoaded { get; private set; } = false;
    // true dès que les traductions UI sont disponibles (cache ou réseau)
    public bool isInterfaceReady { get; private set; } = false;

    private string cachePath;
    private string interfaceCachePath;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else { Destroy(gameObject); return; }
        cachePath = Path.Combine(Application.persistentDataPath, "master_questions_cache.json");
        interfaceCachePath = Path.Combine(Application.persistentDataPath, "interface_translations_cache.json");
    }

    IEnumerator Start()
    {
        isDataLoaded = false;
        isInterfaceReady = false;

        // --- ÉTAPE 1 : Charger les caches locaux immédiatement ---
        LoadInterfaceFromCache();   // Traductions UI (priorité absolue)
        LoadFromLocalCache();       // Questions

        // Lier les icônes et descriptions
        BindIconsAndDescriptions();

        bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hasInternet)
        {
            // --- ÉTAPE 2 : Télécharger la feuille d'interface EN PREMIER ---
            SheetLink interfaceSheet = sheetConfigs.Find(s => s.isInterfaceSheet);
            if (interfaceSheet != null)
            {
                string url = AddCacheBuster(interfaceSheet.url);
                bool interfaceDone = false;
                yield return StartCoroutine(DownloadData(url, interfaceSheet, () => interfaceDone = true));
                // Sauvegarder le cache interface
                SaveInterfaceToCache();
            }

            isInterfaceReady = true;
            if (LocalizationManager.Instance != null) LocalizationManager.Instance.RefreshAllTexts();

            // --- ÉTAPE 3 : Télécharger toutes les feuilles de questions EN PARALLÈLE ---
            float startTime = Time.realtimeSinceStartup;
            gameDatabase.Clear();

            List<SheetLink> questionSheets = sheetConfigs.FindAll(s => !s.isInterfaceSheet);
            int pending = questionSheets.Count;

            foreach (SheetLink config in questionSheets)
            {
                string url = AddCacheBuster(config.url);
                StartCoroutine(DownloadData(url, config, () => pending--));
            }

            while (pending > 0) yield return null;

            LoadLocalCustomQuestions();
            UpdateInspectorList();
            SaveToLocalCache();

            Debug.Log($"[JAJA] Données chargées en {Time.realtimeSinceStartup - startTime:F2}s");
        }
        else
        {
            // Hors-ligne : le cache interface a déjà été chargé plus haut
            isInterfaceReady = true;
            if (LocalizationManager.Instance != null) LocalizationManager.Instance.RefreshAllTexts();
        }

        // Mettre à jour les descriptions localisées APRÈS que l'interface soit prête
        BindIconsAndDescriptions();

        isDataLoaded = true;
        if (LocalizationManager.Instance != null) LocalizationManager.Instance.RefreshAllTexts();
    }

    // ───── HELPERS ─────

    private string AddCacheBuster(string url)
        => url + (url.Contains("?") ? "&" : "?") + "t=" + System.DateTime.Now.Ticks;

    private void BindIconsAndDescriptions()
    {
        foreach (SheetLink config in sheetConfigs)
        {
            if (string.IsNullOrEmpty(config.gameKey)) continue;
            gameIcons[config.gameKey] = config.gameIcon;

            string descKey = "desc_" + config.gameKey;
            string translatedDesc = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(descKey)
                : descKey;

            gameDescriptions[config.gameKey] = (translatedDesc != descKey) ? translatedDesc : config.gameDescription;
        }
    }

    // ───── TÉLÉCHARGEMENT ─────

    public IEnumerator LoadAllSheets()
    {
        float startTime = Time.realtimeSinceStartup;
        isDataLoaded = false;
        gameDatabase.Clear();
        int activeDownloads = sheetConfigs.Count;

        foreach (SheetLink config in sheetConfigs)
            StartCoroutine(DownloadData(AddCacheBuster(config.url), config, () => activeDownloads--));

        while (activeDownloads > 0) yield return null;

        LoadLocalCustomQuestions();
        UpdateInspectorList();
        SaveToLocalCache();
        SaveInterfaceToCache();

        isDataLoaded = true;
        if (LocalizationManager.Instance != null) LocalizationManager.Instance.RefreshAllTexts();
        Debug.Log($"[JAJA] Rechargement complet en {Time.realtimeSinceStartup - startTime:F2}s");
    }

    IEnumerator DownloadData(string url, SheetLink config, System.Action onComplete)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.Success)
                ParseCSV(webRequest.downloadHandler.text, config);
        }
        onComplete?.Invoke();
    }

    // ───── PARSING CSV ─────

    void ParseCSV(string data, SheetLink config)
    {
        string[] lines = data.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return;

        string[] headers = SplitCSVLine(lines[0]);

        if (config.isInterfaceSheet)
        {
            ParseInterfaceMultiLang(lines, headers);
            return;
        }

        if (!gameDatabase.ContainsKey(config.gameKey)) gameDatabase[config.gameKey] = new List<QuestionData>();

        List<int> textCols = new List<int>(), ansCols = new List<int>();
        List<string> textLangs = new List<string>(), ansLangs = new List<string>();
        int diffCol = -1, penCol = -1;

        for (int h = 0; h < headers.Length; h++)
        {
            string head = headers[h].Trim().ToUpper();
            if (head.StartsWith("TEXT_")) { textCols.Add(h); textLangs.Add(head.Replace("TEXT_", "")); }
            else if (head.StartsWith("ANSWER_")) { ansCols.Add(h); ansLangs.Add(head.Replace("ANSWER_", "")); }
            else if (head == "DIFFICULTY" || head == "DIFFICULTE") diffCol = h;
            else if (head == "PENALTIES" || head == "PENALITES" || head == "SIP") penCol = h;
        }

        if (textCols.Count == 0) return;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = SplitCSVLine(lines[i]);
            if (cols.Length <= textCols[0]) continue;

            QuestionData q = new QuestionData { gameType = config.gameKey };
            q.difficulty = (diffCol != -1 && cols.Length > diffCol) ? cols[diffCol].Trim().Replace("\"", "") : "Normal";
            q.penalties = (penCol != -1 && cols.Length > penCol) ? cols[penCol].Trim().Replace("\"", "") : "";

            for (int c = 0; c < textCols.Count; c++)
                if (cols.Length > textCols[c])
                    q.texts.Add(new LocalizedString { lang = textLangs[c], text = CleanCell(cols[textCols[c]]) });

            for (int c = 0; c < ansCols.Count; c++)
                if (cols.Length > ansCols[c])
                    q.answers.Add(new LocalizedString { lang = ansLangs[c], text = CleanCell(cols[ansCols[c]]) });

            gameDatabase[config.gameKey].Add(q);
        }
    }

    void ParseInterfaceMultiLang(string[] lines, string[] headers)
    {
        var tempDict = new Dictionary<string, Dictionary<string, string>>();
        List<int> langIndices = new List<int>();

        for (int h = 0; h < headers.Length; h++)
            if (headers[h].Trim().ToUpper().StartsWith("TEXT_")) langIndices.Add(h);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = SplitCSVLine(lines[i]);
            if (cols.Length < 1 || string.IsNullOrEmpty(cols[0])) continue;

            string key = cols[0].Trim();
            foreach (int idx in langIndices)
            {
                string langCode = headers[idx].Trim().ToUpper().Replace("TEXT_", "");
                if (!tempDict.ContainsKey(langCode)) tempDict[langCode] = new Dictionary<string, string>();
                string val = (cols.Length > idx) ? cols[idx].Trim().Replace("\"", "") : key;
                tempDict[langCode][key] = val;
            }
        }
        LocalizationManager.Instance.LoadAllInterfaceTexts(tempDict);
    }

    // ───── HELPERS CSV ─────

    private string[] SplitCSVLine(string line)
        => Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    private string CleanCell(string s)
        => s.Trim().Replace("\"", "").Replace("\\n", "\n");

    // ───── QUESTIONS CUSTOM ─────

    public void LoadLocalCustomQuestions()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "custom_questions.json");
        if (!File.Exists(filePath)) return;
        string json = File.ReadAllText(filePath);
        var localList = JsonUtility.FromJson<CustomQuestionManager.CustomQuestionList>(json);
        if (localList?.questions == null) return;

        foreach (var q in localList.questions)
        {
            QuestionData conv = new QuestionData { gameType = q.gameType, penalties = q.penalties.ToString(), difficulty = q.difficulty + " (custom)" };
            conv.texts.Add(new LocalizedString { lang = "FR", text = q.text });
            conv.texts.Add(new LocalizedString { lang = "EN", text = q.text });
            if (!gameDatabase.ContainsKey(q.gameType)) gameDatabase[q.gameType] = new List<QuestionData>();
            gameDatabase[q.gameType].Add(conv);
        }
    }

    // ───── CACHE QUESTIONS ─────

    private void SaveToLocalCache()
    {
        DatabaseExport export = new DatabaseExport();
        foreach (var entry in gameDatabase)
            export.categories.Add(new GameCategory { categoryName = entry.Key, questions = entry.Value });
        File.WriteAllText(cachePath, JsonUtility.ToJson(export));
    }

    private void LoadFromLocalCache()
    {
        if (!File.Exists(cachePath)) return;
        DatabaseExport import = JsonUtility.FromJson<DatabaseExport>(File.ReadAllText(cachePath));
        if (import?.categories == null) return;
        foreach (var cat in import.categories) gameDatabase[cat.categoryName] = cat.questions;
        UpdateInspectorList();
    }

    // ───── CACHE INTERFACE (NOUVEAU) ─────

    private void SaveInterfaceToCache()
    {
        var rawData = LocalizationManager.Instance?.GetAllTranslations();
        if (rawData == null || rawData.Count == 0) return;

        InterfaceTranslationsCache cache = new InterfaceTranslationsCache();
        foreach (var langPair in rawData)
        {
            InterfaceLangEntry entry = new InterfaceLangEntry { lang = langPair.Key };
            foreach (var kv in langPair.Value)
                entry.entries.Add(new InterfaceKVP { key = kv.Key, value = kv.Value });
            cache.langs.Add(entry);
        }
        File.WriteAllText(interfaceCachePath, JsonUtility.ToJson(cache));
    }

    private void LoadInterfaceFromCache()
    {
        if (!File.Exists(interfaceCachePath)) return;

        InterfaceTranslationsCache cache = JsonUtility.FromJson<InterfaceTranslationsCache>(File.ReadAllText(interfaceCachePath));
        if (cache?.langs == null || cache.langs.Count == 0) return;

        var rebuilt = new Dictionary<string, Dictionary<string, string>>();
        foreach (var entry in cache.langs)
        {
            rebuilt[entry.lang] = new Dictionary<string, string>();
            foreach (var kv in entry.entries)
                rebuilt[entry.lang][kv.key] = kv.value;
        }

        LocalizationManager.Instance?.LoadAllInterfaceTexts(rebuilt);
        isInterfaceReady = true;
        Debug.Log("[JAJA] Traductions UI chargées depuis le cache local.");
    }

    void UpdateInspectorList()
    {
        inspectorDatabase.Clear();
        foreach (var entry in gameDatabase)
            inspectorDatabase.Add(new GameCategory { categoryName = entry.Key, questions = entry.Value });
    }

    public IEnumerator ReloadForPremium() { yield return StartCoroutine(LoadAllSheets()); }
    public void TriggerReloadForPremium() { StartCoroutine(ReloadForPremium()); }
}