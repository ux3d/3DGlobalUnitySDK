using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderXmlUiConnection : XmlUiConnection {
    private Slider slider;

    public Text slider_label;
    public bool instantEvents = true;

    private void Awake() {
        slider = GetComponent<Slider>();

        if (instantEvents) {
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        } else {
            EventTrigger.TriggerEvent trigger_res = new EventTrigger.TriggerEvent();
            trigger_res.AddListener(x => OnSliderValueChanged(slider.value));
            slider.gameObject.AddComponent<EventTrigger>().triggers.Add(new EventTrigger.Entry() { callback = trigger_res, eventID = EventTriggerType.PointerUp });
        }
    }

    protected override void OnXmlVariableChanged(string value) {
        float fvalue;
        if(float.TryParse(value, out fvalue)) {
            slider.value = fvalue; 
            updateLabel();
        }
    }

    private void OnSliderValueChanged(float value) {
        XmlSettings.Instance.SetValue(settingKeyname, value.ToString());
        updateLabel();
    }

    private void updateLabel() {
        if (slider_label != null) slider_label.text = slider.value.ToString();
    }

}