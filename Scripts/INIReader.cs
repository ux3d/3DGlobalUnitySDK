using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

// based on https://stackoverflow.com/a/14906422
public class INIReader
{
    private string Path;

    public INIReader(string IniPath)
    {
        if (IniPath == null)
            throw new System.Exception("IniPath not specified");

        if (!File.Exists(IniPath))
            throw new System.Exception("Ini file not found");

        Path = new FileInfo(IniPath).FullName;
    }

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    static extern int GetPrivateProfileString(
        string Section,
        string Key,
        string Default,
        StringBuilder RetVal,
        int Size,
        string FilePath
    );

    public string Read(string Key, string Section)
    {
        if (Section == null)
            throw new System.Exception("Section can not be null when reading ini file.");
        if (Key == null)
            throw new System.Exception("Key can not be null when reading ini file.");

        if (KeyExists(Key, Section) == false)
        {
            throw new System.Exception("Key not found in ini file.");
        }

        var RetVal = new StringBuilder(255);
        GetPrivateProfileString(Section, Key, "", RetVal, 255, Path);
        return RetVal.ToString();
    }

    public bool KeyExists(string Key, string Section)
    {
        var RetVal = new StringBuilder(255);
        GetPrivateProfileString(Section, Key, "", RetVal, 255, Path);
        string value = RetVal.ToString();
        return value.Length > 0;
    }
}
