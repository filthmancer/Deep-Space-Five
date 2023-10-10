using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUI : UIPanel
{
    private int levelTarget = 0;
    private int playerCount = 1;
    // Start is called before the first frame update
    void Start()
    {
        var mobile = GameManager.instance.CheckIfMobile();
        this["menu_main"]["button_players"].SetActive(!mobile);
        UI.GetPanel<UIPanel>("GuideScreen")["controls_pc"].SetActive(!mobile);
        UI.GetPanel<UIPanel>("GuideScreen")["controls_touch"].SetActive(mobile);
    }
    public void Button_MoveToLevel()
    {

    }

    public void Button_Play()
    {
        GameManager.instance.StartGame();
        this.SetActive(false);
        UI.GetPanel<GameUI>().SetActive(true);
    }
    public void Button_Guide()
    {
        var guide = UI.GetPanel<UIPanel>("GuideScreen");
        guide.SetActive(true);
        this.SetActive(false);
    }
    public void Button_Options()
    {

    }

    public void Button_PlayerCount(int sign)
    {
        playerCount += sign;
        if (playerCount > PlayerControl.controls_default.Count) playerCount = 1;
        else if (playerCount < 1) playerCount = PlayerControl.controls_default.Count;
        UpdatePlayerCount();
    }

    public void Button_Level(int sign)
    {
        levelTarget += sign;
        if (levelTarget < 0) levelTarget = GameManager.instance.Levels.Length - 1;
        if (levelTarget >= GameManager.instance.Levels.Length) levelTarget = 0;

        UpdateLevelTarget();
    }
    public void UpdateLevelTarget()
    {
        Level level = GameManager.instance.SetLevelTarget(levelTarget);
        this["menu_main"]["button_level"]["text"].GetComponent<TMPro.TextMeshProUGUI>().text = level.gameObject.name;
    }

    public void UpdatePlayerCount()
    {
        GameManager.instance.SetPlayers(playerCount);
        this["menu_main"]["button_players"]["text"].GetComponent<TMPro.TextMeshProUGUI>().text = playerCount.ToString();
        this["menu_main"]["button_players"]["button_right"].SetActive(playerCount != PlayerControl.controls_default.Count);
        this["menu_main"]["button_players"]["button_left"].SetActive(playerCount != 1);
    }

    protected override void UpdateToCurrentState(GameManager.MatchState state)
    {
        switch (state)
        {
            case GameManager.MatchState.Menu:
                this.SetActive(true);
                this["menu_main"]["button_options"].SetActive(false);
                UpdateLevelTarget();
                UpdatePlayerCount();
                break;
            case GameManager.MatchState.Lobby:
            case GameManager.MatchState.InMatch:
            case GameManager.MatchState.PostMatch:
                this.SetActive(false);
                break;
        }
    }
}
