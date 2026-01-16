using UnityEngine;

public class Manager_Event : MonoBehaviour
{
    public void SwitchGameToChristmasTheme()
    {
        TransitionScene.Instance.TransitionCurrentScene(() =>
        {
            ChristmasEvent.Instance.ApplyChristmasTheme();
        }, 1f, 1f);
    }
}
