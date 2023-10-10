using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI : UIPanel
{
    public TMPro.TextMeshProUGUI timer, countIn;
    public TMPro.TextMeshProUGUI currentScore, endScore;
    public GameObject endScoreParent;
    private Coroutine matchRoutine;
    [SerializeField] private UIPopup scorePopup;

    void Start()
    {
        this.SetActive(false);
    }

    public void AddPlayer(int num, PlayerControl p)
    {
        (this["P" + num] as PlayerUI).SetPlayer(p);
        this["P" + num].SetActive(true);
    }

    public override void UpdateUI()
    {
        if (GameManager.instance.MatchCurrentState == GameManager.MatchState.InMatch)
        {
            float minutes = Mathf.FloorToInt((GameManager.instance.GameTime) / 60);
            float seconds = Mathf.FloorToInt(GameManager.instance.GameTime % 60);
            if (seconds > 60)
                seconds = 0;
            timer.text = minutes.ToString("0") + ":" + seconds.ToString("00");
        }
        if (Input.GetKeyUp(KeyCode.Escape)) ReturnToMenu();
    }

    protected override void UpdateToCurrentState(GameManager.MatchState state)
    {
        switch (state)
        {
            case GameManager.MatchState.InMatch:
                this.SetActive(true);
                if (matchRoutine != null) StopCoroutine(matchRoutine);
                endScoreParent.SetActive(false);
                countIn.text = "";
                break;
            case GameManager.MatchState.Lobby:
                this.SetActive(true);
                matchRoutine = StartCoroutine(ShowStart());
                endScoreParent.SetActive(false);
                timer.text = "";
                break;
            case GameManager.MatchState.Menu:
                this.SetActive(false);
                if (matchRoutine != null) StopCoroutine(matchRoutine);
                endScoreParent.SetActive(false);
                break;
            case GameManager.MatchState.PostMatch:
                timer.text = "";
                this["P1"].SetActive(false);
                matchRoutine = StartCoroutine(ShowFinalScore());
                break;
        }
    }
    private IEnumerator ShowStart()
    {
        countIn.text = "READY?";
        yield return new WaitForSeconds(2.0F);
        countIn.text = "SET?";
        yield return new WaitForSeconds(2.0F);
        countIn.text = "GO!";
        GameManager.SetMatchState(GameManager.MatchState.InMatch);
    }

    private IEnumerator ShowFinalScore()
    {
        endScore.text = "";
        yield return new WaitForSeconds(1.0F);
        endScoreParent.SetActive(true);
        yield return new WaitForSeconds(1.0F);
        endScore.text = GameManager.Players[0].Score + " POINTS";
    }

    private int pointStack = 0;

    public void ShowScore(int points)
    {
        // If we are getting a negative point hit, always show it as a new hit;
        if (points < 0)
        {
            pointStack = points;
        }
        else pointStack += points;

        var tmp = scorePopup["value"].GetComponent<TMPro.TextMeshProUGUI>();

        string prefix = pointStack > 0 ? "+" : "";
        tmp.text = prefix + pointStack;

        Color col = pointStack > 0 ? Color.green : Color.red;
        tmp.color = col;
        StopCoroutine(matchRoutine);
        matchRoutine = StartCoroutine(ShowScoreUpdate());
    }

    private IEnumerator ShowScoreUpdate()
    {
        if (!scorePopup.isActive) scorePopup.StartPopup();
        yield return new WaitForSeconds(3.0F);
        scorePopup.FadeOut(0.5F);
        pointStack = 0;
    }

    public void ReturnToMenu()
    {
        GameManager.instance.ReturnToMenu();
    }
}
