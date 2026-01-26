using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class QuestionData
{
    public string gameType;
    public string text;
    public string difficulty;
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

        foreach (SheetLink config in sheetConfigs)
        {
            // On passe le nom du jeu souhaité à la coroutine
            yield return StartCoroutine(DownloadData(config.url, config.gameName));
        }
        Debug.Log("Base de données chargée avec " + gameDatabase.Count + " jeux distincts !");
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
        string[] lines = data.Split('\n');
        if (lines.Length < 2) return;

        // On crée la catégorie si elle n'existe pas encore
        if (!gameDatabase.ContainsKey(targetGameName))
        {
            gameDatabase.Add(targetGameName, new List<QuestionData>());
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split(',');

            if (cols.Length >= 2)
            {
                QuestionData q = new QuestionData {
                    gameType = targetGameName, // On utilise le nom défini dans l'inspecteur
                    text = cols[1].Trim(),
                    difficulty = cols.Length > 2 ? cols[2].Trim() : "Normal"
                };
                gameDatabase[targetGameName].Add(q);
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