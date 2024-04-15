using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SetToggleOnValueChanged : MonoBehaviour
{
    public System.Collections.Generic.List<Toggle> toggles;
    public bool desiredState = false;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnSliderValueChanged); 
    }

    private void OnSliderValueChanged(float value)
    {
        if (toggles == null) return;

        foreach (Toggle toggle in toggles) toggle.isOn = desiredState;
    }
}
