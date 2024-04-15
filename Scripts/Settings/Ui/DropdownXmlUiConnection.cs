using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Dropdown))]
public class DropdownXmlUiConnection : XmlUiConnection {

    [Header("save the index to settings, otherwise the item value")]
    public bool indexToSettings = true;
    private Dropdown dropdown;

    private void Awake() {
        dropdown = GetComponent<Dropdown>();
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    protected override void OnXmlVariableChanged(string value) {
        if(indexToSettings)
        {
            int index = -1;
            if(int.TryParse(value, out index)) 
                if (index < dropdown.options.Count) 
                    dropdown.value = index;
        } 
        else
        {
            for (int i = 0; i < dropdown.options.Count; i++) {
                if (dropdown.options[i].text == value) {
                    dropdown.value = i;
                    break;
                }
            }
        }

        dropdown.RefreshShownValue();
    }

    private void OnDropdownValueChanged(int index) {
        XmlSettings.Instance.SetValue(settingKeyname, indexToSettings ? index.ToString() : dropdown.options[index].text);
    }

}