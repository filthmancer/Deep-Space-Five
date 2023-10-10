using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIElement : MonoBehaviour
{
    private CanvasGroup cgroup;
    void Awake()
    {
        if (_ID.Length > 0)
        {
            ID = _ID;
        }
        else
        {
            ID = this.gameObject.name;
        }
        var parent = this.transform.parent.GetComponent<UIElement>();
        if (parent) parent.AddUIChild(this);

        cgroup = this.transform.GetComponent<CanvasGroup>();

        UI_Awake();
    }

    protected virtual void UI_Awake()
    {

    }

    [SerializeField]
    private string _ID;
    protected string ID;
    private Dictionary<string, UIElement> children = new Dictionary<string, UIElement>();
    public bool isActive;
    public UIElement this[string val, bool nested = false]
    {
        get
        {
            if (children.TryGetValue(val, out UIElement value))
            {
                return value;
            }
            if (nested)
            {
                foreach (UIElement child in children.Values)
                {
                    UIElement e = child[val, true];
                    if (e != null) return e;
                }
            }
            Debug.LogError("Could not find UIElement " + val);
            return null;
        }
    }

    public void AddUIChild(UIElement element)
    {
        if (children.ContainsKey(element.ID))
        {
            Debug.LogError("Multiple instances of id " + element.ID + " within parent " + this.ID + ". Make sure all child IDs are unique.");
        }
        children[element.ID] = element;
    }

    public void SetActive(bool? active)
    {
        isActive = active ?? !isActive;
        //# If this element has a canvas group, use that to hide the element instead of turning it off.
        if (cgroup != null)
        {
            cgroup.alpha = !active.HasValue ?
                            (cgroup.alpha == 1 ? 0 : 1) :
                            (active.Value ? 1 : 0);
            cgroup.interactable = active ?? !cgroup.interactable;
            cgroup.blocksRaycasts = active ?? !cgroup.blocksRaycasts;
        }
        else
        {
            this.gameObject.SetActive(active ?? !this.gameObject.activeSelf);
        }
    }

    public void Show()
    {
        SetActive(true);
    }

    public void Hide()
    {
        SetActive(false);
    }


    public void Update()
    {
        if (!isActive) return;
        UpdateUI();
    }
    public virtual void UpdateUI()
    {

    }

    public void FadeOut(float time)
    {
        if (cgroup == null)
        {
            Debug.LogError("Requested fadeout on UIElement that doesn't have a canvas group!");
            return;
        }
        StartCoroutine(FadeOut_Routine(time));
    }
    private IEnumerator FadeOut_Routine(float time)
    {
        float ticker = time;
        while ((ticker -= Time.deltaTime) > 0.0F)
        {
            cgroup.alpha = (ticker / time);
            yield return null;
        }
        SetActive(false);
    }

}
