using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Scoring : MonoBehaviour
{
    public static void Save(List<string> parameters)
    {
        using (StreamWriter writer = new StreamWriter(GameManager.Instance.savePath + @"\pjcbsave_" + DateTime.Now.ToString("s").Replace(":", "_") + ".ini"))
        {
            writer.WriteLine("[pjcbsave]");
            foreach (string pmt in parameters)
            {
                if (pmt.Contains("="))
                {
                    writer.WriteLine(pmt);
                }
            }
        }
    }
}
