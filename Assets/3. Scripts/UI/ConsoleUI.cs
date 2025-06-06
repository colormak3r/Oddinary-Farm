using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleCommand             // Data structure for a command
{ 
    private string command;
    private string[] args;
    private string description;

    public ConsoleCommand(string command, string[] args, string description = "")
    {
        this.command = command;
        this.args = args;
        this.description = description;
    }
}

public class ConsoleUI : UIBehaviour, DefaultInputActions.IConsoleActions
{
    public static ConsoleUI Main;       // Singleton

    private static string SHOW_STACK_TRACE = "ShowStackTrace";
    private static string LOG_TO_FILE = "LogToFile";

    [Header("Settings")]
    [SerializeField]
    private int maxChar = 100000;       // Max Characters for input
    [SerializeField]
    private bool showStackTrace = false;
    [SerializeField]
    private bool logToFile = false;
    [SerializeField]
    private Color timeTagColor;
    [SerializeField]
    private Color conditionColor;
    [SerializeField]
    private Color errorTypeColor;
    [SerializeField]
    private Color assertTypeColor;
    [SerializeField]
    private Color warningTypeColor;
    [SerializeField]
    private Color logTypeColor;
    [SerializeField]
    private Color exceptionTypeColor;

    [Header("Required Components")]
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private TMP_Text debugText;
    [SerializeField]
    private TMP_Text suggestionText;
    [SerializeField]
    private ScrollRect scrollRect;

    private string log;
    private string filename = "";
    private float verticalNormalizedPosition_cached;

    private Coroutine scrollCoroutine;

    private void Awake()
    {
        if (Main == null)       // Handle singleton
        {
            Main = this;
        }
        else
        {
            Destroy(transform.parent.gameObject);
        }

        DontDestroyOnLoad(transform.parent.gameObject);

        showStackTrace = PlayerPrefs.GetInt(SHOW_STACK_TRACE, 0) == 1;
        logToFile = PlayerPrefs.GetInt(LOG_TO_FILE, 1) == 1;
    }

    protected override void Start()
    {
        base.Start();

        // Input for opening/closing/submitting, and scrolling through the consol
        InputManager.Main.InputActions.Console.SetCallbacks(this);

        inputField.text = "";
        debugText.text = "";
        suggestionText.text = "";

        UnfocusOnInputField();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // Subscribe to events
        inputField.onValueChanged.AddListener(OnInputValueChanged);
        inputField.onValidateInput += ValidateInput;
        Application.logMessageReceived += HandleLogMessageReceived;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        inputField.onValueChanged.RemoveListener(OnInputValueChanged);
        inputField.onValidateInput -= ValidateInput;
        Application.logMessageReceived -= HandleLogMessageReceived;
    }

    private void HandleLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        var log = BuildLogString(condition, stackTrace, type);

        SetLogText(log);

        if (logToFile)
            LogToFile(log);
    }

    private string BuildLogString(string condition, string stackTrace, LogType type)
    {
        // Build output string in HTML format
        StringBuilder sb = new StringBuilder();

        // HTML formats the output in TMP
        // Time color
        sb.Append("<color=#")
          .Append(ColorUtility.ToHtmlStringRGB(timeTagColor))
          .Append(">[")
          .Append(MiscUtility.FormatTimeHHMMSS(Time.time))
          .Append("]</color>");

        var typeColor = logTypeColor;
        switch (type)
        {
            case LogType.Error:
                typeColor = errorTypeColor;
                break;
            case LogType.Assert:
                typeColor = assertTypeColor;
                break;
            case LogType.Warning:
                typeColor = warningTypeColor;
                break;
            case LogType.Exception:
                typeColor = exceptionTypeColor;
                break;
        }

        // Attribute Color
        sb.Append("<color=#")
          .Append(ColorUtility.ToHtmlStringRGB(typeColor))
          .Append(">[")
          .Append(type.ToString())
          .Append("]\n</color>");

        // Condition Color
        sb.Append("<color=#")
          .Append(ColorUtility.ToHtmlStringRGB(conditionColor))
          .Append(">")
          .Append(condition)
          .Append("</color>");

        if (showStackTrace || (type != LogType.Log && type != LogType.Warning))
        {
            sb.Append("\n")
              .Append(stackTrace);
        }
        sb.Append("\n");

        return sb.ToString();
    }

    private void SetLogText(string newLog)
    {
        verticalNormalizedPosition_cached = scrollRect.verticalNormalizedPosition;
        log += newLog + "\n";
        if (log.Length > maxChar)
            log = log.Substring(log.Length - maxChar);

        debugText.text = log;

        ScrollToBottom();
    }

    // Output console logs
    private void LogToFile(string newLog)
    {
        // Create file name
        if (filename == "")
        {
            string d = System.Environment.GetFolderPath(
               System.Environment.SpecialFolder.Desktop) + "/ODD_LOGS";     // Set destination for logs as desktop
            System.IO.Directory.CreateDirectory(d);
            DateTime now = DateTime.Now;
            // Format the DateTime as MMDDYY-HHMMSS
            string t = now.ToString("MMddyy-HHmmss");
            string r = UnityEngine.Random.Range(1000, 9999).ToString();
            filename = d + "/log-" + t + "-" + r + "-" + VersionUtility.VERSION + ".txt";
        }


        try
        {
            // Strip HTML Tags
            string pattern = "<.*?>";
            string resultString = Regex.Replace(newLog, pattern, "");
            System.IO.File.AppendAllText(filename, resultString + "\n");
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    // List of commands
    private const string UNKNOWN_COMMAND = "Unknown command";
    private const string UNKNOWN_ARGUMENT = "Unknown argument";
    private string[] commands =
    {"Help",
    "ShowStackTrace",
    "ShowNetStat",
    "LogToFile",
    "Spawn",
    "PrintItemIdList",
    "PrintSpawnableIdList",
    "Spectate",
    "ShowUI",
    "SpawnTestWave",
    "CanSpawn",
    "SetMinutesPerDay",
    "SetTimeOffset",
    "Window",
    "StartNormalFlood",
    "StartInstantFlood",
    "SetCanFlood",
    "SetFlood",
    "Scenario"};

    private string[] commandHelps =
    {"Help",
    "ShowStackTrace[bool]",
    "ShowNetStat [bool]",
    "LogToFile [bool]",
    "Spawn [id] [x=0] [y=0] [count=1]",
    "PrintItemIdList",
    "PrintSpawnableIdList",
    "Spectate [id] [x] [y]",
    "ShowUI [bool]",
    "SpawnTestWave [x=0] [y=0] [safeRadius=5] [spawnRadius=10]",
    "CanSpawn [bool]",
    "SetMinutesPerDay [realMinutePerDay]",
    "SetTimeOffset [day] [hour] [minute]",
    "Window [fullwin, fullscreen, windowed, fullHD, 2k, 4k, size] [WxH] [fullscreen]",
    "StartNormalFlood",
    "StartInstantFlood",
    "SetCanFlood [bool]",
    "SetFlood [0~1]",
    "Scenario [name]"};

    // Take apart a string and perform a command
    private void ParseCommand(string input)
    {
        input = input.ToLower();            // Turn input into lowercase
        var args = input.Split(" ");        // Grab segments between spaces
        var command = args[0];              // First segment is the command

        // Clear the input field
        Debug.Log(">> " + input);
        inputField.text = "";

        // Parse the command
        try
        {
            if (command == commands[0].ToLower())
            {
                // Help
                string builder = "";
                foreach (var cc in commandHelps) builder += "\n>> " + cc;
                Debug.Log($"List of commands:{builder}");
            }
            else if (command == commands[1].ToLower())
            {
                // ShowStackTrace [bool]
                var defaultBool = args.Length > 1 ? ParseBool(args[1]) : true;
                showStackTrace = defaultBool;
                PlayerPrefs.SetInt(SHOW_STACK_TRACE, showStackTrace ? 1 : 0);
            }
            else if (command == commands[2].ToLower())
            {
                // ShowNetStat [bool]
                var defaultBool = args.Length > 1 ? ParseBool(args[1]) : true;
                NetworkManager.Singleton.gameObject.GetComponent<RuntimeNetStatsMonitor>().Visible = defaultBool;
            }
            else if (command == commands[3].ToLower())
            {
                // LogToFile [bool]
                var defaultBool = args.Length > 1 ? ParseBool(args[1]) : true;
                logToFile = defaultBool;
                PlayerPrefs.SetInt(LOG_TO_FILE, logToFile ? 1 : 0);
            }
            else if (command == commands[4].ToLower())
            {
                // Spawn [id] [x=0] [y=0] [count=1]
                if (AssetManager.Main == null) throw new Exception("AssetManager not found. Has the game started yet?");

                var x = args.Length > 2 ? float.Parse(args[2]) : 0f;        // x position
                var y = args.Length > 3 ? float.Parse(args[3]) : 0f;        // y position
                var count = args.Length > 4 ? int.Parse(args[4]) : 1;       // amount

                AssetManager.Main.SpawnByID(int.Parse(args[1]), new Vector2(x, y), count, true);        // Spawn through asset manager
            }
            else if (command == commands[5].ToLower())
            {
                // PrintItemIdList
                if (AssetManager.Main == null) throw new Exception("AssetManager not found. Has the game started yet?");
                AssetManager.Main.PrintItemIDs();
            }
            else if (command == commands[6].ToLower())
            {
                // PrintSpawnableIdList
                if (AssetManager.Main == null) throw new Exception("AssetManager not found. Has the game started yet?");
                AssetManager.Main.PrintSpawnableIDs();
            }
            else if (command == commands[7].ToLower())
            {
                ulong id = args.Length > 1 ? ulong.Parse(args[1]) : 999;
                var owner = (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost) ? NetworkManager.Singleton.LocalClient.PlayerObject.transform.position : Vector3.zero;
                var x = args.Length > 2 ? float.Parse(args[2]) : owner.x;
                var y = args.Length > 3 ? float.Parse(args[3]) : owner.y;

                if (Spectator.Main == null) throw new Exception("Spectator not found. Has the game started yet?");
                Spectator.Main.SetCamera(id);
                Spectator.Main.SetPosition(new Vector2(x, y));
            }
            else if (command == commands[8].ToLower())
            {
                // ShowUI [bool]
                var defaultBool = args.Length > 1 ? ParseBool(args[1]) : true;
                if (defaultBool)
                    UIManager.Main.ShowUI();
                else
                    UIManager.Main.HideUI();
            }
            else if (command == commands[9].ToLower())
            {
                // SpawnTestWave [x=0] [y=0] [safeRadius=5] [spawnRadius=10]
                var x = args.Length > 1 ? float.Parse(args[1]) : 0f;
                var y = args.Length > 2 ? float.Parse(args[2]) : 0f;
                var safeRadius = args.Length > 3 ? int.Parse(args[3]) : 5;
                var spawnRadius = args.Length > 4 ? int.Parse(args[4]) : 10;

                if (CreatureSpawnManager.Main == null) throw new Exception("CreatureSpawnManager not found. Has the game started yet?");
                CreatureSpawnManager.Main.SpawnTestWave(new Vector2(x, y), safeRadius, spawnRadius);
            }
            else if (command == commands[10].ToLower())
            {
                // CanSpawn [bool]
                var defaultBool = args.Length > 1 ? ParseBool(args[1]) : true;
                if (CreatureSpawnManager.Main == null) throw new Exception("CreatureSpawnManager not found. Has the game started yet?");
                CreatureSpawnManager.Main.SetCanSpawn(defaultBool);
            }
            else if (command == commands[11].ToLower())
            {
                // SetMinutesPerDay [realMinutePerDay]
                var realMinutesPerDay = args.Length > 1 ? float.Parse(args[1]) : 5f;

                if (TimeManager.Main == null) throw new Exception("TimeManager not found. Has the game started yet?");
                TimeManager.Main.SetRealMinutesPerDay(realMinutesPerDay);
            }
            else if (command == commands[12].ToLower())
            {
                // SetTimeOffset [day] [hour] [minute]
                var day = args.Length > 1 ? int.Parse(args[1]) : 1;
                var hour = args.Length > 2 ? int.Parse(args[2]) : 7;
                var minute = args.Length > 3 ? int.Parse(args[3]) : 30;

                if (TimeManager.Main == null) throw new Exception("TimeManager not found. Has the game started yet?");
                TimeManager.Main.SetTimeOffset(day, hour, minute);
            }
            else if (command == commands[13].ToLower())
            {
                // Window [fullwin, fullscreen, windowed, fullHD, 2k, 4k, size] [WxH] [fullscreen]
                var fullscreen = args.Length > 3 ? ParseBool(args[3]) : false;
                switch (args[1])
                {
                    case "fullwin":
                        Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
                        break;
                    case "fullscreen":
                        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                        break;
                    case "windowed":
                        Screen.SetResolution(480, 270, FullScreenMode.Windowed);
                        break;
                    case "fullhd":
                        fullscreen = args.Length > 2 ? ParseBool(args[2]) : false;
                        Screen.SetResolution(1920, 1080, fullscreen);
                        break;
                    case "2k":
                        fullscreen = args.Length > 2 ? ParseBool(args[2]) : false;
                        Screen.SetResolution(2560, 1440, fullscreen);
                        break;
                    case "4k":
                        fullscreen = args.Length > 2 ? ParseBool(args[2]) : false;
                        Screen.SetResolution(3840, 2160, fullscreen);
                        break;
                    case "size":
                        var size = args[2].Split("x");
                        if (size.Length == 2)
                        {
                            int width = int.Parse(size[0]);
                            int height = int.Parse(size[1]);
                            Screen.SetResolution(width, height, fullscreen);
                        }
                        else
                        {
                            throw new ArgumentException(UNKNOWN_ARGUMENT + $" '{args[2]}'");
                        }
                        break;
                    default:
                        throw new ArgumentException(UNKNOWN_ARGUMENT + $" '{args[1]}'");
                }
            }
            else if (command == commands[14].ToLower())
            {
                // StartNormalFlood
                if (FloodManager.Main == null) throw new Exception("FloodManager not found. Has the game started yet?");
                FloodManager.Main.StartNormalFlood();

            }
            else if (command == commands[15].ToLower())
            {
                // StartInstantFlood
                if (FloodManager.Main == null) throw new Exception("FloodManager not found. Has the game started yet?");
                FloodManager.Main.StartInstantFlood();

            }
            else if (command == commands[16].ToLower())
            {
                // SetCanFlood [bool]
                var defaultBool = args.Length > 1 ? ParseBool(args[1]) : true;
                if (FloodManager.Main == null) throw new Exception("FloodManager not found. Has the game started yet?");
                FloodManager.Main.SetCanFlood(defaultBool);
            }
            else if (command == commands[17].ToLower())
            {
                // SetFlood [0~1]
                var floodLevel = args.Length > 1 ? float.Parse(args[1]) : 0f;
                if (FloodManager.Main == null) throw new Exception("FloodManager not found. Has the game started yet?");
                FloodManager.Main.SetFloodLevel(floodLevel);
            }
            else if (command == commands[18].ToLower())
            {
                // Scenario [name]
                if (ScenarioManager.Main == null) throw new Exception("Fatal Error: ScenarioManager not found.");
                if (args.Length > 1)
                {
                    if (args[1].Contains("none"))
                    {
                        ScenarioManager.Main.SetScenario(ScenarioPreset.None);
                    }
                    else if (args[1].Contains("corn"))
                    {
                        ScenarioManager.Main.SetScenario(ScenarioPreset.CornFarmDemo);
                    }
                    else if (args[1].Contains("mid"))
                    {
                        ScenarioManager.Main.SetScenario(ScenarioPreset.MidSizeFarmDemo);
                    }
                    else if (args[1].Contains("chicken"))
                    {
                        ScenarioManager.Main.SetScenario(ScenarioPreset.ChickenFarmDemo);
                    }
                    else
                    {
                        throw new ArgumentException(UNKNOWN_ARGUMENT + $" '{args[1]}'");
                    }
                }
                else
                {
                    ScenarioManager.Main.SetScenario(ScenarioPreset.None);
                }
            }
            else
            {
                OutputNextFrame(input);
                throw new ArgumentException(UNKNOWN_COMMAND + $" '{input}'");
            }
        }
        catch (IndexOutOfRangeException i)
        {
            OutputNextFrame(input);
            string argsOut = "";
            foreach (var arg in args)
            {
                argsOut += arg + " ";
            }
            Debug.Log($"Incorrect number of arguments:\n- Args.Lengths = {args.Length}\n- Args: {argsOut}\n- {i.Message}");
        }
        catch (Exception e)
        {
            OutputNextFrame(input);
            Debug.Log("Error: " + e.Message);
        }
    }

    #region Input Actions
    // Close Console on Input
    public void OnCloseConsole(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed && !IsAnimating)
        {
            CloseConsole();

            UnfocusOnInputField();
        }
    }

    // Submit request on input
    public void OnSubmit(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed && IsShowing)
        {
            if (inputField.text != "")
                ParseCommand(inputField.text);

            FocusOnInputField();
            scrollRect.verticalNormalizedPosition = 0;
        }
    }

    // Auto complete command on input
    public void OnAutoComplete(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed && IsShowing)
        {
            HandleTabPress();
        }
    }

    public void OnScrollToBottom(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed && IsShowing)
        {
            ScrollToBottom(true);
        }
    }
    #endregion

    #region UI features
    private void OnInputValueChanged(string text)
    {
        // NOTE: Suggestion for clarity; it was a bit hard to read at first
        // suggestionText.text = (text == "") ? "" : SuggestCommand(text);
        suggestionText.text = text == "" ? "" : SuggestCommand(text);
    }

    // QUESTION: What does this do exactly?
    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        // Prevent backtick
        if (addedChar == '`')
        {
            /*// Close the console
            CloseConsole();*/

            // Return '\0' to ignore the character
            return '\0';
        }

        // Detect and handle Tab key
        if (addedChar == '\t')
        {
            /*HandleTabPress();*/

            // Return '\0' to ignore the character
            return '\0';
        }

        // Detect and handle Enter key
        if (addedChar == '\n' || addedChar == '\r')
        {
            //HandleEnterPress();

            // Return '\0' to ignore the character (optional)
            return '\0';
        }

        // Accept all other characters
        return addedChar;
    }

    private void ScrollToBottom(bool forced = false)
    {
        if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
        scrollCoroutine = StartCoroutine(ScrollToBottomNextFrame(forced));
    }

    float dummy = 0;
    private IEnumerator ScrollToBottomNextFrame(bool forced = false)
    {
        // Wait for end of frame to let the UI layout update
        yield return new WaitForEndOfFrame();

        // Check if the scroll was near the bottom before the update
        if (verticalNormalizedPosition_cached < 0.01f || forced)
        {
            while (scrollRect.verticalNormalizedPosition >= 0.01f)
            {
                scrollRect.verticalNormalizedPosition = Mathf.SmoothDamp(scrollRect.verticalNormalizedPosition, 0, ref dummy, 0.5f);
                yield return new WaitForEndOfFrame();
            }
            scrollRect.verticalNormalizedPosition = 0;
        }
    }

    public void OpenConsole()
    {
        InputManager.Main.SwitchMap(InputMap.Console);
        Show();
        ScrollToBottom(true);
        FocusOnInputField();
    }

    public void CloseConsole()
    {
        InputManager.Main.SwitchToPreviousMap();
        Hide();
    }
    #endregion

    #region Utility
    private string AutoCompleteCommand(string input)
    {
        foreach (string command in commands)
        {
            if (command.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            {
                return command;
            }
        }
        return ""; // Return empty string if no match is found
    }

    private string SuggestCommand(string input)
    {
        var args = input.Split(" ");
        var command = input.Split(" ")[0];
        foreach (string helpCommand in commandHelps)
        {
            if (helpCommand.ToLower().StartsWith(command, StringComparison.OrdinalIgnoreCase))
            {
                // Highlight the argument
                var helpArgs = helpCommand.Split(" ");
                var output = "";
                for (int i = 0; i < helpArgs.Length; i++)
                {
                    if (i == args.Length - 1)
                    {
                        output += "<b>" + helpArgs[i] + "</b> ";
                    }
                    else
                    {
                        output += helpArgs[i] + " ";
                    }
                }

                return output + "<i>>>[TAB]";
            }
        }
        return ""; // Return empty string if no match is found
    }

    // Parser for boolean values entered into command line
    private bool ParseBool(string arg)
    {
        switch (arg)
        {
            case "t":
            case "true":
                return true;
            case "f":
            case "false":
                return false;
            default:
                throw new ArgumentException(UNKNOWN_ARGUMENT + $" '{arg}'");
        }
    }

    private void HandleTabPress()
    {
        var command = AutoCompleteCommand(inputField.text);
        if (command != "") 
            OutputNextFrame(command);
    }

    private void OutputNextFrame(string text)
    {
        StartCoroutine(OutputNextFrameCoroutine(text));
    }
    private IEnumerator OutputNextFrameCoroutine(string text)
    {
        // Wait for end of frame to let the UI layout update
        yield return new WaitForEndOfFrame();

        inputField.text = text;
        FocusOnInputField();
    }

    private void FocusOnInputField()
    {
        // Select the input field
        inputField.Select();

        // Move the cursor to the end of the text
        inputField.caretPosition = inputField.text.Length;

        // Activate the input field
        inputField.ActivateInputField();
    }

    private void UnfocusOnInputField()
    {
        // Deselect the input field
        inputField.DeactivateInputField();
    }
    #endregion
}
