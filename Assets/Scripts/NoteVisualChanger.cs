using UnityEngine;

public class NoteVisualChanger : MonoBehaviour
{
    public enum NoteType
    {
        Forced,
        HOPO,
        Tap,
        Any,
        FretRelease
    }

    public NoteType currentNoteType;

    public GameObject noteObject;
    public GameObject hopoNoteObject;
    public GameObject fretReleaseNoteObject;



    Color _gemColor;
    [SerializeField]
    public Color gemColor
    {
        get => _gemColor;
        set
        {
            if (_gemColor != value)
            {
                _gemColor = value;
                OnGemColorChanged(value);
            }
        }
    } // used for single-colored notes

    public Color oldColor;
    public Color feverColor = Color.cyan;

    public GameObject gemColorRenderer;
    public int rendererIndex = 0;

    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    void Awake()
    {
        
    }

    void OnGemColorChanged(Color value)
    {
        oldColor = value;
    }

    // Update is called once per frame
    void Update()
    {
        gemColorRenderer.GetComponent<Renderer>().materials[rendererIndex].color = gemColor;
        UIUpdater uIUpdater = FindAnyObjectByType<UIUpdater>();
        if (uIUpdater)
        {
            if (uIUpdater.inStar)
            {
                gemColor = feverColor;
            }
            else
            {
                gemColor = oldColor;
            }
        }
        if (currentNoteType == NoteType.Forced)
        {
            if (noteObject != null)
            {
                noteObject.SetActive(true);
            }
            if (hopoNoteObject != null)
            {
                hopoNoteObject.SetActive(false);
            }
            if (fretReleaseNoteObject != null)
            {
                fretReleaseNoteObject.SetActive(false);
            }
        }
        if (currentNoteType == NoteType.HOPO)
        {
            if (noteObject != null)
            {
                noteObject.SetActive(true);
            }
            if (hopoNoteObject != null)
            {
                hopoNoteObject.SetActive(true);
            }
            
            if (fretReleaseNoteObject != null)
            {
                fretReleaseNoteObject.SetActive(false);
            }
        }
        if (currentNoteType == NoteType.Tap)
        {
            if (noteObject != null)
            {
                noteObject.SetActive(true);
            }
            if (hopoNoteObject != null)
            {
                hopoNoteObject.SetActive(false);
            }
            if (fretReleaseNoteObject != null)
            {
                fretReleaseNoteObject.SetActive(false);
            }
        }
        if (currentNoteType == NoteType.FretRelease)
        {
            if (noteObject != null)
            {
                noteObject.SetActive(false);
            }
            if (hopoNoteObject != null)
            {
                hopoNoteObject.SetActive(false);
            }

            if (fretReleaseNoteObject != null)
            {
                fretReleaseNoteObject.SetActive(true);
            }
        }
    }
}
