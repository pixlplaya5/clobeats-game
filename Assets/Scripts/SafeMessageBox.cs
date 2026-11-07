using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;

public class SafeMessageBox : MonoBehaviour
{
    static SafeMessageBox _instance;
    public static SafeMessageBox Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SafeMessageBox>();
                if (_instance == null)
                {
                    var go = new GameObject("_SafeMessageBox");
                    #if UNITY_EDITOR
                    go.hideFlags = HideFlags.HideAndDontSave;
                    #endif
                    _instance = go.AddComponent<SafeMessageBox>();
                }
            }
            return _instance;
        }
    }

    public Dictionary<System.Windows.Forms.MessageBoxButtons, List<MessageBox.MethodEntry>> winFormsButtonAssigns = new Dictionary<MessageBoxButtons, List<MessageBox.MethodEntry>>
    {
        [MessageBoxButtons.AbortRetryIgnore] = new List<MessageBox.MethodEntry>
        {
            new MessageBox.MethodEntry
            {
                methodName = "Abort",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            },
            new MessageBox.MethodEntry
            {
                methodName = "Retry",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            },
            new MessageBox.MethodEntry
            {
                methodName = "Ignore",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            }
        },
        [MessageBoxButtons.OK] = new List<MessageBox.MethodEntry>
        {
            new MessageBox.MethodEntry
            {
                methodName = "OK",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            }
        },
        [MessageBoxButtons.OKCancel] = new List<MessageBox.MethodEntry>
        {
            new MessageBox.MethodEntry
            {
                methodName = "OK",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            },
            new MessageBox.MethodEntry
            {
                methodName = "Cancel",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            }
        },
        [MessageBoxButtons.RetryCancel] = new List<MessageBox.MethodEntry>
        {
            new MessageBox.MethodEntry
            {
                methodName = "Retry",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            },
            new MessageBox.MethodEntry
            {
                methodName = "Cancel",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            }
        },
        [MessageBoxButtons.YesNo] = new List<MessageBox.MethodEntry>
        {
            new MessageBox.MethodEntry
            {
                methodName = "Yes",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            },
            new MessageBox.MethodEntry
            {
                methodName = "No",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            }
        },
        [MessageBoxButtons.YesNoCancel] = new List<MessageBox.MethodEntry>
        {
            new MessageBox.MethodEntry
            {
                methodName = "Yes",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            },
            new MessageBox.MethodEntry
            {
                methodName = "No",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            },
            new MessageBox.MethodEntry
            {
                methodName = "Cancel",
                method = () => 
                {
                    MenuManager.Instance.ShowMenu("main");
                }
            }
        }
    };

    public DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        try
        {
            return System.Windows.Forms.MessageBox.Show(text, caption, buttons, icon);
        }
        catch
        {
            MessageBox.Instance.Show(text, caption, winFormsButtonAssigns[buttons]);
            return DialogResult.None;
        }
    }
}
