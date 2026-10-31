using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MessageBox : MonoBehaviour
{
    public GameObject mbPrefab;
    public static MessageBox Instance;
    public GameObject buttonPrefab;

    public class MethodEntry
    {
        public string methodName;
        public Action method;
    }
    void Awake()
    {
        if (Instance == null) Instance = this; else if (Instance != this) Destroy(gameObject);
    }
    public void Show(string text, string caption, List<MethodEntry> methodList, bool muteGame = true)
    {
        if (mbPrefab != null)
        {
            if (!SceneManager.GetSceneByName("MainMenu").isLoaded) return;
            MenuManager menuManager = FindAnyObjectByType<MenuManager>();
            GameObject instance = menuManager.ReturnMenuGO("message");
            TMPro.TextMeshProUGUI title = instance.transform.Find("Canvas/MessageBox/Title").gameObject.GetComponent<TMPro.TextMeshProUGUI>();
            TMPro.TextMeshProUGUI text1 = instance.transform.Find("Canvas/MessageBox/Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>();
            title.text = caption;
            text1.text = text;
            Transform buttons = instance.transform.Find("Canvas/MessageBox/DismissButtonLayout");

            
            if (muteGame)
            {
                MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
                musicPlayer.MuteAllAudio(true);
            }
            if (methodList != null)
            {
                foreach (GameObject arbButton in buttons)
                {
                    if (arbButton) Destroy(arbButton); // prevents duplicate entries
                }
                foreach (var action in methodList)
                {
                    GameObject button = Instantiate(buttonPrefab, buttons);
                    TMPro.TextMeshProUGUI textMesh = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    textMesh.text = action.methodName;
                    Button selectable = button.GetComponent<Button>();
                    selectable.onClick.RemoveAllListeners();
                    selectable.onClick.AddListener(() =>
                    {
                        menuManager.ShowMenu("null");
                        action.method();
                    });
                    EventSystem.current.SetSelectedGameObject(button);
                }
            }
            /*if (button != null)
            {
                button.onClick.RemoveAllListeners();
                if (actionOnMessageClose != null) button.onClick.AddListener(() => 
                {
                    menuManager.ShowMenu("null");
                    actionOnMessageClose.Invoke();
                });
                
            }*/
        }
    }
}
