using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameMenuManager : MonoBehaviour
{
    [Header("Références")]
    public GoogleSheetLoader sheetLoader; 
    public GameObject gameCardPrefab;     
    public Transform container;           


    [Header("Navigation")]
    public GameObject filterMenu; // L'objet parent de ton écran de filtres
    public GameObject gameSelectionMenu; // L'objet parent de ton écran de filtres
    public TMP_Text selectedGameTitle; // Le texte du titre dans l'écran filtres
    // On ne génère plus dans Start() pour éviter les erreurs de timing
    
    public void DisplayGames()
    {
        // Nettoyage de sécurité pour éviter les doublons
        foreach (Transform child in container) Destroy(child.gameObject);

        // On vérifie si les données sont prêtes dans le dictionnaire
        if (sheetLoader.gameDatabase.Count == 0)
        {
            Debug.LogWarning("La base de données est vide au moment de l'affichage !");
            return;
        }

       foreach (var game in sheetLoader.gameDatabase)
{
    GameObject card = Instantiate(gameCardPrefab, container);
    
    // 1. On vérifie le texte
    TMP_Text title = card.GetComponentInChildren<TMP_Text>();
    if (title != null) title.text = game.Key; 

    // 2. On vérifie le bouton
    Button btn = card.GetComponent<Button>();
    
    // Si le bouton n'est pas sur la racine, on le cherche dans les enfants
    if (btn == null) btn = card.GetComponentInChildren<Button>();

    if (btn != null)
    {
        string currentGameName = game.Key; // Important pour la capture de variable
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnGameSelected(currentGameName));
    }
    else
    {
        Debug.LogError($"Erreur : Le Prefab '{card.name}' n'a pas de composant Button !");
    }
}
        
        Debug.Log(sheetLoader.gameDatabase.Count + " jeux affichés.");
    }

    void OnGameSelected(string name)
    {
        GameManager.Instance.SelectGame(name);
        // Ici, activez votre écran de filtres (Difficulté / Nb questions)
        // 2. Met à jour l'UI des filtres
    selectedGameTitle.text = name;
    
    // 3. Change d'écran
    filterMenu.SetActive(true);      // Affiche les filtres
    gameSelectionMenu.SetActive(false);
    }
}