using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class PlayerControl : SerializedMonoBehaviour
{
    public static Dictionary<int, KeyCode[]> controls_default = new Dictionary<int, KeyCode[]>()
    {
        {0, new KeyCode[]{
            KeyCode.A, KeyCode.D, KeyCode.LeftShift
            }},
        {1, new KeyCode[]{
            KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Space
            }},
    };
    public static Dictionary<int, KeyCode[]> controls_current;
    [TabGroup("Entity")]
    public string Name;
    public int ID;

    [TabGroup("Visuals")]
    private Transform trans;
    [TabGroup("Visuals")]
    [SerializeField]
    private Transform t_modelParent, t_rattleParent;
    [TabGroup("Visuals")]
    public Color TailColorA, TailColorB;

    [TabGroup("Visuals")]
    public ParticleSystem VelocityParticles,
                            HistoryParticles, HistoryParticles2, FuelParticles;
    [TabGroup("Visuals")]
    public GameObject VelocityParticles_Parent;
    public float RattleSpeed;

    public Vector3 ActualVelocity => velocity_actual;
    private Vector3 velocity_actual;
    private Vector3 velocity_applied;
    private Quaternion rotation_applied;

    [TabGroup("MainGroup", "Physics")]
    [TabGroup("MainGroup", "Physics")]
    /// <summary>
    /// Boost magnitude given when unit is spawned,
    /// applied perpendicular to the orbited planet
    /// </summary>
    public float StartupBoost = 30;

    public int Score = 0;

    #region Speed Cap
    [TabGroup("MainGroup/Physics/PhysicsGroup", "Cap")]
    /// <summary>
    /// Current soft cap of unit's speed, this is increased as the 
    /// unit remains close to this cap
    /// </summary>
    public float SpeedCap = 70;

    [TabGroup("MainGroup/Physics/PhysicsGroup", "Cap")]
    /// <summary>
    /// Stored initial speed cap
    /// </summary>
    private float SpeedCap_initial;

    [TabGroup("MainGroup/Physics/PhysicsGroup", "Cap")]
    public float SpeedCapIncreaseThreshold = 3.0F;
    [TabGroup("MainGroup/Physics/PhysicsGroup", "Cap")]
    public float SpeedCapDecreaseSpeed = 25F;
    [TabGroup("MainGroup/Physics/PhysicsGroup", "Cap")]
    public Vector2[] SpeedCapIncreaseSpeeds = new Vector2[3];

    public float MaxSpeedCap()
    {
        return SpeedCapIncreaseSpeeds[SpeedCapIncreaseSpeeds.Length - 1].x;
    }
    public float InitialSpeedCap()
    {
        return SpeedCap_initial;
    }
    public float SpeedRatio()
    {
        return (SpeedCap - SpeedCap_initial) / (MaxSpeedCap() - SpeedCap_initial);
    }
    #endregion

    public bool isDrifting;

    #region Gravity
    [TabGroup("MainGroup/Physics/PhysicsGroup", "Gravity")]
    public Vector3 GravityVelocity;
    [TabGroup("MainGroup/Physics/PhysicsGroup", "Gravity")]
    public Dictionary<string, float> GravityValues = new Dictionary<string, float>();
    float GravFactor = 1.0F;
    #endregion

    #region Control
    [TabGroup("MainGroup/Physics/PhysicsGroup", "Control")]
    public Vector3 LookVelocity;
    [TabGroup("MainGroup/Physics/PhysicsGroup", "Control")]
    public float ControlVelocityDecay = 0.8F;
    [TabGroup("MainGroup/Physics/PhysicsGroup", "Control")]
    public Dictionary<string, float> ControlValues = new Dictionary<string, float>();
    [SerializeField, TabGroup("MainGroup/Physics/PhysicsGroup", "Control")]
    private float Control_added_speedmax_cap = 2000;
    #endregion

    #region Charge
    [TabGroup("MainGroup/Physics/PhysicsGroup", "Charge")]
    public Vector2[] ChargeGravityThresholds = new Vector2[3];
    #endregion

    #region Fuel
    [TabGroup("MainGroup/Physics/PhysicsGroup", "Fuel")]
    public Dictionary<string, float> FuelValues = new Dictionary<string, float>();
    public float FuelBoost = 0.0F, FuelBoostActual;
    public float FuelCharging => FuelBoost / FuelValues["max"];
    #endregion

    private float immunity_mode = 0.0F;

    private List<GravityField> ActiveFields = new List<GravityField>();
    private float Gravity_added_speed = 0.0F;
    private float Control_added_speedmax = 0.1F;
    private float Control_braking_speed = 1.0F;

    private Vector3 drift, drift_start_b;
    private Vector3 turn = Vector3.zero;

    private Vector3 debugRay;
    private float velocity_applied_rate = 1.0F;
    public float drift_amount = 0.0F, turn_amount = 0.0F;
    public float driftRange;
    public float turnRate;
    private int drift_initial_dir;
    private float brake_time;
    private float Gravity_braking_multiplier = 1.0F;

    void Awake()
    {
        trans = this.transform;
        SpeedCap_initial = SpeedCap;
        GameManager.instance.CurrentState_Change += UpdateCurrentState;
        controls_current = new Dictionary<int, KeyCode[]>(controls_default);
    }

    void Update()
    {
        if (immunity_mode > 0.0F)
        {
            t_modelParent.gameObject.SetActive(Time.time % 1.0F > 0.5F);
            immunity_mode -= Time.deltaTime;
            return;
        }

        switch (GameManager.instance.MatchCurrentState)
        {
            case GameManager.MatchState.InMatch:
                UpdateVisuals();
                break;
        }
    }

    private void Velocity()
    {
        if (GameManager.instance.CheckIfMobile())
        {
            if (Input.touchCount == 0)
            {
                //isDrifting = false;
                touch_brake = false;
                touch_left = false;
                touch_right = false;
                //prev_touch_brake = false;
            }
        }
        float mag = velocity_actual.magnitude;
        CalculateControl();
        mag = velocity_actual.magnitude;
        CalculateGravity();
        mag = velocity_actual.magnitude;

        if (GameManager.d_speedcaptoggle)
        {
            //Stop the speed from going over the cap
            if (velocity_actual.magnitude > SpeedCap)
            {
                //float rate = Mathf.Lerp(0.9993F, 0.999F, (((velocity_actual.magnitude / SpeedCap) - 1.0F) / 2));
                float rate = Mathf.Lerp(0.998F, 0.9998F, 1.0f - SpeedRatio());
                velocity_actual = velocity_actual.normalized * (velocity_actual.magnitude * rate);
            }
        }
        else
        {
            velocity_actual = Vector3.ClampMagnitude(velocity_actual, SpeedCap);
        }

        // If we are within the speed cap thresholds, start increasing our speed
        var velocity_current_mag = velocity_actual.magnitude + (FuelBoost);
        if (SpeedCap - velocity_current_mag < SpeedCapIncreaseThreshold)
        {
            for (int i = 0; i < SpeedCapIncreaseSpeeds.Length; i++)
            {
                if (velocity_current_mag <= SpeedCapIncreaseSpeeds[i].x)
                {
                    SpeedCap += Time.deltaTime * SpeedCapIncreaseSpeeds[i].y * (1.0F + (FuelCharging * 0.7F));
                    break;
                }
            }
        }
        // Otherwise, decrease the cap back towards the initial amount
        else
        {
            SpeedCap = Mathf.Max(SpeedCap - (SpeedCap * Time.deltaTime * SpeedCapDecreaseSpeed), SpeedCap_initial);
        }

        velocity_applied = velocity_actual * Time.deltaTime * velocity_applied_rate;
    }

    private void CalculateControl()
    {
        Vector3 velocity_actual_normalized = velocity_actual.normalized;
        Vector3 cross = Vector3.Cross((velocity_actual).normalized, Vector3.up).normalized;

        bool brake = GetKey(2);
        bool brake_start = GetKeyDown(2);
        bool brake_end = GetKeyUp(2);
        bool left = GetKey(0);
        bool right = GetKey(1);

        ControlValues["speedmax_increase"] = Control_added_speedmax;
        ControlValues["speedmax_actual"] = ControlValues["speedmax"] + ControlValues["speedmax_increase"];

        if (brake_start)
        {
            VelocityParticles.Play();
            HistoryParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            drift_amount = turn_amount;
            brake_time = 0.0F;
        }
        else if (brake)
        {
            //REMOVED - Add fuel based on skid amount
            // if (drift_initial_dir != 0)
            // float drift_fuel_value = FuelValues["drift_increase_rate"] * Mathf.Abs(drift_amount) * Time.deltaTime;
            // FuelValues["drift_current"] = Mathf.Min(FuelValues["drift_current"] + drift_fuel_value, FuelValues["drift_max"]);
        }
        else if (brake_end)
        {
            VelocityParticles.Stop();
            HistoryParticles.Play(true);

            FuelValues["drift_current"] = FuelValues["drift_initial"];
            isDrifting = false;

            //float diminishedturnRate = 10;
            //velocity_actual_normalized = velocity_actual_normalized + (cross * drift_amount / diminishedturnRate);
            //velocity_actual = velocity_actual_normalized * velocity_actual.magnitude;

            turn_amount = drift_amount;
            drift_amount = 0.0F;
            Gravity_braking_multiplier = 1.0F;
        }
        else
        {
            //# Remove slowdown / speedup multipliers 
            if (velocity_applied_rate > 1.0F) velocity_applied_rate -= ControlValues["handbrake_decrease"];
            else if (velocity_applied_rate < 1.0F) velocity_applied_rate += ControlValues["handbrake_decrease"];
            velocity_applied_rate = Mathf.Clamp(velocity_applied_rate, ControlValues["handbrake_min"], 1.0F);
        }

        //# if the drift_turn_amount is over this threshold, we should start drifting
        float drift_threshold = 1.0F;
        // Check if we are currently drifting and shouldnt be.
        // This is used to apply the boost later
        bool overThreshold = Mathf.Abs(drift_amount) > drift_threshold;
        if (overThreshold)
        {
            isDrifting = true;
        }

        //# Increase drift turn rate the closer we are to a grav body
        float gravFactor = (GravityVelocity.magnitude / GravityValues["speedmax_actual"]);
        //# Increase drift rate closer we are to max speed cap
        float speedCapFactor = SpeedCap / MaxSpeedCap();

        driftRange = 18F + (4 * gravFactor) + (5F * speedCapFactor);

        //# The turn speeed we have while drifting
        float driftMaximum = driftRange;
        float driftMinimum = -driftRange;
        float driftIncrease = 1.5F + (0.4F * (gravFactor) + (1.2F * (speedCapFactor)));

        //# the turn speed we have while just 'turning'
        float turnRange = 0.35F + (0.1F * FuelCharging) + (1.8F * (speedCapFactor));
        float turnMaximum = turnRange;
        float turnMinimum = -turnRange;
        float turnIncrease = turnRange / 1.9F;

        int turn_dir = left ? 1 : right ? -1 : 0;
        bool noDirectionDown = !left && !right;
        bool oppositeDirectionDown = turn_amount != 0 && turn_dir != Mathf.Sign(turn_amount);
        if (noDirectionDown || oppositeDirectionDown)
        {
            turn_amount *= 0.98F;
            if (Mathf.Abs(turn_amount) < 0.01F) turn_amount = 0.0F;
            drift_amount *= 0.998F;
            if (Mathf.Abs(drift_amount) < 0.01F) drift_amount = 0.0F;
        }

        if (brake)
        {
            drift_amount = Mathf.Clamp(drift_amount + (driftIncrease * turn_dir) * Time.deltaTime, driftMinimum, driftMaximum);
            GameManager.instance.debug.text = "Drift: " + drift_amount.ToString("0.0");
            drift = (cross * drift_amount);
            brake_time += Time.deltaTime;

            float driftAbs = Mathf.Abs(drift_amount);
            float driftRatio = driftAbs / driftRange;
            velocity_actual_normalized += (drift * Time.deltaTime / 2.5F);
            velocity_actual_normalized.Normalize();

            float brakingForce = ControlValues["braking_force"];
            // add smooth braking over time
            brakingForce *= Mathf.Lerp(0.0F, 1.0F, Mathf.Min(brake_time * 2, 1.0F));
            // multiply the braking if you are also drifting;
            brakingForce *= 1 + driftRatio;
            if (left || right)
            {
                //# Apply a slowdown multiplier as you hold down the brake
                velocity_applied_rate = Mathf.Clamp(velocity_applied_rate - ControlValues["handbrake_decrease"], ControlValues["handbrake_min"], 1.0F);
                //velocity_actual = Vector3.ClampMagnitude(velocity_actual, velocity_actual.magnitude * 0.999F);
                //brakingForce = ControlValues["braking_force"] * driftRatio;
            }
            //# Apply braking force
            velocity_actual = velocity_actual_normalized * (velocity_actual.magnitude - brakingForce);



            //# If we are braking, lower gravity input to compensate for slowed actual speed
            Gravity_braking_multiplier = 1.0F - (ControlValues["braking_gravity_decrease"]);// * driftRatio);

            //TODO - visual leftover, segment to vis component
            VelocityParticles.emissionRate = 20 + (150 * (driftRatio));
            VelocityParticles.main.startLifetimeMultiplier = 0.2F + driftRatio;
            VelocityParticles_Parent.transform.localRotation = Quaternion.Euler(0, 60 * turn_dir, 0);//.LookAt(cross * turn_dir);

            LookVelocity = (velocity_actual_normalized + drift).normalized;

            //# Increase fuel based on current drift
            FuelValues["drift_current"] = FuelValues["drift_increase_rate"] * (1.0F + driftRatio * 0.4F) * Time.deltaTime;
            FuelBoost = Mathf.Clamp(FuelBoost + FuelValues["drift_current"], FuelValues["min"], FuelValues["max"]);
        }
        else
        {
            turn_amount += (turnIncrease * turn_dir) * Time.deltaTime;
            GameManager.instance.debug.text = "TURN: " + turn_amount.ToString("0.0");
            //# soft clamp the max turn
            //# in the past I tried fiddling with an even softer clamp
            //# for turning to contrast the hard clamp of drifting
            if (turn_amount > turnMaximum || turn_amount < turnMinimum)
            {
                turn_amount *= 0.95F;
            }

            //# reset 'turn' to the real velocity if there is no input
            if (turn_amount != 0.0F) turn = (cross * turn_amount);
            else turn = velocity_actual_normalized;

            //# Real turning power, based on fuel 
            float rate = 0.45F + (FuelCharging * 0.55F);

            velocity_actual_normalized += (turn * Time.deltaTime * rate);
            velocity_actual_normalized.Normalize();
            velocity_actual = velocity_actual_normalized * velocity_actual.magnitude;

            //# Apply fuel boost if there is any
            if (FuelBoost > 0.0F)
            {
                FuelBoostActual = Mathf.Lerp(FuelBoostActual, FuelBoost, Time.deltaTime * 3);
                if (turn_amount == 0.0F)
                {
                    turn = velocity_actual_normalized / 5;
                }
                if (velocity_actual.magnitude < SpeedCap)
                {
                    velocity_actual += (FuelBoostActual) * (turn) * Time.deltaTime;
                    velocity_actual = velocity_actual.normalized * (velocity_actual.magnitude * (1 + Time.deltaTime * FuelCharging));
                }

                //# do a little increase of speed cap based on fuel
                //# for some extra oomph
                SpeedCap += FuelCharging * 0.05F;
            }

            LookVelocity = (velocity_actual_normalized + turn).normalized;
        }
    }

    public void CalculateGravity()
    {
        GravityVelocity = Vector3.zero;
        if (ActiveFields.Count == 0)
        {
            return;
        }
        float gravPower = 0.0F;

        //REMOVED - Add speed based off current gravity pull
        //# This was deprecated as speed is no longer constrained to the speed cap
        //# so gravity will always add extra speed.
        //Gravity_added_speed = Mathf.Max(SpeedCap - SpeedCap_initial, 0) * GravityValues["cap_multiplier"];
        //GravityValues["speedinput_increase"] = Gravity_added_speed / 2;

        //# Increase gravity multiplier based on current speed cap.
        //# The faster you are going, the more gravity applies. 
        //# This is to compensate for much faster speeds overpowering
        //# grav force
        GravityValues["speedinput"] = Mathf.Lerp(1.0F, 1.3F, SpeedRatio());


        //# Add Gravity per field
        foreach (GravityField g in ActiveFields)
        {
            Vector3 initvel = (g.transform.position - trans.position).normalized;

            float power = g.PowerAtDist(trans.position);
            Vector3 pull = initvel * (power * (GravityValues["speedinput"]));
            GravityVelocity += (pull);

            //# Add fuel based on this fields 'fuel value'
            float fuelValue = g.FuelAtDist(trans.position);
            float rate = fuelValue * FuelValues["grav_increase_rate"];
            gravPower += rate;


            //REMOVED - Add fuel based off planet pull
            //# Fuel is now generated below off total gravity power, not per planet
            // if (g.PowerAtDist(trans.position, false) > FuelValues["grav_increase_threshold"])
            // {
            //     float rate = FuelValues["grav_increase_rate"] * (g.PowerAtDist(trans.position, false) / FuelValues["grav_increase_threshold"]);
            //     gravPower += rate;
            // }
        }

        //# Limit real max gravity. 
        //# No longer important as grav isn't as variable
        GravityValues["speedmax_actual"] = GravityValues["speedmax"] + GravityValues["speedmax_increase"];

        //# Limit gravity if the braking multiplier is in effect
        GravityVelocity *= Gravity_braking_multiplier;

        //# Little fiddle to lessen gravity as fuel is increased.
        //# Not sure of value as we're doing the same thing below!
        GravityVelocity *= (1.0F - (0.05F * FuelCharging));

        GravityVelocity = Vector3.ClampMagnitude(GravityVelocity, GravityValues["speedmax_actual"]);

        //# Increase fuel based on current gravity power
        FuelValues["grav_current"] = gravPower;
        FuelBoost = Mathf.Clamp(FuelBoost + FuelValues["grav_current"], FuelValues["min"], FuelValues["max"]);

        //# Remove a factor of the gravity based on current fuel
        GravFactor = 1.0F - (0.4F * FuelCharging);

        velocity_actual += GravityVelocity * Time.deltaTime * velocity_applied_rate * GravFactor;
    }

    public void SetField(GravityField g)
    {
        if (!ActiveFields.Contains(g)) ActiveFields.Add(g);
    }
    public void ClearField(GravityField g)
    {
        ActiveFields.Remove(g);
    }

    public void SetPosition(Vector3 pos)
    {
        trans.position = pos;
    }

    public void SetControlVel(Vector3 v, float spd)
    {
        //Control.add(Vector3.zero);
    }

    public void SetInitialVel(Vector3 v)
    {
        velocity_actual = v;
        ///drift = velocity_actual.normalized;
        drift = Vector3.zero;
        turn = Vector3.zero;
        FuelBoost = FuelValues["max"];
        ///SpeedCap = v.magnitude;
        Velocity();

        t_modelParent.transform.LookAt(transform.position + (LookVelocity.normalized * 10));
        float angleDiff = Vector3.SignedAngle(velocity_actual, t_modelParent.transform.forward, Vector3.up);
        t_modelParent.transform.rotation = t_modelParent.transform.rotation * Quaternion.Euler(0, 0, -angleDiff);
    }

    public void OnTriggerEnter(Collider c)
    {
        if (c.transform.parent == this.trans) return;
        if (c.tag == "Destroy") GameManager.instance.KillPlayer(this);
        if (c.tag == "Checkpoint") GameManager.instance.CollectCheckpoint(c.gameObject, this);

        if (c.tag == "Player" && GameManager.instance.GameMode == GameModes.CoopFive)
        {
            GameManager.instance.HighFive(this, c.transform.parent.GetComponent<PlayerControl>());
        }
    }

    #region States
    /// <summary>
    /// Player will be immune to crashing or scoring and won't move
    /// </summary>
    /// <param name="time"></param>
    public void SetImmunityMode(float time)
    {
        immunity_mode = time;
    }

    /// <summary>
    /// Apply an immediate fuel amount
    /// </summary>
    public void BurstChangeSpeedCap()
    {
        FuelBoost = Mathf.Clamp(FuelBoost + FuelValues["burst_amount"], 0.0F, FuelValues["max"]);
    }

    public void ResetVelocity()
    {
        ActiveFields.Clear();
        GravityVelocity = Vector3.zero;
        LookVelocity = Vector3.zero;
        velocity_actual = Vector3.zero;
        Gravity_added_speed = 0.0F;
        Control_added_speedmax = 0.1F;
        SpeedCap = SpeedCap_initial + 20;
        FuelValues["drift_current"] = 0.0F;//FuelValues["drift_initial"];
        FuelValues["grav_current"] = 0.0F;
        //FuelBoost = FuelValues["max"];
        drift_amount = 0.0F;
        turn_amount = 0.0F;
        isDrifting = false;
        HistoryParticles.Clear();
        HistoryParticles2.Clear();
        FuelParticles.Clear();
        Gravity_braking_multiplier = 1.0F;
        //VelocityParticles.Play();
        VelocityParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void UpdateCurrentState(GameManager.MatchState state)
    {
        switch (state)
        {
            case GameManager.MatchState.InMatch:
                immunity_mode = 0.0F;
                break;
            case GameManager.MatchState.Lobby:
                immunity_mode = 3.0F;
                break;
        }
    }

    private void UpdateVisuals()
    {
        t_modelParent.gameObject.SetActive(true);
        for (int i = 0; i < ActiveFields.Count; i++)
        {
            float radialDist = ActiveFields[i].RadialDist(trans.position);
            if (radialDist <= 0.0F)
            {
                // remove active fields that we are too far away from
                ActiveFields.RemoveAt(i);
                i--;
                continue;
            }
            else
            {
                // Cycle through charge thresholds, 
                // if the current gravity speed is higher than a threshold
                // add that thresholds charge
                for (int g = ChargeGravityThresholds.Length - 1; g > 0; g--)
                {
                    if ((1 - radialDist) > ChargeGravityThresholds[g].x)
                    {
                        //Charge.Current += ChargeGravityThresholds[g].y * Time.deltaTime;
                        break;
                    }
                }
            }
        }

        // test if unit is still visible on screen
        if (!GameManager.instance.MainCam_Bounds.Contains(trans.position))
        {
            GameManager.instance.KillPlayer(this);
        }

        Velocity();

        trans.position += velocity_applied;

        HistoryParticles.startSize = 1 + (15 * FuelCharging);
        HistoryParticles.startColor = Color.Lerp(Color.white, TailColorA, FuelCharging);
        HistoryParticles2.startSize = 5 + (40 * FuelCharging);
        HistoryParticles2.startColor = Color.Lerp(TailColorA, TailColorB, 0.1F + FuelCharging);
        FuelParticles.emissionRate = Mathf.Lerp(0, 100, FuelCharging);
        FuelParticles.startColor = TailColorB;

        if (!GetKey(2) && !isDrifting)
        {
            FuelBoost = Mathf.Clamp(FuelBoost * FuelValues["decay_multiplicative"], FuelValues["min"], FuelValues["max"]);
            FuelBoost = Mathf.Clamp(FuelBoost - FuelValues["decay_additive"] * Time.deltaTime, FuelValues["min"], FuelValues["max"]);
        }

        float rattleSpeed_actual = RattleSpeed * (ActualVelocity.magnitude / MaxSpeedCap());
        Vector2 rattle = Random.insideUnitCircle * rattleSpeed_actual;
        t_rattleParent.transform.rotation = Quaternion.Euler(rattle.x, 0.0F, rattle.y);

        t_modelParent.transform.LookAt(transform.position + (LookVelocity.normalized * 10));
        float angleDiff = Vector3.SignedAngle(velocity_actual, t_modelParent.transform.forward, Vector3.up);
        t_modelParent.transform.rotation = t_modelParent.transform.rotation * Quaternion.Euler(0, 0, -angleDiff);

        Debug.DrawRay(trans.position, LookVelocity.normalized * (100 + (FuelBoost / 5)), Color.yellow);
        Debug.DrawRay(trans.position, GravityVelocity * GravFactor * velocity_applied_rate, Color.blue);
        Debug.DrawRay(trans.position, velocity_actual / 5, Color.green);
    }
    #endregion 
    #region Input
    private bool GetKeyDown(int index)
    {
        if (GameManager.instance.CheckIfMobile())
        {
            switch (index)
            {
                // Left
                case 0:
                    if (touch_left && touch_left != prev_touch_left)
                    {
                        prev_touch_left = touch_left;
                        return true;
                    }
                    return false;
                //Right
                case 1:
                    if (touch_right && touch_right != prev_touch_right)
                    {
                        prev_touch_right = touch_right;
                        return true;
                    }
                    return false;
                //Brake
                case 2:
                    if ((touch_brake) && !prev_touch_brake)
                    {
                        prev_touch_brake = true;
                        return true;
                    }
                    return false;
            }
        }
        return Input.GetKeyDown(controls_current[ID][index]);
    }
    private bool GetKey(int index)
    {
        if (GameManager.instance.CheckIfMobile())
        {
            switch (index)
            {
                // Left
                case 0:
                    if (touch_brake && drift_amount <= 0.0F)
                        return false;
                    //if (turn_amount >= 0.0F || !touch_right)
                    return touch_left;
                //return false;
                //Right
                case 1:
                    if (touch_brake && drift_amount >= 0.0F)
                        return false;
                    //if (turn_amount <= 0.0F || !touch_left)
                    return touch_right;
                //return false;
                //Brake
                case 2:
                    return touch_brake;
            }
        }
        return Input.GetKey(controls_current[ID][index]);
    }
    private bool GetKeyUp(int index)
    {
        if (GameManager.instance.CheckIfMobile())
        {
            switch (index)
            {
                // Left
                case 0:
                    if (!touch_left && touch_left != prev_touch_left)
                    {
                        prev_touch_left = touch_left;
                        return true;
                    }
                    return false;
                //Right
                case 1:
                    if (!touch_right && touch_right != prev_touch_right)
                    {
                        prev_touch_right = touch_right;
                        return true;
                    }
                    return false;
                //Brake
                case 2:

                    if ((!touch_brake) && prev_touch_brake)
                    {
                        prev_touch_brake = false;
                        return true;
                    }
                    return false;
            }
        }
        return Input.GetKeyUp(controls_current[ID][index]);
    }

    private bool touch_left, touch_right, touch_brake;
    private bool prev_touch_left, prev_touch_right, prev_touch_brake;

    public void GetTouch(int sign, bool active)
    {
        if (sign == 1)
        {
            touch_right = active;
        }
        else if (sign == -1)
        {
            touch_left = active;
        }
        if (touch_left && touch_right) touch_brake = true;
        if (!touch_left && !touch_right) touch_brake = false;
        // Debug.Log(touch_left + " : " + touch_right + " : " + touch_brake);
    }

    #endregion
}