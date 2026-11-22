using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Threading;
using System.Linq;
using System.ComponentModel;
using TMPro;
using System.Drawing.Text;
using Rewired.Integration.UnityUI;

public class PlayerPrefsLoader : MonoBehaviour
{
    public bool autoLoad = false;
    public GameObject blankImage;
    public GameObject indefiniteLoadingScreen;
    public List<string> songItemNames = new List<string>();

    async void Awake()
    {
        await LoadAllSFX();
        if (autoLoad)
        {
            Debug.Log("Auto Load enabled.");
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
            await Task.Delay(100);
            if (indefiniteLoadingScreen != null)
            {
                indefiniteLoadingScreen.SetActive(false);
            }
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
            RewiredStandaloneInputModule inputModule = FindFirstObjectByType<RewiredStandaloneInputModule>();
            inputModule.allowMouseInput = false;
            if (SceneManager.GetSceneByName("MainMenu").isLoaded) MessageBox.Instance.Show($"Welcome to {Application.productName} !<br>Loading custom songs takes a <b>VERY</b> long time, "
            + "so if you don't want to wait, please exit the game and move your custom songs somewhere else to access the online song catalog, "
            + "or simply wait.<br>Press OK to continue.", "Message", new List<MessageBox.MethodEntry> { new MessageBox.MethodEntry { methodName = "OK", method = async () => {await LoadGame();} } });
        }
        else
        {
            Debug.Log("Auto Load disabled.");
            if (blankImage != null)
            {
                blankImage.SetActive(false);
            }
            if (indefiniteLoadingScreen != null)
            {
                indefiniteLoadingScreen.SetActive(false);
            }
            RewiredStandaloneInputModule inputModule = FindFirstObjectByType<RewiredStandaloneInputModule>();
            inputModule.allowMouseInput = true;
        }
    }

    void OnEnable()
    {
        InitLoadingScreen();
    }

    public void InitLoadingScreen()
    {
        if (!GameManager.Instance.initialized)
        {
            GameObject loadscreen = Instantiate(GameManager.Instance.unDestructibleLoadingPhraseScreen);
            DontDestroyOnLoad(loadscreen);
            GameManager.Instance.unDestructibleLoadingPhraseScreen = loadscreen;
            GameManager.Instance.DisableLoadSongVisual(GameManager.Instance.unDestructibleLoadingPhraseScreen);
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            GameManager.Instance.savePath = documentsPath + Path.DirectorySeparatorChar + "CloBeats" + Path.DirectorySeparatorChar + "save";
            if (!Directory.Exists(GameManager.Instance.savePath))
            {
                Directory.CreateDirectory(GameManager.Instance.savePath);
            }
        }
    }

    public async Task LoadAllSFX()
    {
        SFXPlayer sFXPlayer = FindAnyObjectByType<SFXPlayer>();
        if (sFXPlayer != null)
        {
            if (sFXPlayer.loadedAudioClips.Count == 0)
            {
                string[] audioclips = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "SFXPacks", GameManager.Instance.GetSFXPackName()));
                foreach (string clip in audioclips)
                {
                    if (clip.EndsWith("meta")) continue;
                    AudioClip clip1 = await MusicPlayer.Instance.LoadUnityAudioFile(clip);
                    sFXPlayer.LoadActualClip(clip1);
                }
            }
            else
            {
                Debug.LogWarning("All sound effects are already loaded.");
            }
        }
    }

    public async Task<bool> LoadGame(bool loadMainMenu = true)
    {
        if (indefiniteLoadingScreen != null)
        {
            indefiniteLoadingScreen.SetActive(true);
        }
        if (await LoadWholeGame(loadMainMenu))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public async Task<bool> LoadWholeGame(bool loadMainMenu = true)
    {
        Debug.Log("Loading game...");
        if (await LoadPart1())
        {
            GameManager.Instance.initialized = true;
        } // loads local songs
        LoadPart2(); // loads shaders
        if (indefiniteLoadingScreen != null)
        {
            indefiniteLoadingScreen.SetActive(false);
        }
        if (loadMainMenu)
        {
            LoadingManager loader = FindFirstObjectByType<LoadingManager>();
            if (loader != null)
            {
                MenuManager manager = FindAnyObjectByType<MenuManager>();
                manager.ShowMenu("null");
                loader.LoadScene("HS_Screen", LoadSceneMode.Additive);
                await Task.Delay(6000);
                SceneManager.UnloadSceneAsync("HS_Screen");
                manager.ShowMenu("start");
                return true;
            }
            else
            {
                Debug.LogError("LoadingManager not found in scene!");
                return false;
            }
        }
        else
        {
            return true;
        }
    }

    public async Task<bool> LoadPart1()
    {
        if (!GameManager.Instance.initialized)
        {
            try
            {
                Debug.Log("Loading custom songs");
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string songFoldersPath = documentsPath + Path.DirectorySeparatorChar + "CloBeats" + Path.DirectorySeparatorChar + "songs" + Path.DirectorySeparatorChar + "local";
                if (!Directory.Exists(songFoldersPath))
                {
                    Directory.CreateDirectory(songFoldersPath);
                }
                string[] directories = Directory.GetDirectories(songFoldersPath);
                if (directories != null)
                {
                    int ind = 0;
                    foreach (string dir in directories)
                    {
                        int hash = Mathf.Abs(dir.GetHashCode());
                        GameManager.Instance.songFolders.Add(dir);
                        songItemNames.Add(dir);
                        GameManager.Instance.CacheSingleSong(dir, hash, ind);
                        StartCoroutine(GameManager.Instance.CacheAudioFile(GameManager.Instance.FindQueryAudioInPath(dir, "preview"), hash)); // use preview version if available
                        Debug.Log($"Cached {ind} song dir \"" + dir + "\" with absolute hash code " + hash);
                        ind++;
                        await Task.Yield();
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Song listing failed: " + ex);
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public void LoadPart2()
    {
        Debug.Log("Warming shaders");
        Shader.WarmupAllShaders();
    }
}
