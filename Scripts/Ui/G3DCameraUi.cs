using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class G3DCameraUi : MonoBehaviour {
    private void OnApplicationQuit() {
        XmlSettings.Instance.SaveSettings();
    }

}