using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class UI
{
    public static Dictionary<string, UIPanel> Panels = new Dictionary<string, UIPanel>();
    public static T GetPanel<T>(string id = null) where T : UIPanel
    {
        if (id != null && Panels.TryGetValue(id, out UIPanel panel))
        {
            return panel as T;
        }

        var cast_panel = Panels.First(kvp => kvp.Value is T);
        if (cast_panel.Value != null) return cast_panel.Value as T;

        Debug.LogError("Could not find panel with ID " + id);
        return null;
    }

    // public static T GetElement<T>(string id = null) where T : UIElement
    // {

    // }
}

public class UIPanel : UIElement
{
    public bool IsSingleton;
    protected override void UI_Awake()
    {
        if (IsSingleton)
        {
            if (UI.Panels.ContainsKey(ID))
            {
                Debug.LogError("Multiple instances of singleton panel " + ID);
            }
            UI.Panels[ID] = this;
        }
        GameManager.instance.CurrentState_Change += UpdateToCurrentState;
    }
    protected virtual void UpdateToCurrentState(GameManager.MatchState state)
    {

    }
}
