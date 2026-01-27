using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // Nécessaire pour changer d'écran
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // Singleton pour accéder au GameManager depuis n'importe quel script
    public static GameManager Instance;

    [Header("Données de Session")]
    public List<string> playerNames = new List<string>(); // Liste des noms [cite: 4, 17]
    public string selectedGameMode; // Le jeu choisi (ex: "Petit bac") [cite: 21]
    public string selectedDifficulty = "facile"; // Difficulté par défaut 
    public int questionCount = 20; // Nombre de questions choisi 

[Header("UI Filtres")]
public TMP_Text questionCountText;
    private void Awake()
    {
        // Empêche les doublons et garde le script actif entre les scènes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- ÉCRAN 1 : GESTION DES JOUEURS ---
    public void AddPlayer(string name) 
    {
        // Limite à 10 joueurs comme indiqué sur ton screenshot 
        if (playerNames.Count < 10 && !string.IsNullOrEmpty(name)) 
        {
            playerNames.Add(name);
            Debug.Log("Joueur ajouté : " + name);
        }
    }

    public void RemovePlayer(int index) 
    {
        if (index >= 0 && index < playerNames.Count)
        {
            playerNames.RemoveAt(index);
        }
    }

    // --- ÉCRAN 2 : SÉLECTION DU JEU ---
    public void SelectGame(string gameName) 
    {
        selectedGameMode = gameName; // [cite: 21]
        Debug.Log("Jeu sélectionné : " + gameName);
        // Ici, tu ajouteras le code pour charger la scène des paramètres
    }

    // À ajouter dans votre GameManager.cs
public void SetDifficulty(int index)
{
    // Récupère le texte de l'option sélectionnée dans le Dropdown
    // Assurez-vous que les options du Dropdown correspondent EXACTEMENT à : Facile, Difficile, Hot, Aléatoire
    string[] options = {  "Aléatoire","Facile", "Difficile", "Hot"};
    selectedDifficulty = options[index];
    Debug.Log("Difficulté mise à jour : " + selectedDifficulty);
}

public void SetQuestionCount(float value)
{
    // 1. Calcul de l'arrondi au multiple de 5 (le "Snap")
    // On divise par 5, on arrondit à l'entier, et on multiplie par 5
    int snappedValue = Mathf.RoundToInt(value / 5f) * 5;

    // 2. Mise à jour de la variable de jeu
    questionCount = snappedValue;

    // 3. Mise à jour du texte affiché
    if (questionCountText != null)
    {
        questionCountText.text = snappedValue.ToString() + " questions";
    }

    Debug.Log("Nombre de questions (snapped) : " + questionCount);
}

}