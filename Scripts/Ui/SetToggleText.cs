using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class SetToggleText : MonoBehaviour
{
    private Toggle toggle;
    public Text text;
    public string textOn, textOff;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool value)
    {
        text.text = value ? textOn : textOff;
    }
}
