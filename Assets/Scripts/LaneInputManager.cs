using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

// Enhanced input handler: shows a hit-window visual, supports strum/chords,
// hits multiple notes in a lane within the window, and supports sustained notes.
public class LaneInputManager : MonoBehaviour
{
    public static LaneInputManager Instance;
    public float hitWindowSeconds = 0.15f; // +/- timing window in seconds

    // Optional: assign a simple transparent prefab (SpriteRenderer or UI Image) that will be
    // scaled/positioned to show the timing window at the strike line.
    public GameObject hitWindowPrefab;

    //NoteSpawner spawner;
    GameObject hitWindowInstance;

    // Tracks held frets (for chord/strum behavior)
    HashSet<int> heldLanes = new HashSet<int>();
    int _laneCount;

    int laneCount
    {
        get => _laneCount;
        set
        {
            if (_laneCount != value)
            {
                _laneCount = value;
                OnLaneHeldCountChanged();
            }
        }
    }

    // Active sustains that are currently being held after a successful hit
    class ActiveSustain { public GameObject note; public float endTime; public int lane; }
    List<ActiveSustain> activeSustains = new List<ActiveSustain>();
    //ImprovedStrikeline strikeline;

    //UIUpdater uiUpdater;
    

    public bool autoPlayEnabled = false;
    public bool showHitWindow = false;
    public bool anyFretMode = false;
    GameObject note = null;

    //MusicPlayer mp;
    public float secondsUntil;


    void Start()
    {
        Instance = this;
        //spawner = FindAnyObjectByType<NoteSpawner>();
        //mp = FindAnyObjectByType<MusicPlayer>();
        if (hitWindowPrefab != null)
        {
            hitWindowInstance = Instantiate(hitWindowPrefab, transform);
            hitWindowInstance.name = "HitWindowVisual";
        }
    }

    public void ToggleAutoplay(bool toggle)
    {
        autoPlayEnabled = toggle;
        StartCoroutine(NotificationHandler.Instance.ShowNotifImmediatelyWithTimeout("Autoplay toggled to " + toggle.ToString(), 5f));
    }
    public void ToggleAnyFret(bool toggle)
    {
        anyFretMode = toggle;
        StartCoroutine(NotificationHandler.Instance.ShowNotifImmediatelyWithTimeout("Any Fret Hack toggled to " + toggle.ToString(), 5f));
    }

    void Update()
    {
        //if (spawner == null) spawner = FindAnyObjectByType<NoteSpawner>();
        //if (strikeline == null) strikeline = FindAnyObjectByType<ImprovedStrikeline>();
        //if (uiUpdater == null) uiUpdater = FindAnyObjectByType<UIUpdater>();
        UpdateHitWindowVisual();
        UpdateActiveSustains();
        laneCount = heldLanes.Count;

        if (heldLanes.Contains(0))
        {
            if (ImprovedStrikeline.Instance != null)
            ImprovedStrikeline.Instance.HoldLane(0); // zero-based xOffset
        }
        else
        {
            if (ImprovedStrikeline.Instance != null)
            ImprovedStrikeline.Instance.ReleaseLane(0);
        }
        if (heldLanes.Contains(1))
        {
            if (ImprovedStrikeline.Instance != null)
            ImprovedStrikeline.Instance.HoldLane(1); // zero-based xOffset
        }
        else
        {
            if (ImprovedStrikeline.Instance != null)
            ImprovedStrikeline.Instance.ReleaseLane(1);
        }
        if (heldLanes.Contains(2))
        {
            if (ImprovedStrikeline.Instance != null)
            ImprovedStrikeline.Instance.HoldLane(2); // zero-based xOffset
        }
        else
        {
            if (ImprovedStrikeline.Instance != null)
            ImprovedStrikeline.Instance.ReleaseLane(2);
        }
        if (heldLanes.Contains(3))
        {
            if (ImprovedStrikeline.Instance != null)
            ImprovedStrikeline.Instance.HoldLane(3); // zero-based xOffset
        }
        else
        {
            if (ImprovedStrikeline.Instance != null)
            ImprovedStrikeline.Instance.ReleaseLane(3);
        }
        if (heldLanes.Contains(4))
        {
            if (ImprovedStrikeline.Instance != null)
            ImprovedStrikeline.Instance.HoldLane(4); // zero-based xOffset
        }
        else
        {
            if (ImprovedStrikeline.Instance != null)
            ImprovedStrikeline.Instance.ReleaseLane(4);
        }

        if (autoPlayEnabled)
        {           
            OldInputManager.Instance.denyInput = SceneManager.GetSceneByName("Gameplay").isLoaded;
            TryHitLane(0, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(1, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(2, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(3, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(4, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(7, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(8, true, NoteVisualChanger.NoteType.Forced, true);
        }
        if (OldInputManager.Instance.gamepadMode)
        {
            TryHitLane(7, true, NoteVisualChanger.NoteType.Forced, true);
        }
    }

    // Input handlers to wire from the Input System
    public void OnFretPressed(int laneIndex)
    {
        heldLanes.Add(laneIndex);
        if (OldInputManager.Instance.gamepadMode)
        {
            if (!TryHitLane(8, false, NoteVisualChanger.NoteType.Any, true))
            TryHitLane(laneIndex, false, NoteVisualChanger.NoteType.Any, true);
        }
        else if (laneIndex == 8)
        {
            TryHitLane(8, false, NoteVisualChanger.NoteType.Any, true);
        }
        else if (CanHitTapOrHopo(laneIndex))
        {
            TryHitLane(laneIndex, false, NoteVisualChanger.NoteType.Any, true);
        }
    }

    public void OnFretReleased(int laneIndex)
    {
        heldLanes.Remove(laneIndex);
        EndSustainForLane(laneIndex);
        if (OldInputManager.Instance.gamepadMode)
        {
            if (CanHitTapOrHopo(laneIndex) || laneIndex == 8)
            {
                TryHitLane(laneIndex, false, NoteVisualChanger.NoteType.FretRelease, true);
            }
        }
    }

    bool CanHitTapOrHopo(int laneIndex)
    {
        var note = LaneManager.Instance.GetNextNoteInLane(laneIndex);
        if (note == null) return false;

        var visual = note.GetComponent<NoteVisualChanger>();
        if (visual == null) return false;

        return visual.currentNoteType == NoteVisualChanger.NoteType.Tap
            || visual.currentNoteType == NoteVisualChanger.NoteType.HOPO;
    }

    bool IsChordAtCurrentTick()
    {
        if (NoteSpawner.Instance == null || heldLanes.Count < 2) return false;

        List<float> noteTimes = new List<float>();
        for (int lane = 0; lane <= 4; lane++)
        {
            var note = LaneManager.Instance.GetNextNoteInLane(lane);
            if (note == null) continue;

            var sched = note.GetComponent<ScheduledTime>();
            if (sched == null) continue;

            float scheduledSeconds = sched.scheduledSeconds;
            float noteTick = NoteSpawner.Instance.GetTickAtTimeSeconds(scheduledSeconds, true);
            float currentTick = NoteSpawner.Instance.currentTick;
            if (Mathf.Abs(noteTick - currentTick) <= 1f)
            {
                noteTimes.Add(scheduledSeconds);
            }
        }

        if (noteTimes.Count < 2) return false;
        noteTimes.Sort();

        for (int i = 1; i < noteTimes.Count; i++)
        {
            if (Mathf.Abs(noteTimes[i] - noteTimes[0]) <= hitWindowSeconds)
            {
                Debug.Log(Mathf.Abs(noteTimes[i] - noteTimes[0]) + " is less than or equal to " + hitWindowSeconds);
                return true;
            }
        }
        Debug.Log("not a chord");
        return false;
    }

    // Called when the player strums; if the upcoming note is not a chord, only the highest held fret should be strummed.
    public void OnStrum()
    {
        if (anyFretMode)
        {
            // bypasses heldLanes
            List<int> anyLanes = new List<int>(8);
            foreach (int lane in anyLanes)
            {
                if (TryHitLane(lane, false, NoteVisualChanger.NoteType.Any, true))
                {
                
                }
                else
                {
                    OnOverstrum();
                }
            }
            
        }
        else
        {
            if (heldLanes.Count == 0)
            {
                if (TryHitLane(7, false, NoteVisualChanger.NoteType.Any, true) ||
                TryHitLane(8, false, NoteVisualChanger.NoteType.Any, true))
                {
                    
                }
                else
                {
                    OnOverstrum();
                }
            }

            if (heldLanes.Contains(8))
            {
                if (TryHitLane(8, false, NoteVisualChanger.NoteType.Any, true))
                {
                    
                }
                else
                {
                    OnOverstrum();
                }
            }

            var lanes = new List<int>(heldLanes);
            if (!IsChordAtCurrentTick())
            {
                int highestLane = -1;
                foreach (var lane in lanes)
                {
                    if (highestLane < lane)
                    {
                        highestLane = lane;
                    }
                }

                if (highestLane < 0) return;
                if (TryHitLane(highestLane, false, NoteVisualChanger.NoteType.Forced, false))
                {
                    
                }
                else
                {
                    OnOverstrum();
                }
                
            }
            else
            {
                
                foreach (int lane in heldLanes)
                {
                    if (TryHitLane(lane, false, NoteVisualChanger.NoteType.Forced, true))
                    {
                        
                    }
                    else
                    {
                        OnOverstrum();
                    }
                }
            }
        }
    }

    public void OnOverstrum()
    {
        var sfx = FindAnyObjectByType<SFXPlayer>();
        sfx.PlayClip("miss");
        var ui = FindAnyObjectByType<UIUpdater>();
        ui.UpdateForNoteMiss();
    }

    // Backwards-compatible single-fret hit (e.g., mapping a single key without a strum action)
    public void OnFretHit(int laneIndex)
    {
        TryHitLane(laneIndex, false, NoteVisualChanger.NoteType.Any, true);
    }
    public void OnLaneHeldCountChanged()
    {
        
    }

    bool TryHitLane(int laneIndex, bool autoHit = false, NoteVisualChanger.NoteType noteType = NoteVisualChanger.NoteType.Forced, bool isChord = false)
    {
        if (NoteSpawner.Instance == null) return false;
        float spacingFactor = GameManager.Instance.GetNoteSpeed();
        float strikeY = NoteSpawner.Instance.GetStrikeLineY();
        // Use the same base Y as NoteSpawner so world Y <-> seconds mapping matches:
        float baseY = strikeY + NoteSpawner.Instance.startingYPosition + NoteSpawner.Instance.startingYOffset;
        note = LaneManager.Instance.GetNextNoteInLane(laneIndex);
        if (note == null) return false;
        float noteY = note.transform.position.y;
        // Prefer scheduled song time if available (more accurate and synced to audio DSP clock)
        var sched = note.GetComponent<ScheduledTime>();
        //mp = FindAnyObjectByType<MusicPlayer>();
        float currentSongSeconds = MusicPlayer.Instance != null ? (float)MusicPlayer.Instance.GetElapsedTimeDsp() : Time.time;
        var visual = note.GetComponent<NoteVisualChanger>();
            
        if (sched != null)
        {
            // scheduledSeconds is the song time when the note should be at the strike line
            secondsUntil = sched.scheduledSeconds - currentSongSeconds;
        }
        else
        {
            // Fallback to world-Y -> time mapping used by NoteSpawner
            // NoteSpawner: y = strikeY + startingYPosition + startingYOffset + (timeSeconds + spawnLeadSeconds) * spacingFactor
            // therefore timeSeconds = (y - baseY) / spacingFactor - spawnLeadSeconds
            secondsUntil = (noteY - baseY) / Mathf.Max(0.0001f, spacingFactor) - NoteSpawner.Instance.spawnLeadSeconds;
        }
        float autoHitWindowSeconds = Mathf.Min(0, hitWindowSeconds);
        if (Mathf.Abs(secondsUntil) <= hitWindowSeconds && !autoHit)
        {
            Debug.Log("PlayerHit lane " + laneIndex + " note with " + secondsUntil + " seconds until strike line. (threshold: " + hitWindowSeconds + ")");
            var sustainComp = note.GetComponent<SustainedNote>();
            if (visual != null)
            {
                if (visual.currentNoteType == noteType)
                {
                    LaneManager.Instance.UnregisterNote(note);
                }
                else if (noteType == NoteVisualChanger.NoteType.Any)
                {
                    LaneManager.Instance.UnregisterNote(note);
                }
                else
                {
                    return false;
                }
            }
            

            Color sustainColor = Color.white;

            if (sustainComp != null && sustainComp.durationSeconds > 0f)
            {
                NoteSpawner.Instance.ReturnObjectToPool(note);
                // Start sustain tracking instead of immediately returning to pool.
                activeSustains.Add(new ActiveSustain
                {
                    note = note,
                    endTime = (float)MusicPlayer.Instance.currentTimeDSP + sustainComp.durationSeconds,
                    lane = laneIndex
                });
                // Optionally: play sustain start FX / scoring events here
                if (ImprovedStrikeline.Instance != null)
                {
                    ImprovedStrikeline.Instance.HitNote(laneIndex - 2); // zero-based xOffset
                    ImprovedStrikeline.Instance.HitSustain(laneIndex - 2); // zero-based xOffset
                    ImprovedStrikeline.Instance.SLTopHold(laneIndex);
                }
                int visualLaneInd = 0;
                switch (laneIndex)
                {
                    case 0: sustainColor = Color.green; visualLaneInd = 0; break;
                    case 1: sustainColor = Color.red; visualLaneInd = 1; break;
                    case 2: sustainColor = Color.yellow; visualLaneInd = 2; break;
                    case 3: sustainColor = Color.blue; visualLaneInd = 3; break;
                    case 4: sustainColor = new Color(1f, 0.5f, 0f); visualLaneInd = 4; break;
                    case 7: sustainColor = Color.magenta; visualLaneInd = 2; break;
                    case 8: sustainColor = Color.green; visualLaneInd = 2; break;
                }
                // Spawn / show separate sustain visual managed by SustainManager
                if (SustainManager.Instance != null)
                {
                    SustainManager.Instance.StartSustain(visualLaneInd, sustainComp.durationSeconds, sustainColor, isChord);
                }
            }
            else
            {
                NoteSpawner.Instance.ReturnObjectToPool(note);
                // Optionally: play tap FX / scoring events here
                if (ImprovedStrikeline.Instance != null)
                {
                    if (laneIndex == 7)
                    {
                        ImprovedStrikeline.Instance.HitNote(7);
                        ImprovedStrikeline.Instance.SLTopHit(laneIndex);
                    }
                    else
                    {
                        ImprovedStrikeline.Instance.HitNote(laneIndex - 2); // zero-based xOffset
                        ImprovedStrikeline.Instance.SLTopHit(laneIndex);
                    }
                }
            }
            return true;
        }
        else if (secondsUntil <= autoHitWindowSeconds && autoHit)
        {
            Debug.Log("AutoHit lane " + laneIndex + " note with " + secondsUntil + " seconds until strike line. (threshold: " + autoHitWindowSeconds + ")");

            var sustainComp = note.GetComponent<SustainedNote>();
            LaneManager.Instance.UnregisterNote(note);
            Color sustainColor = Color.white;

            if (sustainComp != null && sustainComp.durationSeconds > 0f)
            {
                NoteSpawner.Instance.ReturnObjectToPool(note);
                // Start sustain tracking instead of immediately returning to pool.
                activeSustains.Add(new ActiveSustain
                {
                    note = note,
                    endTime = (float)MusicPlayer.Instance.currentTimeDSP + sustainComp.durationSeconds,
                    lane = laneIndex
                });
                // Optionally: play sustain start FX / scoring events here
                if (ImprovedStrikeline.Instance != null)
                {
                    if (laneIndex != 7) 
                    {
                        ImprovedStrikeline.Instance.HitNote(laneIndex - 2); // zero-based xOffset
                    }
                    else
                    {
                        ImprovedStrikeline.Instance.HitNote(7);
                    }
                    ImprovedStrikeline.Instance.HitSustain(laneIndex - 2); // zero-based xOffset
                    ImprovedStrikeline.Instance.SLTopHold(laneIndex);
                }
                int visualLaneInd = 0;
                switch (laneIndex)
                {
                    case 0: sustainColor = Color.green; visualLaneInd = 0; break;
                    case 1: sustainColor = Color.red; visualLaneInd = 1; break;
                    case 2: sustainColor = Color.yellow; visualLaneInd = 2; break;
                    case 3: sustainColor = Color.blue; visualLaneInd = 3; break;
                    case 4: sustainColor = new Color(1f, 0.5f, 0f); visualLaneInd = 4; break;
                    case 7: sustainColor = Color.magenta; visualLaneInd = 2; break;
                    case 8: sustainColor = Color.green; visualLaneInd = 2; break;
                }
                // Spawn / show separate sustain visual managed by SustainManager
                if (SustainManager.Instance != null)
                {
                    SustainManager.Instance.StartSustain(visualLaneInd, sustainComp.durationSeconds, sustainColor, isChord);
                }
            }
            else
            {
                NoteSpawner.Instance.ReturnObjectToPool(note);
                // Optionally: play tap FX / scoring events here
                if (ImprovedStrikeline.Instance != null)
                {
                    if (laneIndex == 7)
                    {
                        ImprovedStrikeline.Instance.HitNote(7);
                        ImprovedStrikeline.Instance.SLTopHit(laneIndex);
                    }
                    else
                    {
                        ImprovedStrikeline.Instance.HitNote(laneIndex - 2); // zero-based xOffset
                        ImprovedStrikeline.Instance.SLTopHit(laneIndex);
                    }
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    void UpdateActiveSustains()
    {
        if (activeSustains.Count == 0) return;

        for (int i = activeSustains.Count - 1; i >= 0; --i)
        {
            var s = activeSustains[i];
            if (s.note == null)
            {
                activeSustains.RemoveAt(i);
                continue;
            }

            // If sustain time elapsed, end it
            if (MusicPlayer.Instance.currentTimeDSP >= s.endTime)
            {
                //Debug.Log(s.lane + " ended");
                if (NoteSpawner.Instance != null)
                {
                    NoteSpawner.Instance.ReturnObjectToPool(s.note);
                }
                activeSustains.RemoveAt(i);
                if (ImprovedStrikeline.Instance != null)
                {
                    ImprovedStrikeline.Instance.DisableSustainSparks(s.lane - 2);
                    ImprovedStrikeline.Instance.SLTopHit(s.lane);
                }
                if (SustainManager.Instance != null)
                {
                    SustainManager.Instance.EndSustain(s.lane);
                }
            }
            else
            {
                // sustain is ongoing; scoring / FX per-frame can be handled here
                if (UIUpdater.Instance != null)
                {
                    UIUpdater.Instance.UpdateForSustainHold(UIUpdater.Instance.inStar ? Time.deltaTime * 80f : Time.deltaTime * 40f); // e.g., score for holding sustain
                }
            }
        }
    }

    void EndSustainForLane(int laneIndex)
    {
        // Ends any active sustain that belongs to the released lane (player released fret early)
        for (int i = activeSustains.Count - 1; i >= 0; --i)
        {
            if (activeSustains[i].lane == laneIndex)
            {
                var note = activeSustains[i].note;
                if (note != null && NoteSpawner.Instance != null) NoteSpawner.Instance.ReturnObjectToPool(note);
                activeSustains.RemoveAt(i);
                if (ImprovedStrikeline.Instance != null)
                {
                    ImprovedStrikeline.Instance.DisableSustainSparks(laneIndex);
                    ImprovedStrikeline.Instance.SLTopHit(laneIndex);
                }
                if (SustainManager.Instance != null)
                {
                    SustainManager.Instance.EndSustain(laneIndex);
                }
            }
        }
    }

    void UpdateHitWindowVisual()
    {
        if (hitWindowInstance == null || NoteSpawner.Instance == null) return;

        float strikeY = NoteSpawner.Instance.GetStrikeLineY();
        float spacingFactor = GameManager.Instance.GetNoteSpeed();
        
        // visual height in world units approximated by seconds * spacingFactor
        float height = hitWindowSeconds * spacingFactor;
        // position at strike line
        Vector3 position = hitWindowInstance.transform.position;
        position.y = strikeY;
        hitWindowInstance.transform.position = position;

        // Scale: assume the prefab's localScale.y == 1 corresponds to height == 1 world unit.
        Vector3 scale = hitWindowInstance.transform.localScale;
        scale.y = height;
        hitWindowInstance.transform.localScale = scale;

        if (!showHitWindow)
        {
            hitWindowInstance.SetActive(false);
        }
        else
        {
            hitWindowInstance.SetActive(true);
        }
    }
}
