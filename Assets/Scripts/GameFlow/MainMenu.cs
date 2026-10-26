using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : UIPanel
{
    [SerializeField]
    Button startButton;

    [SerializeField]
    Button loadButton;

    [SerializeField]
    Button quitButton;

    void Awake()
    {
        startButton.onClick.AddListener(() => MainMenuEvents.StartClicked.Invoke());
        loadButton.onClick.AddListener(() => MainMenuEvents.LoadClicked.Invoke());
        quitButton.onClick.AddListener(() => MainMenuEvents.QuitClicked.Invoke());
    }
}
