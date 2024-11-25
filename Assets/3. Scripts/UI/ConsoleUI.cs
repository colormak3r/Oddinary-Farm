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

    [Header("Settings")]
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
    }

    private void Start()
    {
        InputManager.Main.InputActions.Console.SetCallbacks(this);

        inputField.text = "";
        debugText.text = "";
        suggestionText.text = "";
    }
    private void OnEnable()
    {
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

    private void OnInputValueChanged(string text)
    {
        suggestionText.text = text == "" ? "" : SuggestCommand(text);
    }

    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        // Prevent backtick
        if (addedChar == '`')
        {
            CloseConsole();

            // Return '\0' to ignore the character
            return '\0';
        }

        // Detect and handle Tab key
        if (addedChar == '\t')
        {
            HandleTabPress();

            // Return '\0' to ignore the character
            return '\0';
        }

        // Accept all other characters
        return addedChar;
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

        StartCoroutine(ScrollToBottomNextFrame());

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
                filename = d + "/log-" + t + "-" + r + ".txt";
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

    private IEnumerator ScrollToBottomNextFrame()
    {
        // Wait for end of frame to let the UI layout update
        yield return new WaitForEndOfFrame();

        // Check if the scroll was near the bottom before the update
        if (verticalNormalizedPosition_cached < 0.01f)
        {
            scrollRect.verticalNormalizedPosition = 0;
        }
    }

    public void OpenConsole()
    {
        InputManager.Main.SwitchMap(InputMap.Console);
        Show();
        FocusOnInputField();
    }

    public void CloseConsole()
    {
        InputManager.Main.SwitchToPreviousMap();
        Hide();
    }

    public void OnCloseConsole(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
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

    private const string UNKNOWN_COMMAND = "Unknown command";
    private const string UNKNOWN_ARGUMENT = "Unknown argument";
    private string[] commands =
    {"help",
    "showstacktrace",
    "shownetstat",
    "logtofile"};

    private string[] commandHelps =
    {"help",
    "showstacktrace [bool]",
    "shownetstat [bool]",
    "logtofile [bool]"};

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
            if (command.Contains(commands[0]))
            {
                string builder = "";
                foreach (var cc in commandHelps) builder += "\n>> " + cc;
                Debug.Log($"List of commands:{builder}");
            }
            else if (command.Contains(commands[1]))
            {
                showStackTrace = defaultBool;
            }
            else if (command.Contains(commands[2]))
            {
                NetworkManager.Singleton.gameObject.GetComponent<RuntimeNetStatsMonitor>().Visible = defaultBool;
            }
            else if (command.Contains(commands[3]))
            {
                logToFile = defaultBool;
            }
            else
            {
                OutputNextFrame(command);
                Debug.Log(UNKNOWN_COMMAND + $" '{command}'");
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
        foreach (string command in commandHelps)
        {
            if (command.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            {
                return command;
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
}
