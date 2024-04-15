using UnityEngine;

public abstract class XmlUiConnection : MonoBehaviour, IXmlSettingsListener {
    public XmlSettingsKey settingKeyname;

    protected void Start() {
        XmlSettings.Instance.RegisterListener(settingKeyname, this);
    }

    protected void OnDestroy() {
        XmlSettings.Instance.UnregisterListener(settingKeyname, this);
    }

    protected abstract void OnXmlVariableChanged(string value);
    public void OnXmlValueChanged(XmlSettingsKey key, string value) {
        OnXmlVariableChanged(value);
    }

}