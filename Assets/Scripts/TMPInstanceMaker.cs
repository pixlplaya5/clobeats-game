using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq.Expressions;
using TMPro;
using UnityEngine;

public class TMPInstanceMaker : MonoBehaviour
{
    static TMPInstanceMaker _instance;
    public static TMPInstanceMaker Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in scene
                _instance = FindAnyObjectByType<TMPInstanceMaker>();
                if (_instance == null)
                {
                    var go = new GameObject("_TMPInstanceMaker");
                    // hide from scene hierarchy during edit/play to avoid clutter
                    #if UNITY_EDITOR
                    go.hideFlags = HideFlags.HideAndDontSave;
                    #endif
                    _instance = go.AddComponent<TMPInstanceMaker>();
                }
            }
            return _instance;
        }
    }
    public List<GameObject> textObjects = new List<GameObject>();
    List<GameObject> canvases = new List<GameObject>();

    public void CreateTextObject(string text, Vector2 position, Vector3 rotation, float size)
    {
        GameObject canvasObject = new GameObject("Canvas (\"" + text + "\")", typeof(RectTransform), typeof(Canvas));
        
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        GameObject textObject = new GameObject("Text (TMP) \"" + text + "\"");
        textObject.transform.SetParent(canvas.transform, false);
        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchoredPosition = position;
        textTransform.localRotation = Quaternion.Euler(rotation);
        var tmpComp = textObject.AddComponent<TextMeshProUGUI>();
        tmpComp.text = text;
        tmpComp.fontSize = size;
        tmpComp.alignment = TextAlignmentOptions.Center;
        
        textObjects.Add(textObject);
        canvases.Add(canvasObject);
        Debug.Log("[TMPInstanceMaker.CreateTextObject] TMP Text created with text \"" + text + "\"");
    }

    public IEnumerator CreateTextObjectAtGameTime(float gameTime, string text, Vector2 position, Vector3 rotation, float size)
    {
        while (Time.time < gameTime)
        {
            yield return null;
        }
        CreateTextObject(text, position, rotation, size);
    }

    public IEnumerator CreateTextObjectAtDSPTime(double dspTime, string text, Vector2 position, Vector3 rotation, float size)
    {
        while (AudioSettings.dspTime < dspTime)
        {
            yield return null;
        }
        CreateTextObject(text, position, rotation, size);
    }

    public void DeleteAllTextObjects()
    {
        try
        {
            foreach (GameObject canvasObject in canvases)
            {
                Destroy(canvasObject);
            }
            textObjects.Clear();
            canvases.Clear();
        }
        catch 
        {
            Debug.LogWarning("[TMPInstanceMaker.DeleteTextObject] Unable to destroy all text objects.");
        }
    }
}
