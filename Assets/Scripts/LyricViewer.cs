using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LyricViewer : MonoBehaviour
{
    public static LyricViewer Instance;
    public TextMeshProUGUI textMesh;
    public string currentLyricPhrase; // use full lyric phrase, not syllables
    public float xOffset = 0f;
    public float yOffset = 0f;
    [Range(0f,1f)]
    public float opacity;
    public Image background;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (textMesh)
        {
            textMesh.transform.localPosition = new Vector3(xOffset, yOffset, 0);
            textMesh.text = currentLyricPhrase;
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, opacity);
        }
        if (background)
        {
            background.color = new Color(background.color.r, background.color.g, background.color.b, opacity / 2);
        }
    }
}
