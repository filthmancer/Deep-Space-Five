using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class GameManager : SerializedMonoBehaviour
{
    private static GameManager _instance;
    public static GameManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<GameManager>();
            }
            return _instance;
        }
    }
    public static bool d_speedcaptoggle = true,
                            d_ui_speed_toggle,
                            d_ui_fuel_toggle;
    void Awake()
    {
        // instance = this;
        MainCam_Bounds = OrthographicBounds(MainCam);
    }

    public static List<PlayerControl> Players
    {
        get { return instance._Players; }
    }

    private List<PlayerControl> _Players;
    private int CreatePlayers = 1;
    public GameModes GameMode = GameModes.Checkpoints;

    public Camera MainCam;
    public AudioSource MusicTrack;
    public PlayerControl PlayerObj;
    public Level MainLevel, LevelTarget;
    public Level[] Levels;

    public float GameTime;

    public PlayerColorContainer[] Colors;
    public UnityEngine.UI.Text FPS;
    private float FPS_timer;
    private float FPS_hudRefreshRate = 1.0F;

    public static float SolarRate = 0.01F;
    public static float ScoreCooldown = 0.2F;
    private static float ScoreCooldown_current = 0.0F;

    public GravityField InitField;

    public GameObject Checkpoint;
    private GameObject CurrentCheckpoint;

    public Bounds MainCam_Bounds;

    public ParticleSystem collectedParticles;

    public enum MatchState
    {
        Menu = 0,
        Lobby = 1,
        InMatch = 2,
        PostMatch = 3
    }
    public MatchState MatchCurrentState = MatchState.Menu;

    public System.Action<MatchState> CurrentState_Change = s => { };
    public TMPro.TextMeshProUGUI debug;

    [SerializeField]
    private AudioClip[] Songs;
    private int currentMusicTrack;

    public void SetPlayers(int num)
    {
        CreatePlayers = num;
        switch (num)
        {
            case 1:
                GameMode = GameModes.Checkpoints;
                break;
            case 2:
                GameMode = GameModes.CoopFive;
                break;
        }
    }

    public static Bounds OrthographicBounds(Camera camera)
    {
        float screenAspect = (float)Screen.width / (float)Screen.height;
        float cameraHeight = camera.orthographicSize * 2;
        Bounds bounds = new Bounds(
            camera.transform.position,
            new Vector3(cameraHeight * screenAspect, 3000, cameraHeight));
        return bounds;
    }
    // Use this for initialization
    void Start()
    {
        Application.targetFrameRate = 60;
        SetMatchState(MatchState.Menu);
        currentMusicTrack = Random.Range(0, Songs.Length);
        PlayNextSong();
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) TouchButtonDown(-1);
        if (Input.GetKeyUp(KeyCode.T)) TouchButtonUp(-1);

        if (Time.unscaledTime > FPS_timer)
        {
            int fps = (int)(1f / Time.unscaledDeltaTime);
            FPS.text = "FPS: " + fps.ToString("0");
            FPS_timer = Time.unscaledTime + FPS_hudRefreshRate;
        }

        switch (MatchCurrentState)
        {
            case MatchState.InMatch:
                GameTime -= Time.deltaTime;
                if (GameTime <= 0F)
                {
                    EndGame();
                }
                break;
        }
        if (ScoreCooldown_current > 0.0F) ScoreCooldown_current -= Time.deltaTime;

        if (!Application.isEditor && !Debug.isDebugBuild) return;
        if (Input.GetKeyDown(KeyCode.Z))
            d_speedcaptoggle = !d_speedcaptoggle;

        if (Input.GetKeyDown(KeyCode.X))
            d_ui_speed_toggle = !d_ui_speed_toggle;

        if (Input.GetKeyDown(KeyCode.C))
            d_ui_fuel_toggle = !d_ui_fuel_toggle;
    }

    void PlayNextSong()
    {
        currentMusicTrack++;
        if (currentMusicTrack >= Songs.Length) currentMusicTrack = 0;
        MusicTrack.clip = Songs[currentMusicTrack];
        StartCoroutine(PlaySongRoutine());
    }

    IEnumerator PlaySongRoutine()
    {
        MusicTrack.clip.LoadAudioData();
        while (MusicTrack.clip.loadState == AudioDataLoadState.Loading)
            yield return null;
        MusicTrack.Play();
        while (MusicTrack.isPlaying || !Application.isFocused)
            yield return null;
        MusicTrack.clip.UnloadAudioData();
        PlayNextSong();
    }
    public void StartGame()
    {
        InitField = MainLevel.InitField;
        _Players = new List<PlayerControl>();
        for (int i = 0; i < CreatePlayers; i++)
        {
            PlayerControl p = CreatePlayerInOrbit(InitField);
            p.ID = i;
            p.TailColorA = Colors[i][0];
            p.TailColorB = Colors[i][1];
            p.Name = "p" + (i + 1);
            _Players.Add(p);

            UI.GetPanel<GameUI>().AddPlayer(i + 1, p);
        }

        GameTime = MainLevel.MatchTime;
        SetMatchState(MatchState.Lobby);
        if (GameMode == GameModes.Checkpoints)
            SpawnCheckpoint();
    }

    public static void SetMatchState(GameManager.MatchState state)
    {
        instance.MatchCurrentState = state;
        instance.CurrentState_Change(state);
    }

    public void EndGame()
    {
        GameObject.Destroy(CurrentCheckpoint);
        SetMatchState(MatchState.PostMatch);
    }

    public void ReturnToMenu()
    {
        GameObject.Destroy(CurrentCheckpoint);
        SetMatchState(MatchState.Menu);
        for (int i = 0; i < _Players.Count; i++)
        {
            GameObject.Destroy(_Players[i].gameObject);
        }
    }

    public void KillPlayer(PlayerControl c)
    {
        AddScore(-2, c);
        c.ResetVelocity();
        var o = MainLevel.RandomPlayerSpawnField();
        if (o)
        {
            o.RandomPlayerOrbit(ref c);
            c.SetImmunityMode(2.0F);
        }

    }

    public void Destroy(PlayerControl c)
    {
        int num = 0;
        for (int i = 0; i < _Players.Count; i++)
        {
            if (_Players[i] == c)
            {
                num = i;
            }
        }

        Destroy(c.gameObject);
        Players[num] = CreatePlayerInOrbit(InitField);
        Players[num].TailColorA = Colors[num][0];
        Players[num].TailColorB = Colors[num][1];
        Players[num].Name = "p" + (num + 1);
    }

    public PlayerControl CreatePlayerInOrbit(GravityField f)
    {
        PlayerControl p = (PlayerControl)Instantiate(PlayerObj);
        if (f.gameObject.activeSelf)
        {
            f.RandomPlayerOrbit(ref p);
        }
        else
        {
            p.transform.position = Vector3.zero;
        }

        return p;
    }

    public void SpawnCheckpoint()
    {
        var checkpointRange = Vector2.Lerp(new Vector2(0.15F, 0.5F),
                                            new Vector2(0.3F, 0.9F),
                                            Players[0].SpeedRatio());
        var newpos = MainLevel.RandomCheckpointField().RandomOrbitPosition(checkpointRange.x, checkpointRange.y);
        if (CurrentCheckpoint != null)
        {
            var oldpos = CurrentCheckpoint.transform.position;
            //# Make sure the new checkpoint is not too near to the old one,
            //# and not roughly the same distance from the centre as the old one
            System.Func<bool> testNewCheckpoint = () =>
            {
                //Debug.Log("OLDDIST: " + Vector3.Distance(newpos, oldpos) + " ---- RANGEDIST: " + Mathf.Abs(Vector3.Distance(newpos, InitField.transform.position) -
                //Vector3.Distance(oldpos, InitField.transform.position)));
                bool isTooNearOld = Vector3.Distance(newpos, oldpos) < 300;
                bool isSameRangeAsOld = Mathf.Abs(Vector3.Distance(newpos, InitField.transform.position) -
                                                Vector3.Distance(oldpos, InitField.transform.position)) < 30;
                return isTooNearOld || isSameRangeAsOld;
            };

            while (testNewCheckpoint())
            {
                newpos = MainLevel.RandomCheckpointField().RandomOrbitPosition(checkpointRange.x, checkpointRange.y);
            }
        }

        CurrentCheckpoint = GameObject.Instantiate(Checkpoint);
        CurrentCheckpoint.transform.position = newpos;
        CurrentCheckpoint.transform.position += Vector3.up;
    }

    public void CollectCheckpoint(GameObject c, PlayerControl p)
    {
        if (ScoreCooldown_current > 0.0F) return;
        if (c == CurrentCheckpoint)
        {
            if (p.isDrifting)
            {
                AddScore(2, p);
            }
            else AddScore(1, p);
            CurrentCheckpoint.GetComponent<CheckPoint>().Collect();
            collectedParticles.transform.position = CurrentCheckpoint.transform.position;
            collectedParticles.Play();
            SpawnCheckpoint();
            Players[0].BurstChangeSpeedCap();
            ScoreCooldown_current = ScoreCooldown;
        }
    }

    public void HighFive(PlayerControl a, PlayerControl b)
    {
        if (ScoreCooldown_current > 0.0F) return;
        int score = 1;

        // SPEED
        //lets brute force this.
        // calculating the collision force between two players
        // based on inverting their dot product, multiplied by
        // their combined magnitude
        float dot = Vector3.Dot(a.ActualVelocity.normalized, b.ActualVelocity.normalized);

        float multiplier = 1.1F + -dot;
        float force = a.ActualVelocity.magnitude + b.ActualVelocity.magnitude;
        float finalPower = force * multiplier;

        int speedPoints = (int)Mathf.Clamp(finalPower / 250.0F, 0.0F, 10.0F);
        score += speedPoints;

        // DRIFTING
        if (a.isDrifting) score += 2;
        if (b.isDrifting) score += 2;
        AddGlobalScore(score);

        collectedParticles.transform.position = Vector3.Lerp(a.transform.position, b.transform.position, 0.5F);
        collectedParticles.Play();

        a.BurstChangeSpeedCap();
        b.BurstChangeSpeedCap();
        ScoreCooldown_current = ScoreCooldown;
    }

    public void AddScore(int value, PlayerControl player)
    {
        switch (GameMode)
        {
            case GameModes.Checkpoints:
                AddScore_Internal(value, player);
                break;
            case GameModes.CoopFive:
                AddGlobalScore(value);
                break;
        }
    }

    public void AddScore_Internal(int value, PlayerControl player)
    {
        int newscore = Mathf.Clamp(player.Score + value, 0, 1000);
        if (newscore != player.Score)
        {
            player.Score = newscore;
            UI.GetPanel<GameUI>().ShowScore(value);
        }
    }

    public void AddGlobalScore(int value)
    {
        AddScore_Internal(value, Players[0]);
    }


    public Level SetLevelTarget(int index)
    {
        if (index < 0 || index >= Levels.Length)
        {
            Debug.LogError("Could not find level index " + index);
            return LevelTarget;
        }
        LevelTarget = Levels[index];
        if (MainLevel != null)
            GameObject.Destroy(MainLevel.gameObject);

        MainLevel = GameObject.Instantiate(LevelTarget);
        return LevelTarget;
    }

    public void TouchButtonDown(int sign)
    {
        Players[0].GetTouch(sign, true);
    }
    public void TouchButtonUp(int sign)
    {
        Players[0].GetTouch(sign, false);
    }

    public bool MobileDevMode;

#if !UNITY_EDITOR && UNITY_WEBGL
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern bool IsMobile();
#endif

    public bool CheckIfMobile()
    {
        var isMobile = MobileDevMode;

#if !UNITY_EDITOR && UNITY_WEBGL
        isMobile = IsMobile();
#endif

        return isMobile;
    }
}

[System.Serializable]
public class PlayerColorContainer
{
    public Color A, B;
    public Color this[int i] => i == 0 ? A : B;
}
