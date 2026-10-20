using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ConfigLoader : MonoBehaviour
{
    public string configFilePath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + "config.cfg"; 
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Dictionary<string, string> LoadSettings()
    {
        var settings = new Dictionary<string, string>();
        if (File.Exists(configFilePath))
        {
            var lines = File.ReadAllLines(configFilePath);
            foreach (var line in lines)
            {
                if (line.Contains("="))
                {
                    var parts = line.Split('=');
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    settings[key] = value;
                }
            }
        }
        return settings;
    }
    private void GenerateConfig(List<string> settings)
    {
        using (StreamWriter writer = new StreamWriter(configFilePath))
        {
            foreach (var set in settings)
            {
                if (set.Contains("="))
                {
                    writer.WriteLine(set);
                }
            }
        }
    }
        
}
