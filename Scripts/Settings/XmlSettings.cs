using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

public class XmlSettings {
    private XmlSettings() {
        listeners = new Dictionary<XmlSettingsKey, List<IXmlSettingsListener>>();
        settings = new Dictionary<XmlSettingsKey, string>();
    }

    #region singleton

    private static XmlSettings instance;
    public static XmlSettings Instance {
        get {
            if (instance == null) {
                instance = new XmlSettings();
                instance.ReadSettings();
            }
            return instance;
        }
    }
    
    #endregion


    #region events

    private Dictionary<XmlSettingsKey, List<IXmlSettingsListener>> listeners;

    public void RegisterListener(XmlSettingsKey key, IXmlSettingsListener listener) {
        if (!listeners.ContainsKey(key)) listeners.Add(key, new List<IXmlSettingsListener>());
        if (!listeners[key].Contains(listener)) {
            listeners[key].Add(listener);
            listener.OnXmlValueChanged(key, GetValue(key));
        }
    }
    public void UnregisterListener(XmlSettingsKey key, IXmlSettingsListener listener) {
        if(listeners.ContainsKey(key) && listeners[key].Contains(listener)) listeners[key].Remove(listener);
    }

    private void NotifyListeners() {
        foreach(XmlSettingsKey key in listeners.Keys) NotifyListeners(key);
    }
    private void NotifyListeners(XmlSettingsKey key) {
        if (!listeners.ContainsKey(key)) return;
        if (!settings.ContainsKey(key)) return;

        string value = settings[key];
        foreach(IXmlSettingsListener listener in listeners[key]) listener.OnXmlValueChanged(key, value);
    }

    #endregion


    #region data

    private Dictionary<XmlSettingsKey, string> settings;

    public void SetValue(XmlSettingsKey key, string value) {
        if (settings.ContainsKey(key) && settings[key] == value) return;

        settings[key] = value;
        NotifyListeners(key);
    }
    public string GetValue(XmlSettingsKey key, string defaultValue=null) {
        if (!settings.ContainsKey(key)) {
            if (defaultValue != null) SetValue(key, defaultValue);
            else return null;
        }
        return settings[key];
    }

    #endregion


    #region readWrite

    private bool freshlyInitialized = false;
    public bool isFreshlyInitialized()
    {
        return freshlyInitialized;
    }

    public void SaveSettings() {
        XmlDocument document = new XmlDocument();
        document.LoadXml("<settings></settings>");

        XmlNode root = document.FirstChild;
        foreach(XmlSettingsKey key in settings.Keys) {
            var element = document.CreateElement("element");
            root.AppendChild(element);
            element.SetAttribute("key", key.ToString());
            element.SetAttribute("value", settings[key]);
        }

        string path = getSettingsPath();
        string dir = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        document.Save(path);
    }
    public void ReadSettings() {
        try {
            var newSettings = new Dictionary<XmlSettingsKey, string>();

            XmlDocument document = new XmlDocument();
            document.Load(getSettingsPath());
            XmlNode root = document.FirstChild;
            foreach (XmlElement element in root.ChildNodes) {
                XmlSettingsKey key = element.GetAttribute("key").AsXMLSettingsKey();
                if(key != XmlSettingsKey.NULL) newSettings[key] = element.GetAttribute("value");
            }
            settings = newSettings;
        } catch (Exception)
        {
            freshlyInitialized = true;
        }
    }

    private string getSettingsPath()
    {
        return Path.Combine(Path.Combine(Application.persistentDataPath, "G3D"), "settings.xml");
    }

    #endregion

}