using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimpleAnim
{
    public Transform target, start, end;
    public float time;
    public AnimationCurve curve;
    public IEnumerator SimpleAnimate()
    {
        if (curve.keys.Length == 0) curve = AnimationCurve.Linear(0, 0, 1, 1);
        target.position = start.position;
        float ticker = 0.0F;
        while ((ticker += Time.deltaTime) < time)
        {
            target.position = Vector3.Lerp(start.position, end.position, curve.Evaluate(ticker / time));
            yield return null;
        }
    }
}
public class UIPopup : UIElement
{
    [SerializeField]
    private float fadeOut = 0.0F;
    public bool useSimpleAnim;
    public SimpleAnim simpleAnim;
    public void StartPopup()
    {
        this.SetActive(true);
        if (useSimpleAnim)
        {
            simpleAnim.target.gameObject.SetActive(true);
            StartCoroutine(simpleAnim.SimpleAnimate());
        }
    }

}
