using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Reads an INI file into a dictionary
/// Only supports reading, not writing
/// Only supports one level of sections
/// </summary>
public class INIReader
{
    private Dictionary<string, string> data = new Dictionary<string, string>();

    public INIReader(string IniPath)
    {
        if (IniPath == null)
            throw new System.Exception("IniPath not specified");

        if (!File.Exists(IniPath))
            throw new System.Exception("Ini file not found");

        foreach (string line in File.ReadLines(new FileInfo(IniPath).FullName))
        {
            // skip empty lines and section headers
            if (
                line.Length == 0
                || line == "[MonitorConfiguration]"
                || (line.StartsWith("[") && line.EndsWith("]"))
            )
            {
                continue;
            }
            try
            {
                var lineData = line.Split('=');
                if (lineData.Length != 2)
                {
                    throw new System.Exception("Invalid line in ini file: " + line);
                }
                data.Add(lineData[0], lineData[1]);
            }
            catch (System.Exception e)
            {
                throw new System.Exception(
                    "Error parsing line \"" + line + "\" in ini file: " + e.Message
                );
            }
        }
    }

    public string Read(string Key)
    {
        if (Key == null)
            throw new System.Exception("Key can not be null when reading ini file.");

        if (KeyExists(Key) == false)
        {
            throw new System.Exception("Key not found in ini file.");
        }
        return data[Key];
    }

    public bool KeyExists(string Key)
    {
        return data.ContainsKey(Key);
    }
}
