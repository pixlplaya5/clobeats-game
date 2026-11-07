using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugGUI : MonoBehaviour
{
    public string guiText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 36;
        style.normal.textColor = Color.white;
        style.wordWrap = true;
    
        GUI.Label(new Rect(10, 10, Display.main.renderingWidth, Display.main.renderingHeight), guiText, style);
    }
}
