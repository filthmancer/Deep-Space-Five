using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CheckPoint : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro timerText;
    public GameObject model;
    public float modelRotationRate = 5;
    public float modelScaleRate = 0.3F;
    [SerializeField]
    private float deathTimer = 1.5F;
    private Vector3 model_localScale;

    public ParticleSystem collectedParticles;

    private BoxCollider bcollider;

    private float lifetime;
    [SerializeField]
    private float lifetimeMax = 10.0F;
    private int lifetime_rounded = 0;
    private Color timer_color_normal = new Color(1, 1, 1, 0.1F);
    private Color timer_color_final = new Color(1, 1, 1, 1);

    private bool isActive = true;
    void Awake()
    {
        bcollider = this.GetComponent<BoxCollider>();
    }
    // Start is called before the first frame update
    void Start()
    {
        model_localScale = model.transform.localScale;
        CalculateTimer();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isActive) return;

        model.transform.Rotate(0, 0, modelRotationRate * Time.deltaTime);
        model.transform.localScale = model_localScale * (0.8F + (1 - Mathf.Sin(Time.time)) * modelScaleRate);
        lifetime += Time.deltaTime;

        CalculateTimer();

        if (lifetime > lifetimeMax)
        {
            GameManager.instance.SpawnCheckpoint();
            Destroy(this.gameObject);
            isActive = false;
        }
    }

    private void CalculateTimer()
    {
        //# Show timer text on object
        int rounded_new = Mathf.RoundToInt(lifetimeMax - lifetime);
        if (rounded_new == 0) rounded_new = 1;
        if (rounded_new != lifetime_rounded)
        {
            lifetime_rounded = rounded_new;
            timerText.text = rounded_new.ToString();
            //# If we are in the final countdown for the checkpoint, show it more urgently
            // bool finalTime = rounded_new <= 5;
            // timerText.color = finalTime ? timer_color_final : timer_color_normal;
            timerText.color = Color.Lerp(timer_color_normal, timer_color_final, lifetime / lifetimeMax);
        }
    }

    public void Collect()
    {
        isActive = false;
        model.SetActive(false);
        bcollider.enabled = false;
        //collectedParticles.Play();
        timerText.enabled = false;
        StartCoroutine(DelayedDestroy());
    }

    IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(deathTimer);
        Object.Destroy(this.gameObject);
    }
}
