using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using TMPro;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleUI : UIBehaviour, DefaultInputActions.IConsoleActions
{
    public static ConsoleUI Main;

    private static string SHOW_STACK_TRACE = "ShowStackTrace";
    private static string LOG_TO_FILE = "LogToFile";

    [Header("Settings")]
    [SerializeField]
    private string version = "v0.1";
    [SerializeField]
    private string versionNumber = ".01";
    [SerializeField]
    private int maxChar = 100000;
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
        if (Main == null)
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

    private void Start()
    {
        InputManager.Main.InputActions.Console.SetCallbacks(this);

        inputField.text = "";
        debugText.text = "";
        suggestionText.text = "";
    }
    protected override void OnEnable()
    {
        base.OnEnable();

        inputField.onValueChanged.AddListener(OnInputValueChanged);
        inputField.onValidateInput += ValidateInput;
        Application.logMessageReceived += HandleLogMessageReceived;
    }

    private void OnDisable()
    {
        inputField.onValueChanged.RemoveListener(OnInputValueChanged);
        inputField.onValidateInput -= ValidateInput;
        Application.logMessageReceived -= HandleLogMessageReceived;
    }

    private void HandleLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<color=#")
          .Append(ColorUtility.ToHtmlStringRGB(timeTagColor))
          .Append(">[")
          .Append(Helper.FormatTimeHHMMSS(Time.time))
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

        sb.Append("<color=#")
          .Append(ColorUtility.ToHtmlStringRGB(typeColor))
          .Append(">[")
          .Append(type.ToString())
          .Append("]\n</color>");

        sb.Append("<color=#")
          .Append(ColorUtility.ToHtmlStringRGB(conditionColor))
          .Append(">")
          .Append(condition)
          .Append("</color>");

        if (showStackTrace || type == LogType.Error)
        {
            sb.Append("\n")
              .Append(stackTrace);
        }
        sb.Append("\n");

        UpdateLog(sb.ToString());
    }

    private void UpdateLog(string newLog)
    {
        verticalNormalizedPosition_cached = scrollRect.verticalNormalizedPosition;
        log += newLog + "\n";
        if (log.Length > maxChar)
            log = log.Substring(log.Length - maxChar);

        debugText.text = log;

        ScrollToBottom();

        if (logToFile)
        {
            if (filename == "")
            {
                string d = System.Environment.GetFolderPath(
                   System.Environment.SpecialFolder.Desktop) + "/ODD_LOGS";
                System.IO.Directory.CreateDirectory(d);
                DateTime now = DateTime.Now;
                // Format the DateTime as MMDDYY-HHMMSS
                string t = now.ToString("MMddyy-HHmmss");
                string r = UnityEngine.Random.Range(1000, 9999).ToString();
                filename = d + "/log-" + t + "-" + r + "-" + version + versionNumber + ".txt";
            }

            try
            {
                string pattern = "<.*?>";
                string resultString = Regex.Replace(newLog, pattern, "");
                System.IO.File.AppendAllText(filename, resultString + "\n");
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
    }

    private const string UNKNOWN_COMMAND = "Unknown command";
    private const string UNKNOWN_ARGUMENT = "Unknown argument";
    private string[] commands =
    {"Help",
    "ShowStackTrace",
    "ShowNetStat",
    "LogToFile",
    "Spawn",
    "PrintItemIdList"};

    private string[] commandHelps =
    {"Help",
    "ShowStackTrace [bool]",
    "ShowNetStat [bool]",
    "LogToFile [bool]",
    "Spawn [id] [x] [y] [count]",
    "PrintItemIdList"};

    private void ParseCommand(string command)
    {
        command = command.ToLower();
        var args = command.Split(" ");

        // Set default value for 1-argument commands
        var defaultBool = true;
        if (args.Length == 2)
        {
            defaultBool = ParseBool(args[1]);
        }

        // Clear the input field
        Debug.Log(">> " + command);
        inputField.text = "";

        // Parse the command
        try
        {
            if (command.Contains(commands[0].ToLower()))
            {
                string builder = "";
                foreach (var cc in commandHelps) builder += "\n>> " + cc;
                Debug.Log($"List of commands:{builder}");
            }
            else if (command.Contains(commands[1].ToLower()))
            {
                showStackTrace = defaultBool;
                PlayerPrefs.SetInt(SHOW_STACK_TRACE, showStackTrace ? 1 : 0);
            }
            else if (command.Contains(commands[2].ToLower()))
            {
                NetworkManager.Singleton.gameObject.GetComponent<RuntimeNetStatsMonitor>().Visible = defaultBool;
            }
            else if (command.Contains(commands[3].ToLower()))
            {
                logToFile = defaultBool;
                PlayerPrefs.SetInt(LOG_TO_FILE, logToFile ? 1 : 0);
            }
            else if (command.Contains(commands[4].ToLower()))
            {
                if (AssetManager.Main == null) throw new Exception("AssetManager not found. Has the game started yet?");

                AssetManager.Main.SpawnByID(int.Parse(args[1]), new Vector2(float.Parse(args[2]), float.Parse(args[3])), int.Parse(args[4]), true);
            }
            else if (command.Contains(commands[5].ToLower()))
            {
                if (AssetManager.Main == null) throw new Exception("AssetManager not found. Has the game started yet?");
                AssetManager.Main.PrintItemIDs();
            }
            else
            {
                OutputNextFrame(command);
                throw new ArgumentException(UNKNOWN_COMMAND + $" '{command}'");
            }
        }
        catch (IndexOutOfRangeException)
        {
            OutputNextFrame(command);
            Debug.Log("Incorrect number of arguments");
        }
        catch (Exception e)
        {
            OutputNextFrame(command);
            Debug.Log(e.Message);
        }
    }


    #region  Input Actions

    public void OnCloseConsole(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed && !IsAnimating)
        {
            CloseConsole();
        }
    }

    public void OnSubmit(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (inputField.text != "")
                ParseCommand(inputField.text);

            FocusOnInputField();
            scrollRect.verticalNormalizedPosition = 0;
        }
    }

    public void OnAutoComplete(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleTabPress();
        }
    }

    public void OnScrollToBottom(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ScrollToBottom(true);
        }
    }

    #endregion

    #region UI features

    private void OnInputValueChanged(string text)
    {
        suggestionText.text = text == "" ? "" : SuggestCommand(text);
    }

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
        if (scrollCoroutine != null)
            StopCoroutine(scrollCoroutine);
        StartCoroutine(ScrollToBottomNextFrame(forced));
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
        foreach (string helpCommand in commandHelps)
        {
            if (helpCommand.ToLower().StartsWith(input, StringComparison.OrdinalIgnoreCase))
            {
                return helpCommand + " <i>>>[TAB]";
            }
        }
        return ""; // Return empty string if no match is found
    }

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
        if (command != "") OutputNextFrame(command);
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

    #endregion
}
