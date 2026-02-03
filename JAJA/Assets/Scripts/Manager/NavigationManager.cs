using UnityEngine;
using System.Collections.Generic;
using System;
using DG.Tweening;

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance;

    [Header("Panneaux")]
    public GameObject startMenu;
    public GameObject settingsMenu;
    public GameObject gameSelectionMenu;
    public GameObject filterMenu;
    public GameObject gamePanel;
    public GameObject addQuestionMenu;
    public GameObject addedQuestionsListMenu;
    public GameObject endMenu;

    [Header("Animation Settings")]
    public float panelPopDuration = 0.4f;
    public Ease panelPopEase = Ease.OutBack;

    [Header("Effets")]
    public ParticleSystem confettiParticles;
    public ParticleSystem confettiParticlesTwo;

    private Stack<GameObject> menuStack = new Stack<GameObject>();
    public event Action<GameObject> OnMenuOpened;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Initialisation sans animation pour le premier écran
        ShowMenu(startMenu, false, false);
    }

    public void ShowMenu(GameObject menuToShow, bool keepHistory = true, bool animate = true)
    {
        if (menuToShow == null) return;

        if (keepHistory && menuStack.Count > 0)
        {
            menuStack.Peek().SetActive(false);
        }
        else
        {
            HideAll();
        }

        menuToShow.SetActive(true);
        menuStack.Push(menuToShow);

        if (animate)
        {
            menuToShow.transform.localScale = Vector3.zero;
            menuToShow.transform.DOScale(Vector3.one, panelPopDuration)
                .SetEase(panelPopEase)
                .SetUpdate(true); // Permet l'anim même si le TimeScale est à 0
        }
        else
        {
            menuToShow.transform.localScale = Vector3.one;
        }

        OnMenuOpened?.Invoke(menuToShow);

        if (confettiParticles != null)
        {
            if (menuToShow == endMenu)
            {
                confettiParticles.Play();
                confettiParticlesTwo.Play();
            }
            else
            {
                confettiParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                confettiParticlesTwo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    public void GoBack()
    {
        // Si on a plus de 1 menu, on peut faire un retour classique
        if (menuStack.Count > 1)
        {
            // On enlève le menu actuel
            GameObject currentMenu = menuStack.Pop();
            currentMenu.SetActive(false);

            // On affiche le précédent
            GameObject previousMenu = menuStack.Peek();
            previousMenu.SetActive(true);

            // Animation de pop au retour
            previousMenu.transform.localScale = Vector3.zero;
            previousMenu.transform.DOScale(Vector3.one, panelPopDuration)
                .SetEase(panelPopEase)
                .SetUpdate(true);

            OnMenuOpened?.Invoke(previousMenu);
        }
        else
        {
            // --- CAS PARTICULIER (ex: après un Rejouer) ---
            // Si la pile est vide ou n'a qu'un menu, le bouton retour renvoie au StartMenu
            Debug.Log("Pile vide ou racine, retour au menu principal.");
            OpenStartMenu();
        }
    }

    public void OpenStartMenu() => ShowMenu(startMenu, false);
    public void OpenSettings() => ShowMenu(settingsMenu);
    public void OpenGameSelection() => ShowMenu(gameSelectionMenu);
    public void OpenFilters() => ShowMenu(filterMenu);
    public void OpenGamePanel() => ShowMenu(gamePanel);
    public void OpenAddQuestion() => ShowMenu(addQuestionMenu);
    public void OpenAddedQuestionsList() => ShowMenu(addedQuestionsListMenu);
    public void OpenEndMenu() => ShowMenu(endMenu);

    private void HideAll()
    {
        startMenu.SetActive(false);
        settingsMenu.SetActive(false);
        gameSelectionMenu.SetActive(false);
        filterMenu.SetActive(false);
        gamePanel.SetActive(false);
        addQuestionMenu.SetActive(false);
        addedQuestionsListMenu.SetActive(false);
        endMenu.SetActive(false);
        menuStack.Clear();
    }

    public void ResetHistoryAndOpen(GameObject menu)
    {
        menuStack.Clear(); // Vide l'historique complet
        ShowMenu(menu, true, true); // Ouvre le nouveau menu proprement
    }
}