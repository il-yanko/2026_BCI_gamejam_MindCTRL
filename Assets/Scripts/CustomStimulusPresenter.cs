using UnityEngine;
using UnityEngine.Events;
using BCIEssentials.Stimulus.Presentation.Standard;

public class CustomStimulusPresenter : ColourToggleStimulusPresenter
{
    public UnityEvent OnSelected;
    public override void Select()
    {
        Debug.Log("Custom stimulus selected!");
        OnSelected.Invoke();
    }
}
