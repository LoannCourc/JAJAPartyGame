using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // Nécessaire pour changer d'écran

public class GameManager : MonoBehaviour
{
    // Singleton pour accéder au GameManager depuis n'importe quel script
    public static GameManager Instance;

    [Header("Données de Session")]
    public List<string> playerNames = new List<string>(); // Liste des noms [cite: 4, 17]
    public string selectedGameMode; // Le jeu choisi (ex: "Petit bac") [cite: 21]
    public string selectedDifficulty = "facile"; // Difficulté par défaut 
    public int questionCount = 20; // Nombre de questions choisi 

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

    // --- ÉCRAN 3 : FILTRES & DIFFICULTÉ ---
    public void SetDifficulty(string difficulty) 
    {
        selectedDifficulty = difficulty; // Facile, difficile ou hot 
    }

    public void SetQuestionCount(float count) 
    {
        questionCount = (int)count; // Convertit la valeur du Slider en entier 
    }
}