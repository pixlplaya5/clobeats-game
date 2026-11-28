using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SaveData : MonoBehaviour
{
    public class DataEntry
    {
        public string key;
        public string value;
    }
    public static SaveData Instance;
    public TextMeshProUGUI teirRef;
    public TextMeshProUGUI accuracyRef;
    public TextMeshProUGUI songNameRef;
    public TextMeshProUGUI scoreRef;
    public TextMeshProUGUI perfectsRef;
    public TextMeshProUGUI goodsRef;
    public TextMeshProUGUI okaysRef;
    public TextMeshProUGUI badsRef;
    public TextMeshProUGUI missesRef;
    public TextMeshProUGUI mnsRef;
    public TextMeshProUGUI sustainBonusRef;
    public TextMeshProUGUI cmRef;

    string currentTeir;
    float currentAccuracy;
    string currentSongName;
    float currentScore;
    float currentPerfHits;
    float currentGoodHits;
    float currentOkayHits;
    float currentBadHits;
    float currentMisses;
    float currentMNS;
    float currentSustainBonus;
    string currentControlMethod;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
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

    public static string CalculateTeir(float accuracy)
    {
        if (accuracy == 100)
        {
            return "SS";
        }
        else if (accuracy >= 90)
        {
            return "S";
        }
        else if (accuracy >= 80)
        {
            return "A";
        }
        else if (accuracy >= 70)
        {
            return "B";
        }
        else if (accuracy >= 60)
        {
            return "C";
        }
        else
        {
            return "F";
        }
    }

    public void UpdateInfo(List<DataEntry> info)
    {
        foreach (var entry in info)
        {
            if (entry.key.Equals("teir", StringComparison.OrdinalIgnoreCase))
            {
                currentTeir = entry.value;
            }
            else if (entry.key.Equals("acc", StringComparison.OrdinalIgnoreCase))
            {
                currentAccuracy = float.Parse(entry.value);
            }
            else if (entry.key.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                currentSongName = entry.value;
            }
            else if (entry.key.Equals("score", StringComparison.OrdinalIgnoreCase))
            {
                currentScore = float.Parse(entry.value);
            }
            else if (entry.key.Equals("perfect", StringComparison.OrdinalIgnoreCase))
            {
                currentPerfHits = float.Parse(entry.value);
            }
            else if (entry.key.Equals("good", StringComparison.OrdinalIgnoreCase))
            {
                currentGoodHits = float.Parse(entry.value);
            }
            else if (entry.key.Equals("okay", StringComparison.OrdinalIgnoreCase))
            {
                currentOkayHits = float.Parse(entry.value);
            }
            else if (entry.key.Equals("bad", StringComparison.OrdinalIgnoreCase))
            {
                currentBadHits = float.Parse(entry.value);
            }
            else if (entry.key.Equals("miss", StringComparison.OrdinalIgnoreCase))
            {
                currentMisses = float.Parse(entry.value);
            }
            else if (entry.key.Equals("mns", StringComparison.OrdinalIgnoreCase))
            {
                currentMNS = float.Parse(entry.value);
            }
            else if (entry.key.Equals("sb", StringComparison.OrdinalIgnoreCase))
            {
                currentSustainBonus = float.Parse(entry.value);
            }
            else if (entry.key.Equals("cm", StringComparison.OrdinalIgnoreCase))
            {
                currentControlMethod = entry.value;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (teirRef) teirRef.text = currentTeir;
        if (accuracyRef) accuracyRef.text = currentAccuracy.ToString();
        if (songNameRef) songNameRef.text = currentSongName;
        if (scoreRef) scoreRef.text = "Score: " + currentScore;
        if (perfectsRef) perfectsRef.text = "Perfects: " + currentPerfHits;
        if (goodsRef) goodsRef.text = "Goods: " + currentGoodHits;
        if (okaysRef) okaysRef.text = "Okays: " + currentOkayHits;
        if (badsRef) badsRef.text = "Bads: " + currentBadHits;
        if (missesRef) missesRef.text = "Misses: " + currentMisses;
        if (mnsRef) mnsRef.text = "Max Note Streak: " + currentMNS;
        if (sustainBonusRef) sustainBonusRef.text = "Sustain Bonus: " + currentSustainBonus;
        if (cmRef) cmRef.text = "Control Method: " + currentControlMethod;
    }
}
