using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Text;
using System.Text.RegularExpressions;
using System;
using UnityEngine.Rendering.PostProcessing;

public class VenueAnimationPlayer : MonoBehaviour
{
    public static VenueAnimationPlayer Instance { get; private set; }
    public enum AnimationType
    {
        JSON,
        MIDI
    }
    public AnimationType type = AnimationType.MIDI;

    public GameObject mainCamera = null;
    public GameObject highwayCamera = null;
    public string cameraAnimationFile;
    public float currentTick = -1f;
    public float currentRealtimeSecs = -1f;
    NoteSpawner ns;

    
    [Header("Haptic Beat Impact")]
    public bool hapticOnEveryFourthNote = false;
    public bool hapticToggle
    {
        get => hapticOnEveryFourthNote;
        set{
            if (hapticOnEveryFourthNote != value)
            {
                hapticOnEveryFourthNote = value;
                OnHapticChanged(value);
            }
        }
    }
    [Range(0f, 1f)] public float hapticLeftImpact = 1f;
    [Range(0f, 1f)] public float hapticRightImpact = 1f;
    [Range(0.01f, 0.5f)] public float hapticAttack = 0.08f;
    [Range(0.01f, 1f)] public float hapticRelease = 0.35f;
    public float hapticPulseDuration = 0.05f;
    public int hapticPulseDivision = 1;
    Coroutine hapticCoroutine;
    OldInputManager oldInputManager;
    [Header("Chromatic Aberration Beat Impact")]
    public bool chromaticAberrationOnQuarterNotes = false;
    public bool chrabrToggle
    {
        get => chromaticAberrationOnQuarterNotes;
        set {
            if (chromaticAberrationOnQuarterNotes != value)
            {
                chromaticAberrationOnQuarterNotes = value;
                OnChrAbrChanged(value);
            }
        }
    }
    [Range(0f, 1f)] public float chromaticAberrationImpact = 1f;
    [Range(0.01f, 0.5f)] public float chromaticAberrationAttack = 0.08f;
    [Range(0.01f, 1f)] public float chromaticAberrationRelease = 0.35f;
    public void StartHapticPulse()
    {
        StopHapticPulse();
        hapticCoroutine = StartCoroutine(HapticPulseRoutine());
    }

    public void StopHapticPulse()
    {
        if (hapticCoroutine != null)
        {
            StopCoroutine(hapticCoroutine);
            hapticCoroutine = null;
        }

        SendHaptic(0f, 0f, 0f);
    }

    IEnumerator HapticPulseRoutine()
    {
        float nextFourthNoteTick = -1f;

        while (true)
        {
            if (ns == null)
            {
                ns = FindAnyObjectByType<NoteSpawner>();
                yield return null;
                continue;
            }

            float current = Mathf.Max(0f, ns.currentTick);
            float quarterTicks = ns.GetTicksPerQuarterNoteAtTick(current);
            float fourthNoteLength = Mathf.Max(1, quarterTicks * hapticPulseDivision);

            if (nextFourthNoteTick <= current)
            {
                nextFourthNoteTick = ((current / fourthNoteLength) + 1) * fourthNoteLength;
            }

            while (ns.currentTick < nextFourthNoteTick)
            {
                yield return null;
            }

            quarterTicks = ns.GetTicksPerQuarterNoteAtTick(nextFourthNoteTick);
            float attackTicks = Mathf.Max(1f, quarterTicks * hapticAttack);
            float releaseTicks = Mathf.Max(1f, quarterTicks * hapticRelease);
            float impactStartTick = ns.currentTick;

            while (ns.currentTick < impactStartTick + attackTicks)
            {
                float t = Mathf.Clamp01((ns.currentTick - impactStartTick) / attackTicks);
                float amount = Mathf.Lerp(0f, 1f, t);
                SendHaptic(hapticLeftImpact * amount, hapticRightImpact * amount, hapticPulseDuration);
                yield return null;
            }

            float releaseStartTick = ns.currentTick;
            while (ns.currentTick < releaseStartTick + releaseTicks)
            {
                float t = Mathf.Clamp01((ns.currentTick - releaseStartTick) / releaseTicks);
                float amount = Mathf.Lerp(1f, 0f, t);
                SendHaptic(hapticLeftImpact * amount, hapticRightImpact * amount, hapticPulseDuration);
                yield return null;
            }

            SendHaptic(0f, 0f, 0f);
            nextFourthNoteTick += fourthNoteLength;
        }
    }

    void SendHaptic(float leftMotor, float rightMotor, float duration)
    {
        if (oldInputManager == null)
        {
            oldInputManager = FindAnyObjectByType<OldInputManager>();
        }

        if (oldInputManager != null)
        {
            oldInputManager.TriggerVibration(
                Mathf.Clamp01(leftMotor),
                Mathf.Clamp01(rightMotor),
                Mathf.Max(0f, duration));
        }
    }
    Coroutine chromaticAberrationCoroutine;
    ChromaticAberration chromaticAberration;

    // --- Scripting support ---
    [System.Serializable]
    public class ScriptDef { public string name; public string body; }
    [System.Serializable]
    public class AnimDef { public AnimationClip clip; }

    [System.Serializable]
    public class ScriptEvent { public float tick; public string scriptName; public string[] args; [System.NonSerialized] public bool executed; }

    public List<ScriptDef> scripts = new List<ScriptDef>();
    public List<AnimationClip> preMadeAnims = new List<AnimationClip>();
    public List<ScriptEvent> scriptEvents = new List<ScriptEvent>();

    // function registry: name -> action(args)
    Dictionary<string, Action<List<string>>> fnRegistry = new Dictionary<string, Action<List<string>>>();
    float lastSecondForScripts = 0f;
    float lastTickForMIDICues = 0f;
    float lastTickForLyrics = 0f;

    [System.Serializable]
    public class ScriptBundle { public ScriptDef[] scripts; public ScriptEvent[] scriptEvents; }


    void Start()
    {
        ns = FindAnyObjectByType<NoteSpawner>();
        if (chromaticAberrationOnQuarterNotes)
        {
            StartChromaticAberrationPulse();
        }
        if (hapticOnEveryFourthNote)
        {
            StartHapticPulse();
        }
    }
    void OnChrAbrChanged(bool yes)
    {
        if (yes)
        {
            StartChromaticAberrationPulse();
        }
        else
        {
            StopChromaticAberrationPulse();
        }
    }
    void OnHapticChanged(bool yes)
    {
        if (yes)
        {
            StartHapticPulse();
        }
        else
        {
            StopHapticPulse();
        }
    }

    void OnDisable()
    {
        StopChromaticAberrationPulse();
        StopHapticPulse();
    }

    public void StartChromaticAberrationPulse()
    {
        StopChromaticAberrationPulse();
        chromaticAberrationCoroutine = StartCoroutine(ChromaticAberrationPulseRoutine());
    }

    public void StopChromaticAberrationPulse()
    {
        if (chromaticAberrationCoroutine != null)
        {
            StopCoroutine(chromaticAberrationCoroutine);
            chromaticAberrationCoroutine = null;
        }

        SetChromaticAberration(0f, false);
    }

    IEnumerator ChromaticAberrationPulseRoutine()
    {
        while (true)
        {
            if (ns == null)
            {
                ns = FindAnyObjectByType<NoteSpawner>();
                yield return null;
                continue;
            }

            float beatTicks = ns.GetTicksPerQuarterNoteAtTick(Mathf.Max(0, ns.currentTick));
            float nextBeatTick = (ns.currentTick / beatTicks + 1) * beatTicks;

            while (ns.currentTick < nextBeatTick)
            {
                yield return null;
            }

            beatTicks = ns.GetTicksPerQuarterNoteAtTick(nextBeatTick);
            float attackTicks = Mathf.Max(1f, beatTicks * chromaticAberrationAttack);
            float releaseTicks = Mathf.Max(1f, beatTicks * chromaticAberrationRelease);

            SetChromaticAberration(0f, true);
            float impactStartTick = ns.currentTick;
            while (ns.currentTick < impactStartTick + attackTicks)
            {
                float t = Mathf.Clamp01((ns.currentTick - impactStartTick) / attackTicks);
                SetChromaticAberration(Mathf.Lerp(0f, chromaticAberrationImpact, t), true);
                yield return null;
            }

            float releaseStartTick = ns.currentTick;
            while (ns.currentTick < releaseStartTick + releaseTicks)
            {
                float t = Mathf.Clamp01((ns.currentTick - releaseStartTick) / releaseTicks);
                SetChromaticAberration(Mathf.Lerp(chromaticAberrationImpact, 0f, t), true);
                yield return null;
            }

            SetChromaticAberration(0f, false);
        }
    }

    void SetChromaticAberration(float intensity, bool enabled)
    {
        if (mainCamera == null) return;
        if (chromaticAberration == null)
        {
            PostProcessVolume volume = mainCamera.GetComponent<PostProcessVolume>();
            if (volume == null || volume.profile == null || !volume.profile.TryGetSettings(out chromaticAberration))
            {
                return;
            }
        }

        chromaticAberration.enabled.overrideState = true;
        chromaticAberration.enabled.value = enabled;
        chromaticAberration.intensity.overrideState = true;
        chromaticAberration.intensity.value = Mathf.Clamp01(intensity);
    }

    public void TryToggleCamera(bool toggle)
    {
        try
        {
            if (mainCamera != null)
            {
                mainCamera.SetActive(toggle);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[VenueAnimationPlayer.TryToggleCamera] Failed to toggle venue camera: " + ex.Message);
        }
        
    }

    public void TryToggleHighwayCam(bool toggle)
    {
        try
        {
            if (highwayCamera)
            {
                highwayCamera.SetActive(toggle);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[VenueAnimationPlayer.TryToggleHighwayCam] Failed to toggle highway camera: " + ex.Message);
        }
    }
    public void TryCueCamAnim(GameObject camera, AnimationClip clip)
    {
        try
        {
            if (camera)
            {
                if (clip)
                {
                    Animation animation = camera.GetComponent<Animation>();
                    if (animation)
                    {
                        animation.clip = clip;
                        animation.Play(PlayMode.StopAll);
                    }
                    else
                    {
                        animation = camera.AddComponent<Animation>();
                        animation.clip = clip;
                        animation.Play(PlayMode.StopAll);
                    }
                }
            }

        }
        catch (Exception ex)
        {
            Debug.LogError("[VenueAnimationPlayer.TryCueCamAnim] Failed to cue camera animation: " + ex.Message);
        }
    }
    public void TryCueCamAnimCont(string cue)
    {
        if (highwayCamera)
        {
            Animator animator = highwayCamera.GetComponent<Animator>();
            if (animator)
            {
                try
                {
                    animator.Play(cue);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[VenueAnimationPlayer.TryCueCamAnimCont(\"" + cue + "\")] Unable to play state: " + ex.Message);
                }
            }
        }
    }

    // not recommended
    public GameObject TrySpawnNewCamera(int ID, AnimationClip animationClip, Vector3 position, Vector3 rotation, bool includeFrameLimit = true, int framerateLimit = 60)
    {
        try
        {
            GameObject cameraObj = new GameObject("VenueCamera_" + ID);
            Camera cam = cameraObj.AddComponent<Camera>();
            cameraObj.transform.position = position;
            cameraObj.transform.rotation = Quaternion.Euler(rotation);
            if (includeFrameLimit)
            {
                CameraEffects effects = cameraObj.AddComponent<CameraEffects>();
                effects.fps = framerateLimit;
            }
            if (animationClip)
            {
                Animation camAnim = cameraObj.AddComponent<Animation>();
                camAnim.clip = animationClip;
                camAnim.AddClip(animationClip, animationClip.name);
                camAnim.Play(animationClip.name);
            }
            return cameraObj;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[VenueAnimationPlayer.TrySpawnNewCamera] Failed to spawn camera: " + ex.Message);
            return null;
        }
    }

    public void ReturnCameraToDefaultPosition(Vector3 pos, Vector3 rot)
    {
        if (mainCamera)
        {
            mainCamera.transform.position = pos;
            mainCamera.transform.rotation = Quaternion.Euler(rot);
        }
    }

    public void TryDeleteSpawnedCamera(int ID)
    {
        try
        {
            GameObject cameraObj = GameObject.Find("VenueCamera_" + ID);
            if (cameraObj)
            {
                Destroy(cameraObj);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[VenueAnimationPlayer.TryDeleteSpawnedCamera] Failed to delete spawned camera: " + ex.Message);
        }
    }

    public void Load()
    {
        try
        {
            if (type == AnimationType.JSON && cameraAnimationFile != null || cameraAnimationFile != "")
            {
                string path = Path.GetFullPath(cameraAnimationFile);
                if (File.Exists(path))
                {
                    if (Path.GetFileNameWithoutExtension(path) == "scripts")
                    {
                        LoadScriptsFromFile(path);
                    }
                } 
            }
            else
            {
                return;
            }
        }
        catch
        {
            return;
        }
        
    }

    public void Unload()
    {
        try
        {
            foreach (var ev in scriptEvents) 
            {
                ev.executed = false;
            }
            scripts.Clear();
            scriptEvents.Clear();
            cameraAnimationFile = string.Empty;
        }
        catch
        {
            
        }
    }
    

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RegisterBuiltins();
    }


    

    // Load scripts and events from a JSON file (runtime)
    public void LoadScriptsFromFile(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("VenueAnimationPlayer: script file not found: " + path);
            return;
        }
        string json = File.ReadAllText(path);
        try
        {
            var bundle = JsonUtility.FromJson<ScriptBundle>(json);
            if (bundle != null)
            {
                scripts = bundle.scripts != null ? bundle.scripts.ToList() : new List<ScriptDef>();
                scriptEvents = bundle.scriptEvents != null ? bundle.scriptEvents.ToList() : new List<ScriptEvent>();
                // reset executed flags
                foreach (var ev in scriptEvents) ev.executed = false;
                Debug.Log("VenueAnimationPlayer: loaded " + scripts.Count + " scripts and " + scriptEvents.Count + " events from " + path);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("VenueAnimationPlayer: Failed to parse scripts file: " + ex.Message);
        }
    }

    
    void Update()
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (SceneManager.GetSceneByName("v_PJCBPV").isLoaded)
        {
            if (!mainCamera)
            {
                mainCamera = MMDCam.Instance.gameObject;
            }
        }
        if (SceneManager.GetSceneByName("Gameplay").isLoaded)
        {
            if (!highwayCamera)
            {
                highwayCamera = GameObject.Find("Highway_cam");
            }
        }
        
        
        if (ns == null) ns = FindAnyObjectByType<NoteSpawner>();
        if (ns != null)
        {
            currentTick = ns.currentTick; // follow tempo map for MIDI venue cues
        }
        MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
        if (musicPlayer)
        {
            currentRealtimeSecs = (float)musicPlayer.currentTimeDSP;
        }

        // scripting: detect seek backwards and reset executed flags
        if (scriptEvents != null && scriptEvents.Count > 0)
        {
            if (currentRealtimeSecs < lastSecondForScripts)
            {
                foreach (var ev in scriptEvents) ev.executed = false;
            }

            foreach (var ev in scriptEvents)
            {
                if (!ev.executed && currentRealtimeSecs >= ev.tick)
                {
                    ExecuteScriptEvent(ev);
                    ev.executed = true;
                }
            }

            lastSecondForScripts = currentRealtimeSecs;
        }

        if (gameManager.currentSongVenueCueEvents != null && gameManager.currentSongVenueCueEvents.Count > 0)
        {
            if (currentTick < lastTickForMIDICues)
            {
                foreach (var ev in gameManager.currentSongVenueCueEvents) ev.passed = false;
            }

            foreach (var ev in gameManager.currentSongVenueCueEvents)
            {
                if (!ev.passed && currentTick >= ev.spawnTime)
                {
                    ExecuteScriptByString(ev.value);
                    ev.passed = true;
                }
            }

            lastTickForMIDICues = currentTick;
        }

        if (gameManager.currentSongLyrics != null && gameManager.currentSongLyrics.Count > 0)
        {
            if (currentTick < lastTickForLyrics)
            {
                foreach (var ev in gameManager.currentSongLyrics) ev.passed = false;
            }

            foreach (var ev in gameManager.currentSongLyrics)
            {
                if (!ev.passed && currentTick >= ev.spawnTick)
                {
                    if (ev.value.StartsWith("[")) continue; // ignore "[play]" notes
                    //Debug.Log(ev.value + " (Freq=" + ev.sungNote + ", StartTick=" + ev.spawnTick + ", EndTick=" + (ev.spawnTick + ev.length) + ")"); // log lyrics to console for now
                    //Debug.Log($"$beep,{ev.sungNote},{ev.lengthMs},1.0,Sine"); // custom debug console synth
                    //System.Threading.Thread playThread = new System.Threading.Thread(() => NAudioBeepSynth.PlayToneNAudio(ev.sungNote, (int)ev.lengthMs, 1.0));
                    //playThread.IsBackground = true;
                    //playThread.Start();
                    ev.passed = true;
                }
            }

            lastTickForLyrics = currentTick;
        }

    }

    // --- Scripting runtime methods ---
    void RegisterBuiltins()
    {
        fnRegistry.Clear();
        // regular scripts
        fnRegistry["SetPVCamPosition"] = (args) =>
        {
            float x = ParseF(args, 0), y = ParseF(args, 1), z = ParseF(args, 2);
            MMDCam.Instance.SetPosition(new Vector3(x, y, z));
        };
        fnRegistry["SetPVCamRotation"] = (args) =>
        {
            float x = ParseF(args, 0), y = ParseF(args, 1), z = ParseF(args, 2);
            MMDCam.Instance.SetRotation(new Vector3(x, y, z));
        };
        fnRegistry["SetPVCamDistance"] = (args) =>
        {
            float z = ParseF(args, 0);
            MMDCam.Instance.SetDistance(z);
        };
        fnRegistry["SetPVCamVisible"] = (args) =>
        {
            bool tgle = bool.Parse(args[0]);
            MMDCam.Instance.SetVisibility(tgle);
        };
        fnRegistry["ForceExit"] = (args) =>
        {
            if (args.Count > 0) return;
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            StartCoroutine(musicPlayer.EndSong(false));
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            gameManager.ResetAllValues();
            gameManager.ExitGame(false);
        };
        fnRegistry["PVCamChromAbr"] = (args) =>
        {
            bool tgle = bool.Parse(args[0]);
            chrabrToggle = tgle;
        };
        fnRegistry["PVHaptic"] = (args) =>
        {
            bool tgle = bool.Parse(args[0]);
            hapticToggle = tgle;
        };
        fnRegistry["StartPVFromPath"] = (args) =>
        {
            Debug.Log($"[StartPVFromPath({args[0]})] TODO: implement *.pjcbpv file logic");
        };
    }

    void ExecuteScriptEvent(ScriptEvent ev)
    {
        if (ev == null) return;
        // find script def by name
        var def = scripts.Find(s => s.name == ev.scriptName);
        if (def != null)
        {
            ExecuteScriptBody(def.body, ev.args);
        }
        else
        {
            // allow calling a single function name directly
            if (fnRegistry.TryGetValue(ev.scriptName, out var fn)) fn(ev.args != null ? ev.args.ToList() : new List<string>());
            else Debug.LogWarning("Script or function not found: " + ev.scriptName);
        }
    }
    void ExecuteScriptByString(string ev)
    {
        try
        {
            if (string.IsNullOrEmpty(ev)) return;
            string originalFunctionName = ev.Trim();
            string expression = originalFunctionName;
            bool wrappedInBrackets = expression.StartsWith("[") && expression.EndsWith("]");

            // Accept: "function", "function(args)", or "[function (args)]".
            if (wrappedInBrackets)
            {
                expression = expression.Substring(1, expression.Length - 2).Trim();
            }

            string funcName;
            string argsText = string.Empty;
            int sep = expression.IndexOf('(');
            if (sep >= 0)
            {
                funcName = expression.Substring(0, sep).Trim();
                int close = expression.LastIndexOf(')');
                if (close > sep)
                {
                    argsText = expression.Substring(sep + 1, close - sep - 1);
                }
            }
            else
            {
                // Preserve bracket-only legacy names such as [coop_all_near].
                funcName = wrappedInBrackets ? originalFunctionName : expression.Trim();
            }

            // parse arguments using the existing ParseArgs (handles quoted strings)
            List<string> argSplits = ParseArgs(argsText);

            // find script def by name
            var def = scripts.Find(s => s.name == funcName);
            if (def != null)
            {
                ExecuteScriptBody(def.body, argSplits.ToArray());
                return;
            }

            // allow calling a single function name directly
            if (fnRegistry.TryGetValue(funcName, out var fn))
            {
                fn(argSplits);
            }
            else if (wrappedInBrackets && fnRegistry.TryGetValue(originalFunctionName, out var legacyFn))
            {
                legacyFn(argSplits);
            }
            else
            {
                Debug.LogWarning("Script or function not found: " + funcName);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[VenueAnimationPlayer.ExecuteScriptByString] An error occured when parsing script string: " + ex.Message);
        }
    }
    void CueAnimEvent(NoteSpawner.GlobalEventInfo ev)
    {
        if (ev == null) return;
        Regex regex = new Regex("\\[[^\\]]*\\]", RegexOptions.IgnoreCase);
        var def = preMadeAnims.Find(s => s.name == regex.Match(ev.value).Value);
        if (def != null)
        {
            TryCueCamAnim(mainCamera.gameObject, def);
        }
        else
        {
            if (mainCamera)
            {
                AnimationClip clip = Resources.Load<AnimationClip>(regex.Match(ev.value).Value);
                if (clip)
                {
                    TryCueCamAnim(mainCamera.gameObject, clip);
                }
                else
                {
                    Debug.LogWarning("Animation not found (tick " + ev.spawnTime + ", " + ev.spawnTimeMs + " ms): " + regex.Match(ev.value).Value);
                }
            }
        }
    }

    // Public helper to execute a script or function immediately (editor/runtime)
    public void ExecuteScriptNow(string scriptName, string[] args = null)
    {
        if (string.IsNullOrEmpty(scriptName)) return;
        var def = scripts.Find(s => s.name == scriptName);
        if (def != null)
        {
            ExecuteScriptBody(def.body, args);
            return;
        }
        if (fnRegistry.TryGetValue(scriptName, out var fn))
        {
            fn(args != null ? args.ToList() : new List<string>());
            return;
        }
        Debug.LogWarning("ExecuteScriptNow: script or function not found: " + scriptName);
    }

    void ExecuteScriptBody(string bodyTemplate, string[] invocationArgs)
    {
        if (string.IsNullOrEmpty(bodyTemplate)) return;
        string body = bodyTemplate;
        if (invocationArgs != null)
        {
            for (int i = 0; i < invocationArgs.Length; i++)
                body = body.Replace("$" + i, invocationArgs[i]);
        }

        var statements = body.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var stmt in statements)
        {
            string s = stmt.Trim();
            if (string.IsNullOrEmpty(s)) continue;
            int p = s.IndexOf('(');
            if (p < 0) continue;
            string fname = s.Substring(0, p).Trim();
            int end = s.LastIndexOf(')');
            if (end < p) end = s.Length - 1;
            string argText = s.Substring(p + 1, end - (p + 1));
            var argList = ParseArgs(argText);
            if (fnRegistry.TryGetValue(fname, out var fn)) fn(argList);
            else Debug.LogWarning("Script function not found: " + fname);
        }
    }

    List<string> ParseArgs(string argText)
    {
        var res = new List<string>();
        if (string.IsNullOrEmpty(argText)) return res;
        int i = 0, n = argText.Length;
        var sb = new StringBuilder();
        bool inQuote = false;
        char quote = '"';
        for (; i < n; i++)
        {
            char c = argText[i];
            if (!inQuote && (c == '"' || c == '\'')) { inQuote = true; quote = c; continue; }
            if (inQuote)
            {
                if (c == quote) { inQuote = false; continue; }
                if (c == '\\' && i + 1 < n) { i++; sb.Append(argText[i]); continue; }
                sb.Append(c); continue;
            }
            if (c == ',') { res.Add(sb.ToString().Trim()); sb.Length = 0; continue; }
            sb.Append(c);
        }
        if (sb.Length > 0) res.Add(sb.ToString().Trim());
        return res;
    }

    float ParseF(List<string> args, int i)
    {
        if (args != null && args.Count > i && float.TryParse(args[i], out var v)) return v;
        return 0f;
    }
}
