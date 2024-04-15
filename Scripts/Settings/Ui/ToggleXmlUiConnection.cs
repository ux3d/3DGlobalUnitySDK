using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleXmlUiConnection : XmlUiConnection {
    private Toggle toggle;

    private void Awake() {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    protected override void OnXmlVariableChanged(string value) {
        bool bvalue;
        if(bool.TryParse(value, out bvalue)) {
            toggle.isOn = bvalue;
        }
    }

    private void OnToggleValueChanged(bool value) {
        XmlSettings.Instance.SetValue(settingKeyname, value.ToString());
    }

}
