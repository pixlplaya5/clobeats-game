using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Rewired;


public class OldInputManager : MonoBehaviour
{
    public static OldInputManager Instance;
    public enum InputUpdateType
    {
        PerFrame,
        Fixed,
        Late
    }

    public InputUpdateType updateType = InputUpdateType.PerFrame;
    public int rewiredPlayerId = 1;
    private Player player;
    public GameObject pauseMenu;
    private LaneInputManager laneInputManager;
    public bool isPaused = false;
    public bool denyInput = false;
    public bool gamepadMode = false;
    public float currentTimeScale = 1;
    public float whammyAmount = -1;
    public float tiltAmount = -1;
    public float scrollAxis = 0f;


    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    void Awake()
    {
        rewiredPlayerId = ReInput.players.GetPlayerId("Player0");
        player = ReInput.players.GetPlayer(rewiredPlayerId);
        DontDestroyOnLoad(this.gameObject);
        laneInputManager = FindFirstObjectByType<LaneInputManager>();
    }

    public void ToggleGamepadMode(bool yes)
    {
        gamepadMode = yes;
        GameManager.Instance.gamepadMode = yes;
        StartCoroutine(NotificationHandler.Instance.ShowNotifImmediatelyWithTimeout("Gamepad Mode toggled to " + yes.ToString(), 5f));
    }

    private async Task InputNoteDown(int value)
    {
        if (laneInputManager != null)
        {
            laneInputManager.OnFretPressed(value);
            await Task.Yield();
        }
    }
    private async Task InputNoteUp(int value)
    {
        if (laneInputManager != null)
        {
            laneInputManager.OnFretReleased(value);
            await Task.Yield();
        }
    }

    private async Task InputNote(int value, bool held)
    {
        if (laneInputManager != null)
        {
            if (held)
            {
                laneInputManager.OnFretPressed(value);
            }
            else
            {
                laneInputManager.OnFretReleased(value);
            }
            await Task.Yield();
        }
    }
    private async Task InputStrum()
    {
        if (laneInputManager != null)
        {
            if (GameManager.Instance.inSong)
                laneInputManager.OnStrum();
            
            await Task.Yield();
        }
    }

    
    // Update is called once per frame
    async void Update()
    {   
        laneInputManager = FindFirstObjectByType<LaneInputManager>();
        if (laneInputManager == null) return;
        if (player == null)
        {
            player = ReInput.players.GetPlayer(rewiredPlayerId);
        }
        Time.timeScale = currentTimeScale;
            
        if (updateType == InputUpdateType.PerFrame)
        {
            await GetInput();
        }
    }

    async void FixedUpdate()
    {
        if (updateType == InputUpdateType.Fixed)
        {
            await GetInput();
        }
    }
    async void LateUpdate()
    {
        if (updateType == InputUpdateType.Late)
        {
            await GetInput();
        }
    }
    public void TriggerVibration(float leftMotor, float rightMotor, float duration)
    {   
        // Pass motor index, motor level (0.0 to 1.0), and duration in seconds
        // Motor 0 is typically the Left/Low-Frequency motor
        player.SetVibration(0, leftMotor, duration);
        
        // Motor 1 is typically the Right/High-Frequency motor
        player.SetVibration(1, rightMotor, duration);
    }
    public void MobileVibrate()
    {
        //Handheld.Vibrate(); // untested, may vibrate continuously
        Solo.MOST_IN_ONE.MOST_HapticFeedback.Generate(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.MediumImpact);
    }
    private async Task GetInput()
    {
        if (GameManager.Instance.isLoading)
        {
            return;
        }
        scrollAxis = player.GetAxis("ScrollWheel");
        whammyAmount = player.GetAxis("Whammy");
        tiltAmount = player.GetAxis("Tilt");
        if (SceneManager.GetSceneByName("HS_Screen").isLoaded)
        {
            return;
        }
        if (SceneManager.GetSceneByName("MainMenu").isLoaded && !SceneManager.GetSceneByName("Gameplay").isLoaded)
        {
            if (MenuManager.Instance != null)
            {
                if (player.GetButtonDown("Green"))
                {
                    if (MenuManager.Instance.IsMenuOpen(MenuManager.StartMenuId))
                    {
                        MenuManager.Instance.Submit(2);
                    }
                    else if (MenuManager.Instance.IsMenuOpen(MenuManager.QuickPlayMenuId))
                    {
                        MenuManager.Instance.Submit(1);
                    }
                    else
                    {
                        MenuManager.Instance.SubmitCurrentSelection();
                    }
                }
                if (player.GetButtonDown("Red"))
                {
                    MenuManager.Instance.Exit();
                }
                if (MenuManager.Instance.IsMenuOpen(MenuManager.QuickPlayMenuId))
                {
                    if (player.GetButtonDown("StrumDown") || scrollAxis == 1)
                    {
                        UGUIMenuList menuList = FindAnyObjectByType<UGUIMenuList>();
                        if (menuList != null) menuList.SelectNext();
                    }
                    else if (player.GetButtonDown("StrumUp") || scrollAxis == -1)
                    {
                        UGUIMenuList menuList = FindAnyObjectByType<UGUIMenuList>();
                        if (menuList != null) menuList.SelectPrevious();
                    }
                }
                else
                {
                    if (player.GetButtonDown("StrumDown"))
                    {
                        MenuManager.Instance.SelectNextControl();
                    }
                    else if (player.GetButtonDown("StrumUp"))
                    {
                        MenuManager.Instance.SelectPreviousControl();
                    }
                }
                
            }
        }
        else if (SceneManager.GetSceneByName("Gameplay").isLoaded)
        {
            if (isPaused)
            {
                if (player.GetButtonDown("Green"))
                {
                    MenuManager.Instance.SubmitCurrentSelection();
                }
                if (player.GetButtonDown("Red"))
                {
                    MenuManager.Instance.Exit();
                }
                if (player.GetButtonUp("Start")) PauseGame();
                if (player.GetButtonDown("StrumDown"))
                {
                    MenuManager.Instance.SelectNextControl();
                }
                else if (player.GetButtonDown("StrumUp"))
                {
                    MenuManager.Instance.SelectPreviousControl();
                }
            }
            else
            {
                if (!gamepadMode)
                {
                    await InputNote(0, player.GetButton("Green"));
                    await InputNote(1, player.GetButton("Red"));
                    await InputNote(2, player.GetButton("Yellow"));
                    await InputNote(3, player.GetButton("Blue"));
                    await InputNote(4, player.GetButton("Orange"));

                    if (player.GetButtonDown("StrumUp")) await InputStrum();
                    if (player.GetButtonDown("StrumDown")) await InputStrum();

                    if (player.GetButtonUp("Start")) PauseGame();
                    if (player.GetButtonUp("Select")) ReleaseSP();

                
                    if (tiltAmount == 1)
                    {
                        ReleaseSP();
                    }
                }
                else if (denyInput)
                {
                    if (player.GetButtonUp("Start")) PauseGame();
                    if (player.GetButtonUp("Select")) ReleaseSP();
                }
                else
                {
                    await InputNote(0, player.GetButtonDown("Green"));
                    await InputNote(1, player.GetButtonDown("Red"));
                    await InputNote(2, player.GetButtonDown("Yellow"));
                    await InputNote(3, player.GetButtonDown("Blue"));
                    await InputNote(4, player.GetButtonDown("Orange"));

                    if (player.GetButtonUp("Start")) PauseGame();
                    if (player.GetButtonUp("Select")) ReleaseSP();
                }
            }
        }
    }
    public void ReleaseSP()
    {
        // automatically handled by just releasing whenever star power hits 100% just like in regular roBeats
        if (SceneManager.GetSceneByName("Gameplay").isLoaded)
        {
            StarMeter spmeter = FindAnyObjectByType<StarMeter>();
            if (spmeter != null)
            {
                if (spmeter.value >= 50 && !UIUpdater.Instance.inStar)
                {
                    UIUpdater.Instance.StarPowerToggle(GameManager.Instance.currentPart.ToLower(), true);
                }
                else
                {
                    return;
                }
            }
        }
    }
    public void PauseGame()
    {
        if (SceneManager.GetSceneByName("Gameplay").isLoaded)
        {
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            if (isPaused)
            {
                if (musicPlayer != null)
                {
                    //musicPlayer.resumeAudio();
                    musicPlayer.PlayAllStems();
                }
                //currentTimeScale = 1.0f; // Resume game
                MenuManager menu = FindAnyObjectByType<MenuManager>();
                if (menu != null)
                {
                    menu.ShowMenu("null");
                }
                ImprovedStrikeline strikeline = FindAnyObjectByType<ImprovedStrikeline>();
                if (strikeline != null)
                {
                    strikeline.ResetAnims();
                }
                VenueAnimationPlayer venue = FindAnyObjectByType<VenueAnimationPlayer>();
                if (venue)
                {
                    venue.TryToggleCamera(true);
                }
                isPaused = false;
            }
            else
            {
                if (musicPlayer != null)
                {
                    //musicPlayer.pauseAudio();
                    musicPlayer.PauseAllStems();
                }
                //currentTimeScale = 1.0f; // Pause game
                MenuManager menu = FindAnyObjectByType<MenuManager>();
                if (menu != null)
                {
                    menu.ShowMenu("pause");
                }
                ImprovedStrikeline strikeline = FindAnyObjectByType<ImprovedStrikeline>();
                if (strikeline != null)
                {
                    strikeline.ResetAnims();
                }
                VenueAnimationPlayer venue = FindAnyObjectByType<VenueAnimationPlayer>();
                if (venue)
                {
                    venue.TryToggleCamera(false);
                }
                isPaused = true;
            }
        }
        
    }
    
}
