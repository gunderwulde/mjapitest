using UnityEngine;

public class Console : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI console;
    [SerializeField] private int maxLines = 50;
    [SerializeField] private bool showLogType = true;
    
    private string consoleText = "";
    private int lineCount = 0;

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string prefix = "";
        if (showLogType)
        {
            prefix += $"[{type}] ";
        }
        
        string coloredMessage = GetColoredMessage(logString, type);
        string newLine = prefix + coloredMessage + "\n";
        
        consoleText += newLine;
        lineCount++;
        
        // Limitar el número de líneas
        if (lineCount > maxLines)
        {
            int firstLineEnd = consoleText.IndexOf('\n');
            if (firstLineEnd > 0)
            {
                consoleText = consoleText.Substring(firstLineEnd + 1);
                lineCount--;
            }
        }
        
        if (console != null)
        {
            console.text = consoleText;
        }
    }

    string GetColoredMessage(string message, LogType type)
    {
        return type switch
        {
            LogType.Error => $"<color=red>{message}</color>",
            LogType.Assert => $"<color=red>{message}</color>",
            LogType.Warning => $"<color=yellow>{message}</color>",
            LogType.Exception => $"<color=red>{message}</color>",
            _ => message
        };
    }

    public void ClearConsole()
    {
        consoleText = "";
        lineCount = 0;
        if (console != null)
        {
            console.text = "";
        }
    }
}
