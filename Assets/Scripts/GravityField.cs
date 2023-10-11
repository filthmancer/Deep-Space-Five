using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

public class GravityField : MonoBehaviour
{
    [System.Flags]
    public enum Flags
    {
        StableOrbit = 1,
        CheckpointSpawnable = 2,
        PlayerSpawnable = 1 << 2
    }
    public Flags FieldFlags;
    //power of gravity pull
    public AnimationCurve GravityCurve;
    public float Mass = 1.0F;

    public AnimationCurve FuelValue;

    //Radiuses of gravity pull
    public float FarRadius = 5.0F, NearRadius = 1.0F;

    public MeshRenderer background;

    private Transform trans;

    public GravityField OrbitParent;
    public bool StableOrbit;
    private Vector3 velocity, appliedVelocity;
    [SerializeField]
    private float initialBoost;
    [SerializeField]
    private float maxVelocity = 10;
    [SerializeField]
    private float gravRatio = 0.2F;

    // Use this for initialization
    void Start()
    {
        //# generate visual dropoff for 'gravity mesh' shader
        Keyframe[] k = GravityCurve.keys;
        float fade = (FarRadius - DistAtPower(k[k.Length - 2].value)) / 32;
        background.material.SetFloat("_FadeMaximum", fade);
        trans = this.transform;

        if (OrbitParent != null)
        {
            Vector3 pvel = OrbitParent.trans.position - trans.position;
            pvel.Normalize();
            Vector3 dotdir = Vector3.Cross(pvel, Vector3.up).normalized;
            velocity = initialBoost * dotdir;
        }
    }

    void Update()
    {
        if (GameManager.instance.MatchCurrentState == GameManager.MatchState.Menu)
            return;

        //# janky update loop test to see if current players
        //# are within grav range of this body
        for (int i = 0; i < GameManager.Players.Count; i++)
        {
            if (GameManager.Players[i] == null) continue;
            float dist = Vector3.Distance(trans.position, GameManager.Players[i].transform.position);
            if (dist < FarRadius)
            {
                GameManager.Players[i].SetField(this);
            }
        }

        if (OrbitParent != null)
        {
            //# Couldn't get real orbiting to work without it falling apart over time
            if (!FieldFlags.HasFlag(Flags.StableOrbit))
            {
                Vector3 pull = (OrbitParent.transform.position - trans.position).normalized;
                float power = OrbitParent.PowerAtDist(trans.position);
                velocity += pull * power * Time.deltaTime * gravRatio;
                appliedVelocity = Vector3.ClampMagnitude(velocity, maxVelocity);
                transform.position += appliedVelocity;
            }
            //# instead lets use a simple rotatearound
            else
            {
                transform.RotateAround(OrbitParent.trans.position, Vector3.up, Time.deltaTime * maxVelocity);
            }

        }

        Debug.DrawRay(trans.position, velocity, Color.green);
    }

    public float DistAtPower(float power)
    {
        return FarRadius * power;
    }

    /// <summary>
    /// Game gravity is calculated as radial distance
    /// evaluated on a square-inverse-ish curve
    /// multiplied by planet mass
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public float PowerAtDist(Vector3 pos, bool mass_multiplied = true)
    {
        float dist = RadialDist(pos);
        if (dist > 1) return 0;
        dist = GravityCurve.Evaluate(dist);
        if (mass_multiplied) dist *= Mass;
        return dist;
    }

    /// <summary>
    /// Distance of player from planet as a ratio of its gravitational reach
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public float RadialDist(Vector3 pos)
    {
        float dist = Vector3.Distance(pos, transform.position);
        float inner = dist - NearRadius;
        float outer = FarRadius - NearRadius;
        return inner / outer;
    }

    public float FuelAtDist(Vector3 pos)
    {
        float dist = 1.0f - RadialDist(pos);
        return FuelValue.Evaluate(dist);
    }

    /// <summary>
    /// Power given based on global solar rate
    /// multiplied by mass over radial distance
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public float SolarPower(Vector3 pos)
    {
        return (GameManager.SolarRate * Mass) / RadialDist(pos);
    }

    public void RandomPlayerOrbit(ref PlayerControl p)
    {
        Vector3 pos = RandomOrbitPosition(0.45F, 0.65F);
        Vector3 pvel = transform.position - pos;
        pvel.Normalize();
        Vector3 dotdir = Vector3.Cross(pvel, Vector3.up).normalized;
        float power = PowerAtDist(pos);
        p.SetInitialVel(dotdir * (power));
        p.SetPosition(pos);
    }

    public Vector3 RandomOrbitPosition(float min = 0.0F, float max = 1.0F)
    {
        float range = FarRadius - NearRadius;
        float dist = Random.Range(NearRadius + (range * min), (FarRadius * max));
        Vector3 vel = Utility.RandomVectorInclusive(1, 0, 1).normalized;
        Vector3 pos = transform.position + (vel * dist);
        pos.y = 0.0F;
        return pos;
    }
}
