using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : UIPanel
{
    [SerializeField]
    private Image Fuel, Speed, SpeedCap;
    [SerializeField]
    public TextMeshProUGUI Score;
    [SerializeField]
    private GameObject debug_parent;
    [SerializeField]
    private Text debug_velocity, debug_drift, debug_control;

    private PlayerControl _player;
    protected override void UI_Awake()
    {
        SetActive(false);
    }
    // Update is called once per frame
    public override void UpdateUI()
    {
        if (!_player)
        {
            Debug.LogWarning("Showing PlayerUI " + this.gameObject.name + " without an attached player!");
            SetActive(false);
            return;
        }

        float playerCapCurrent = _player.SpeedCap - _player.InitialSpeedCap();
        float playerCapMax = _player.MaxSpeedCap();
        float playerSpeedCurrent = _player.ActualVelocity.magnitude - _player.InitialSpeedCap();

        Fuel.fillAmount = _player.FuelCharging;

        Vector3 screenPos = GameManager.instance.MainCam.WorldToViewportPoint(_player.transform.position);

        Vector2 screenScale = new Vector2(1600, 900);

        Fuel.GetComponent<RectTransform>().anchoredPosition = new Vector2((screenScale.x) * screenPos.x, (screenScale.y) * screenPos.y);

        Speed.fillAmount = 0.05F + (playerSpeedCurrent / playerCapMax) * 0.95F;
        SpeedCap.fillAmount = 0.05F + (playerCapCurrent / playerCapMax) * 0.95F;
        Score.text = _player.Score.ToString();

        if (debug_parent.activeSelf)
        {
            debug_velocity.text = _player.ActualVelocity.magnitude.ToString("0.0") + " / " + _player.SpeedCap.ToString("0.0");
            debug_drift.text = _player.isDrifting + ": " + _player.drift_amount.ToString("0.0");
            debug_control.text = _player.turnRate.ToString("0.0");
        }

        if (GameManager.d_ui_speed_toggle != SpeedCap.gameObject.activeSelf)
        {
            SpeedCap.gameObject.SetActive(GameManager.d_ui_speed_toggle);
            Speed.gameObject.SetActive(GameManager.d_ui_speed_toggle);
        }
        if (GameManager.d_ui_fuel_toggle != Fuel.gameObject.activeSelf)
        {
            Fuel.gameObject.SetActive(GameManager.d_ui_fuel_toggle);
        }
    }

    public void SetPlayer(PlayerControl p)
    {
        this.gameObject.SetActive(true);
        _player = p;
        var col = _player.TailColorA;
        col.a = 0.3F;
        Fuel.color = col;
    }

    public void SetDebug(bool active)
    {
        debug_parent.SetActive(active);
    }
}
