using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleAnimate : MonoBehaviour
{
    [System.Serializable]
    public struct AnimState
    {
        public string id;
        public Vector3 scale;
        public Color color;
        public float transitionTime;
    }
    public Graphic animatedImage;
    public Transform animatedTransform;

    public List<AnimState> States;
    private Coroutine move_coroutine;
    private AnimState currentState;

    public void MoveToState(string id)
    {
        var newState = States.Find(s => s.id == id);
        if (newState.id == null)
            return;
        currentState = newState;

        if (move_coroutine != null)
            StopCoroutine(move_coroutine);

        move_coroutine = StartCoroutine(MoveToState_Routine());
    }
    private IEnumerator MoveToState_Routine()
    {
        var initialCol = animatedImage != null ? animatedImage.color : Color.white;
        var initialScale = animatedTransform != null ? animatedTransform.localScale : Vector3.one;
        var time = 0.0F;
        while ((time += Time.deltaTime) < currentState.transitionTime)
        {
            if (animatedImage != null)
            {
                animatedImage.color = Color.Lerp(initialCol, currentState.color, time / currentState.transitionTime);
            }
            if (animatedTransform != null)
            {
                animatedTransform.localScale = Vector3.Lerp(initialScale, currentState.scale, time / currentState.transitionTime);
            }
            yield return null;
        }
        if (animatedImage != null)
        {
            animatedImage.color = currentState.color;
        }
        if (animatedTransform != null)
        {
            animatedTransform.localScale = currentState.scale;
        }
    }
}
