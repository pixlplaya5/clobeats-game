using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArgumentHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Retrieves all arguments passed to the application
        string[] args = System.Environment.GetCommandLineArgs();

        foreach (string arg in args)
        {
            if (arg == "--help")
            {
                SafeMessageBox.Instance.Show("TODO: show about screen", "Help", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.None);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
