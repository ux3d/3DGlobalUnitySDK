using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowOnValueChanged : MonoBehaviour
{
    [Header("Slider")]
    public Slider sliderToLookAt = null;
    public float sliderValueToShowMyself = 0f;
    public bool sliderInvert = false;

    [Header("Toggle")]
    public Toggle toggleToLookAt = null;
    public bool toggleValueToShowMyself = true;

    [Header("Dropdown")]
    public Dropdown dropdownToLookAt = null;
    public List<int> dropdownIndicesToShowMyself = new List<int>();

    private void Awake()
    {
        if (sliderToLookAt != null)
        {
            sliderToLookAt.onValueChanged.AddListener(OnSliderValueChanged);
            OnSliderValueChanged(sliderToLookAt.value);
        }
        if (toggleToLookAt != null)
        {
            toggleToLookAt.onValueChanged.AddListener(OnToggleValueChanged);
            OnToggleValueChanged(toggleToLookAt.isOn);
        }
        if (dropdownToLookAt != null && dropdownIndicesToShowMyself.Count > 0)
        {
            dropdownToLookAt.onValueChanged.AddListener(OnDropdownValueChanged);
            OnDropdownValueChanged(dropdownToLookAt.value);
        }
    }

    private void OnSliderValueChanged(float value)
    {
        gameObject.SetActive(sliderInvert ? value != sliderValueToShowMyself : value == sliderValueToShowMyself);
    }

    private void OnToggleValueChanged(bool value)
    {
        gameObject.SetActive(value == toggleValueToShowMyself);
    }
    private void OnDropdownValueChanged(int index)
    {
        gameObject.SetActive(dropdownIndicesToShowMyself.Contains(index));
    }
}
