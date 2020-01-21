using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildingButtonsHandler : MonoBehaviour
{
    public Button WheelsMode;
    public Button RocketMode;
    public Button Play;
    public Button Reset;

    public LineDraw BuildController;

    void StartGame()
    {
        BuildController.IsRaceStarted = true;
    }

    void SetWheelsMode()
    {
        BuildController.CurrentBuildMode = Assets.Scripts.BuildModes.DrawingWheels;
    }

    void SetRocketMode()
    {
        BuildController.CurrentBuildMode = Assets.Scripts.BuildModes.DrawingRockets;
    }

    void ResetBuilding()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Start is called before the first frame update
    void Start()
    {
        Play.onClick.AddListener(() => StartGame());
        RocketMode.onClick.AddListener(() => SetRocketMode());
        WheelsMode.onClick.AddListener(() => SetWheelsMode());
        Reset.onClick.AddListener(() => ResetBuilding());
    }
}
