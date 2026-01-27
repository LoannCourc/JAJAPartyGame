using UnityEngine;

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance;

    [Header("Panneaux")]
    public GameObject startMenu;      // Menu principal (Joueurs)
    public GameObject settingsMenu;   // Menu paramètres
    public GameObject addQuestionMenu; // Menu ajout de questions.

    [Header("Écrans de Personnalisation")]
    public GameObject creationPanel; // L'écran avec le Dropdown "Choix du jeu"
    public GameObject listPanel;     // L'écran avec le Scroll View (Historique)

    void Awake()
    {
        Instance = this;
        ShowStartMenu(); // Par défaut, on affiche l'accueil
    }

    // Affiche le menu des joueurs (Accueil)
    public void ShowStartMenu()
    {
        HideAll();
        startMenu.SetActive(true);
    }

    // Affiche les paramètres
    public void ShowSettings()
    {
        HideAll();
        settingsMenu.SetActive(true);
    }

    // Affiche l'écran d'ajout de questions
    public void ShowAddQuestions()
    {
        HideAll();
        addQuestionMenu.SetActive(true);
    }

    public void ShowCreation()
    {
        creationPanel.SetActive(true);
        listPanel.SetActive(false);
    }

    public void ShowHistory()
    {
        creationPanel.SetActive(false);
        listPanel.SetActive(true);
    }

    // Désactive tous les panneaux pour éviter les superpositions
    private void HideAll()
    {
        if (startMenu) startMenu.SetActive(false);
        if (settingsMenu) settingsMenu.SetActive(false);
        if (addQuestionMenu) addQuestionMenu.SetActive(false);
    }
}