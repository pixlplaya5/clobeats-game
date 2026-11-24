using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class UIUpdater : MonoBehaviour
{
    public static UIUpdater Instance;
    public TMPro.TMP_Text scoreText;
    public GameObject scoreObject;
    public TMPro.TMP_Text comboText;
    public TMPro.TMP_Text comboDotsText;
    public TMPro.TMP_Text notesHitText;
    public NewRockMeter rockMeterSlider;
    public StarMeter spMeter;

    public GameObject songInfoPanel;
    public TMPro.TMP_Text currentBPMText;
    public TMPro.TMP_Text currentTickText;
    public TMPro.TMP_Text currentEventText;
    private NoteSpawner noteSpawner;
    private LaneInputManager lim;

    public float savedScore = 0f;
    public int savedCombo = 1;
    public int maxCombo;
    int comboDotsCount = 0;
    public int savedNotesHit = 0;
    public int savedGeneralNotesHit = 0;
    public int savedPerfectNotesHit;
    public int savedGoodNotesHit;
    public int savedOkayNotesHit;
    public int savedBadNotesHit;
    public int savedMissedNotes;
    public float savedSustainScore = 0f;
    int rockMeter = 50;
    float spMeterRead = 0;
    public float tempo = 120.000f;

    public int combolimit = 4;

    public bool inStar = false;

    public enum UpdateType
    {
        Visuals,
        ScoreAdd,
        ScoreMinus,
    }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public void InitializeUI()
    {
        scoreText.text = "0";
        comboText.text = "1x";
        comboDotsText.text = "";
        notesHitText.text = "0";
        rockMeterSlider.value = rockMeter;
        spMeter.value = spMeterRead;
    }
    public void UpdateForNoteHit()
    {
        lim = FindAnyObjectByType<LaneInputManager>();
        if (lim != null)
        {
            UpdateScore();
            UpdateCombo(UpdateType.ScoreAdd);
            UpdateNotesHit(lim.lastHitLane, lim.secondsUntil);
            UpdateRockMeter();
            if (!inStar)
            {
                UpdateStarMeter(0.25f);
            }
        }
        GuitarPlayer player = FindAnyObjectByType<GuitarPlayer>();
        if (player != null)
        {
            player.NoteHit();
        }
        
    }

    public void StarPowerToggle(string part, bool toggle)
    {
        SFXPlayer sFX = FindAnyObjectByType<SFXPlayer>();
        MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
        HighwayTextureChanger highway = FindAnyObjectByType<HighwayTextureChanger>();
        inStar = toggle;

        if (toggle)
        {
            combolimit = 8;
            savedCombo = savedCombo * 2;
            highway.TrySetSpriteColor(0, 1, 1);
            sFX.PlayClip("Woosh");
            //sFX.PlayClip("FeverCheer1");
            musicPlayer.ToggleStemReverb(part, true);
        }
        else
        {
            combolimit = 4;
            highway.TrySetSpriteColor(1, 1, 1);
            sFX.PlayClip("Star_Release");
            musicPlayer.ToggleStemReverb(part, false);
        }

    }
    public void UpdateForNoteMiss()
    {
        ResetCombo();
        DecreaseRockMeter();
    }
    public void UpdateForSustainHold(float sustainAmount)
    {
        UpdateScoreSustain(sustainAmount);
    }
    public void UpdateRockMeter()
    {
        rockMeter = Mathf.Clamp(rockMeter + 1, 0, 100);
        rockMeterSlider.value = rockMeter;
    }
    public void UpdateStarMeter(float amount)
    {
        spMeterRead = Mathf.Clamp(spMeterRead + amount, 0, 100);
        spMeter.value = spMeterRead;
    }
    public void UpdateScore()
    {
        savedScore += 50f * savedCombo;
        scoreText.text = savedScore.ToString("N0");
    }
    public void UpdateScoreSustain(float amount)
    {
        savedScore += amount;
        savedSustainScore += amount;
        scoreText.text = savedScore.ToString("N0");
    }
    public void UpdateCombo(UpdateType type = UpdateType.ScoreAdd)
    {
        if (type == UpdateType.ScoreAdd)
        {
            if (savedNotesHit % 10 == 0 && savedNotesHit != 0)
            {
                savedCombo += 1; // Increase combo every 10 notes hit
                comboDotsCount = 0;
            }
            comboDotsCount += 1;
        }
        else if (type == UpdateType.ScoreMinus)
        {
            if (savedNotesHit % 10 == 0 && savedNotesHit != 0)
            {
                savedCombo -= 1; // decrease combo every 10 notes hit
                comboDotsCount = 0;
            }
            comboDotsCount -= 1;
        }
        else if (type == UpdateType.Visuals)
        {
            comboText.text = savedCombo.ToString() + "x";
            comboDotsText.text = new string('.', comboDotsCount);

            if (savedCombo == 2)
            {
                comboText.color = new Color(1f, 0.5f, 0f);
                comboDotsText.color = new Color(1f, 0.5f, 0f);
            }
            else if (savedCombo == 3)
            {
                comboText.color = Color.green;
                comboDotsText.color = Color.green;
            }
            else if (savedCombo == 4)
            {
                comboText.color = new Color(1f, 0f, 1f);
                comboDotsText.color = new Color(1f, 0f, 1f);
            }
            else
            {
                if (inStar)
                {
                    comboText.color = Color.cyan;
                    comboDotsText.color = Color.cyan;
                }
                else
                {
                    comboText.color = Color.white;
                    comboDotsText.color = Color.white;
                }
            
            }
        }
        
        
        if (savedCombo >= combolimit)
        {
            savedCombo = combolimit; // Limit combo to 4x
            comboDotsCount = 10;
        }
        
        
    }
    public void UpdateNotesHit(int lane, float timingSeconds)
    {
        savedNotesHit += 1;
        savedGeneralNotesHit += 1;
        
        if (Mathf.Abs(timingSeconds) <= 0.02f)
        {
            savedPerfectNotesHit += 1;
        }
        else if (Mathf.Abs(timingSeconds) <= 0.04f)
        {
            savedGoodNotesHit += 1;
        }
        else if (Mathf.Abs(timingSeconds) <= 0.08f)
        {
            savedOkayNotesHit += 1;
        }
        else if (Mathf.Abs(timingSeconds) <= 0.15f)
        {
            savedBadNotesHit += 1;
        }

        if (maxCombo != savedNotesHit)
        {
            maxCombo += 1;
        }
        notesHitText.text = savedNotesHit.ToString();
    }
    public void ResetCombo()
    {
        savedMissedNotes += 1;
        if (savedNotesHit == 0)
        {
            return;
        }
        else
        {
            SFXPlayer sFXPlayer = FindAnyObjectByType<SFXPlayer>();
            sFXPlayer.PlayClip("multilost");
            savedCombo = 1;
            savedNotesHit = 0;
            comboDotsCount = 0;
            comboText.text = savedCombo.ToString() + "x";
            notesHitText.text = savedNotesHit.ToString();
        }
    }
    public void DecreaseRockMeter()
    {
        rockMeter = Mathf.Clamp(rockMeter - 1, 0, 100);
        rockMeterSlider.value = rockMeter;
    }
    
    public void UpdateSongInfo(string title, string artist, int year)
    {
        songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().text = title;
        songInfoPanel.transform.Find("ArtistText - UI").GetComponent<TMPro.TMP_Text>().text = "by " + artist + ", " + year.ToString();
    }
    public void SetSongInfoOpacity(float amount)
    {
        songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().color = new Color(songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().color.r,
        songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().color.g,
        songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().color.b,
        Mathf.Clamp01(amount));

        songInfoPanel.transform.Find("ArtistText - UI").GetComponent<TMPro.TMP_Text>().color = new Color(songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().color.r,
        songInfoPanel.transform.Find("ArtistText - UI").GetComponent<TMPro.TMP_Text>().color.g,
        songInfoPanel.transform.Find("ArtistText - UI").GetComponent<TMPro.TMP_Text>().color.b,
        Mathf.Clamp01(amount));
    }
    public void UpdateBPM(double bpm)
    {
        currentBPMText.text = "BPM: " + bpm.ToString("F1");
    }
    public void UpdateCurrentTick(float tick)
    {
        currentTickText.text = $"Tick: {tick}"; // Append the current tick to the BPM text
    }
    public async Task UpdateCurrentEvent(string value)
    {
        currentEventText.text = $"GlobalEvent: {value}";
        //Debug.Log("Current MIDI text event: " + value);
        await Task.Yield();
    }

    public IEnumerator SongInfoAnim(float duration = 6f)
    {
        if (songInfoPanel != null)
        {
            SetSongInfoOpacity(1);
            yield return new WaitForSeconds(duration);
            SetSongInfoOpacity(0);
        }
    }

    public void ScoreVisibility(bool toggle)
    {
        if (toggle)
        {
            scoreObject.SetActive(true);
        }
        else
        {
            scoreObject.SetActive(false);
        }
    }

    // Update is called once per frame
    async void Update()
    {
        if (noteSpawner == null)
        {
            noteSpawner = GameObject.FindAnyObjectByType<NoteSpawner>();
            if (noteSpawner == null)
            {
                
            }
        }
        else
        {
            SongFolderLoader songFolderLoader = FindAnyObjectByType<SongFolderLoader>();
            //UpdateLoadingPhrase(songFolderLoader.loadingPhrase);
            UpdateCurrentTick(noteSpawner.currentTick);
            tempo = SyncInfoToTempo(noteSpawner.FindSyncForTick(noteSpawner.currentTick));
            UpdateBPM(tempo);
            await UpdateCurrentEvent(noteSpawner.GetEventInfoStringOnTick(noteSpawner.currentTick));
            UpdateCombo(UpdateType.Visuals);
            if (inStar)
            {
                UpdateStarMeter(-Time.deltaTime * 5f);
                //savedcombo = savedcombo * 2;
            }
        }
    }

    public float SyncInfoToTempo(NoteSpawner.SyncInfo syncInfo)
    {
        if (syncInfo != null)
        {
            return syncInfo.bpm;
        }
        else
        {
            return 0f;
        }
    }
}


