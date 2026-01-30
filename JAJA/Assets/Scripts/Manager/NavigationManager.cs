using UnityEngine;
using System.Collections.Generic;
using System;

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance;

    [Header("Panneaux")]
    public GameObject startMenu;         // Ajout des joueurs
    public GameObject settingsMenu;      // Options
    public GameObject gameSelectionMenu; // Liste des jeux
    public GameObject filterMenu;        // Filtres
    public GameObject gamePanel;         // Gameplay (questions/gorgées)
    public GameObject addQuestionMenu;   // Ajouter questions
    public GameObject addedQuestionsListMenu; // Liste historique questions

    // La pile qui mémorise l'ordre d'ouverture des menus
    private Stack<GameObject> menuStack = new Stack<GameObject>();
    public event Action<GameObject> OnMenuOpened;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // On initialise l'application sur le menu de départ
        ShowMenu(startMenu, false); 
    }

    // Fonction universelle pour afficher un menu
    // keepHistory = true permet de pouvoir revenir en arrière
    public void ShowMenu(GameObject menuToShow, bool keepHistory = true)
    {
        if (menuToShow == null) return;

        // Si on veut pouvoir revenir en arrière, on sauvegarde le menu actuel
        if (keepHistory && menuStack.Count > 0)
        {
            menuStack.Peek().SetActive(false);
        }
        else
        {
            HideAll(); // Sinon on nettoie tout
        }

        menuToShow.SetActive(true);
        menuStack.Push(menuToShow);
        OnMenuOpened?.Invoke(menuToShow);
    }

    // LA fonction magique pour tous tes boutons "Retour"
    public void GoBack()
    {
        if (menuStack.Count <= 1) 
        {
            Debug.Log("Déjà sur le menu principal");
            return;
        }

        // On enlève le menu actuel de la pile
        GameObject currentMenu = menuStack.Pop();
        currentMenu.SetActive(false);

        // On affiche le menu précédent
        GameObject previousMenu = menuStack.Peek();
        previousMenu.SetActive(true);
        OnMenuOpened?.Invoke(previousMenu);
    }

    // Raccourcis pour tes boutons dans l'Inspector
    public void OpenStartMenu() => ShowMenu(startMenu, false); // Faux car c'est la racine
    public void OpenSettings() => ShowMenu(settingsMenu);
    public void OpenGameSelection() => ShowMenu(gameSelectionMenu);
    public void OpenFilters() => ShowMenu(filterMenu);
    public void OpenGamePanel() => ShowMenu(gamePanel);
    public void OpenAddQuestion() => ShowMenu(addQuestionMenu);
    public void OpenAddedQuestionsList() => ShowMenu(addedQuestionsListMenu);

    private void HideAll()
    {
        startMenu.SetActive(false);
        settingsMenu.SetActive(false);
        gameSelectionMenu.SetActive(false);
        filterMenu.SetActive(false);
        gamePanel.SetActive(false);
        addQuestionMenu.SetActive(false);
        addedQuestionsListMenu.SetActive(false);
        
        menuStack.Clear();
    }
}