using UnityEngine;

public static class Debug
{
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }
    public static void Log(object message, UnityEngine.Object context)
    {
        UnityEngine.Debug.Log(message, context);
    }
    public static void LogError(object message)
    {
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        UnityEngine.Debug.LogError(message);

    }
    public static void LogError(object message, UnityEngine.Object context)
    {
        UnityEngine.Debug.LogError(message, context);
    }
    public static void LogException(System.Exception exception)
    {
        UnityEngine.Debug.LogException(exception);
    }
    public static void LogException(System.Exception exception, UnityEngine.Object context)
    {
        UnityEngine.Debug.LogException(exception, context);
    }
    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }
    public static void LogWarning(object message, UnityEngine.Object context)
    {
        UnityEngine.Debug.LogWarning(message, context);
    }
}