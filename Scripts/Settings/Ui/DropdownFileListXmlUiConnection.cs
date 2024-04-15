using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

public class DropdownFileListXmlUiConnection : XmlUiConnection
{

    [Tooltip("folder to look in without seperator (root: StreamingAssets/G3D/)")]
    public string baseDirectory = "folder";


    private Dropdown dropdown;

    private List<string> list_paths;

    private void Awake()
    {
        dropdown = GetComponent<Dropdown>();
        list_paths = new List<string>();

        //load filenames to the gui
        string path = getPath();
        if(Directory.Exists(path))
        {
            foreach(string filepath in Directory.EnumerateFiles(path))
            {
                if (filepath.EndsWith(".meta")) continue;
                dropdown.options.Add(new Dropdown.OptionData() { image = null, text = getDropdownItemTextFromPath(filepath)});
                list_paths.Add(filepath);
            }
        } else
        {
            Directory.CreateDirectory(path);
        }

        if(XmlSettings.Instance.GetValue(settingKeyname) == null && list_paths.Count > 0)
        {
            XmlSettings.Instance.SetValue(settingKeyname, list_paths[0]);
        }

        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    protected override void OnXmlVariableChanged(string value)
    {
        string filename = getDropdownItemTextFromPath(value);

        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == filename)
            {
                dropdown.value = i;
                dropdown.RefreshShownValue();
                break;
            }
        }
    }

    private string getDropdownItemTextFromPath(string path)
    {
        return Path.GetFileName(path);
    }

    private void OnDropdownValueChanged(int value)
    {
        XmlSettings.Instance.SetValue(settingKeyname, list_paths[value]);
    }

    private string getPath()
    {
        return Path.Combine(Path.Combine(Application.streamingAssetsPath, "G3D"), $"{baseDirectory}");
    }
}
