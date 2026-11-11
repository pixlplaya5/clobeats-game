using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class GlobalMoveY : MonoBehaviour
{
    public List<GameObject> objectsToMove;
    //public float speed = 5f;

    // cached references to avoid per-object/find overhead
    //ImprovedStrikeline sl;
    //MusicPlayer mp;
    //NoteDeletor nd;
    //NoteSpawner ns;
    //LaneInputManager lim;
    //float cachedSpeed;
    //float prefsPollInterval = 0.5f; // seconds between PlayerPrefs polls
    //float lastPrefsCheckTime = 0f;
    // Start is called before the first frame update
    /*void Start()
    {
        cachedSpeed = GameManager.Instance.GetNoteSpeed();
        speed = cachedSpeed;
        sl = FindAnyObjectByType<ImprovedStrikeline>();
        mp = FindAnyObjectByType<MusicPlayer>();
        nd = FindAnyObjectByType<NoteDeletor>();
        ns = FindAnyObjectByType<NoteSpawner>();
        lim = FindAnyObjectByType<LaneInputManager>();
    }

    void Awake()
    {
        cachedSpeed = GameManager.Instance.GetNoteSpeed();
        speed = cachedSpeed;
    }
    void OnEnable()
    {
        cachedSpeed = GameManager.Instance.GetNoteSpeed();
        speed = cachedSpeed;
    }*/

    // Update is called once per frame
    void Update()
    {
        // Poll PlayerPrefs occasionally rather than every frame
        //if (Time.time - lastPrefsCheckTime > prefsPollInterval)
        //{
        //    lastPrefsCheckTime = Time.time;
        //    float newSpeed = PlayerPrefs.GetFloat("Hyperspeed", cachedSpeed);
        //    if (Mathf.Abs(newSpeed - cachedSpeed) > 0.0001f)
        //    {
        //        cachedSpeed = newSpeed;
        //        speed = cachedSpeed;
        //    }
        //}

        //if (!isMoving) return;

        // Ensure cached global refs are present
        //if (sl == null) sl = FindAnyObjectByType<ImprovedStrikeline>();
        //if (mp == null) mp = FindAnyObjectByType<MusicPlayer>();
        //if (nd == null) nd = FindAnyObjectByType<NoteDeletor>();
        //if (ns == null) ns = FindAnyObjectByType<NoteSpawner>();
        //if (lim == null) lim = FindAnyObjectByType<LaneInputManager>();

        // Iterate backwards so we can remove destroyed/null entries safely
        // Prefer DSP-derived elapsed time (allows negative lead before audio starts).
        float currentSongSeconds = 0f;
        if (MusicPlayer.Instance != null)
        {
            currentSongSeconds = (float)MusicPlayer.Instance.GetElapsedTimeDsp();
        }
        else if (NoteSpawner.Instance != null)
        {
            currentSongSeconds = NoteSpawner.Instance.GetTimeInSecondsAtTick(NoteSpawner.Instance.currentTick);
        }

        try
        {
            for (int i = objectsToMove.Count - 1; i >= 0; --i)
            {
                var obj = objectsToMove[i];
                if (obj == null)
                {
                    objectsToMove.RemoveAt(i);
                    continue;
                }

                var t = obj.transform;
                float spacingFactor = GameManager.Instance.GetNoteSpeed();
                // If the object has a ScheduledTime component, compute its world Y directly
                // from its scheduled song time and the currentSongSeconds using NoteSpawner's
                // layout parameters so tempo changes are respected.
                var sched = obj.GetComponent<ScheduledTime>();
                if (sched != null && NoteSpawner.Instance != null)
                {
                    spacingFactor = GameManager.Instance.GetNoteSpeed();
                    float strikeY = NoteSpawner.Instance != null ? NoteSpawner.Instance.GetStrikeLineY() : 0f;
                    float targetY = strikeY + NoteSpawner.Instance.startingYPosition + NoteSpawner.Instance.startingYOffset + ((sched.scheduledSeconds - currentSongSeconds) + NoteSpawner.Instance.spawnLeadSeconds) * spacingFactor;
                    // preserve x,z
                    t.position = new Vector3(t.position.x, targetY, t.position.z);
                }
                else
                {
                    t.Translate(0f, -GameManager.Instance.GetNoteSpeed() * Time.deltaTime, 0f, Space.World);
                }

                float twend = LaneInputManager.Instance.hitWindowSeconds * spacingFactor;
                if (obj.name.ToLower().Contains("sustainvisual") || obj.GetComponent<Sustain>() != null)
                {
                    continue;
                }
                else
                {
                    if (obj.transform.position.y < -twend)
                    {
                        if (PlayerPrefs.GetInt("EnableLite", 0) == 0)
                        {
                            NotePoolManager.Instance.Return(obj);
                        }
                        else
                        {
                            Destroy(obj);
                        }
                    
                        LaneManager.Instance.UnregisterNote(obj);
                        objectsToMove.RemoveAt(i);
                        var note = obj.GetComponent<PooledNote>();
                        if (note != null)
                        {
                            ImprovedStrikeline.Instance.MissNote();
                        }
                        continue;
                    }
                }
            }
        }
        catch
        {
            
        }
        
    }
}
