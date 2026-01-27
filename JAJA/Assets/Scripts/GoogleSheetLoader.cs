using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class QuestionData
{
    public string gameType;
    public string text;
    public string option1;    // Pour "Tu préfères"
    public string option2;    // Pour "Tu préfères"
    public string difficulty;
    public string sips;
}

[System.Serializable]
public class GameCategory
{
    public string categoryName;
    public List<QuestionData> questions = new List<QuestionData>();
}

// Nouvelle classe pour lier un Nom de Jeu à une URL
[System.Serializable]
public class SheetLink
{
    public string gameName; // Exemple: "Action ou Vérité"
    public string url;      // Ton lien CSV
}

public class GoogleSheetLoader : MonoBehaviour
{
    // --- LE SINGLETON ---
    public static GoogleSheetLoader Instance;
    [Header("Configuration")]
    public List<SheetLink> sheetConfigs; // Remplace la liste de string par ceci

    [Header("Visualisation (Lecture Seule)")]
    public List<GameCategory> inspectorDatabase = new List<GameCategory>();

    public Dictionary<string, List<QuestionData>> gameDatabase = new Dictionary<string, List<QuestionData>>();

void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Important pour garder les données entre les écrans
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (sheetConfigs.Count > 0)
            StartCoroutine(LoadAllSheets());
        else
            Debug.LogWarning("Aucune configuration de Sheet n'a été renseignée !");
    }

   IEnumerator LoadAllSheets()
{
    gameDatabase.Clear();
    inspectorDatabase.Clear();

    List<Coroutine> activeCoroutines = new List<Coroutine>();

    // On lance TOUS les téléchargements en même temps
    foreach (SheetLink config in sheetConfigs)
    {
        activeCoroutines.Add(StartCoroutine(DownloadData(config.url, config.gameName)));
    }

    // On attend que toutes les coroutines soient terminées
    foreach (var coroutine in activeCoroutines)
    {
        yield return coroutine;
    }

    Debug.Log("Base de données chargée en parallèle !");
}

    IEnumerator DownloadData(string url, string targetGameName)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // On transmet le nom cible au parsing
                ParseCSV(webRequest.downloadHandler.text, targetGameName);
            }
            else
            {
                Debug.LogError("Erreur sur l'URL de " + targetGameName + " : " + webRequest.error);
            }
        }
    }

    void ParseCSV(string data, string targetGameName)
{
    // On remplace les sauts de ligne Windows par un format standard
    string[] lines = data.Replace("\r", "").Split('\n');
    if (lines.Length < 2) return;

    if (!gameDatabase.ContainsKey(targetGameName))
        gameDatabase.Add(targetGameName, new List<QuestionData>());

    for (int i = 1; i < lines.Length; i++)
    {
        string line = lines[i].Trim();
        if (string.IsNullOrEmpty(line)) continue;

        string[] cols = line.Split(',');

        // On vérifie qu'on a au moins les colonnes de base (0 à 5)
        if (cols.Length >= 6)
        {
            QuestionData q = new QuestionData();
            q.gameType = targetGameName;
            q.difficulty = cols.Length > 4 ? cols[4].Trim() : "Normal"; // Colonne 'category'

            // LOGIQUE "TU PRÉFÈRES"
            if (targetGameName.ToLower().Contains("préfère"))
            {
                // Vérifie qu'on a bien les deux options (index 5 et 6)
                q.option1 = cols[5].Trim();
                q.option2 = (cols.Length > 6) ? cols[6].Trim() : "...";
                q.text = $"{q.option1} \n\n OU \n\n {q.option2} ?";
                
                // Les gorgées sont normalement à l'index 7
                q.sips = (cols.Length > 7) ? cols[7].Trim() : "1";
            }
            else
            {
                // JEUX CLASSIQUES
                q.text = cols[5].Trim(); // Colonne 'text'
                
                // Pour les jeux classiques, les gorgées sont souvent juste après le texte (index 6)
                // MAIS si ton fichier a la même structure partout, elles sont à l'index 7
                q.sips = (cols.Length > 7) ? cols[7].Trim() : "1";
            }

            gameDatabase[targetGameName].Add(q);
        }
        else
        {
            Debug.LogWarning($"Ligne {i} ignorée car incomplète : {line}");
        }
    }
    UpdateInspectorList();
}

    void UpdateInspectorList()
    {
        inspectorDatabase.Clear();
        foreach(KeyValuePair<string, List<QuestionData>> entry in gameDatabase)
        {
            GameCategory newCat = new GameCategory();
            newCat.categoryName = entry.Key;
            newCat.questions = entry.Value;
            inspectorDatabase.Add(newCat);
        }
    }
}