using UnityEngine;

public static class Debug
{
    public static void Log(object message)
    {
#if !UNITY_IOS && UNITY_ANDROID
        UnityEngine.Debug.Log(message);
#endif
    }
    public static void Log(object message, UnityEngine.Object context)
    {
#if !UNITY_IOS && UNITY_ANDROID
        UnityEngine.Debug.Log(message, context);
#endif
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
#if !UNITY_IOS && UNITY_ANDROID
        UnityEngine.Debug.LogException(exception);
#endif
    }
    public static void LogException(System.Exception exception, UnityEngine.Object context)
    {
#if !UNITY_IOS && UNITY_ANDROID
        UnityEngine.Debug.LogException(exception, context);
#endif
    }
    public static void LogWarning(object message)
    {
#if !UNITY_IOS && UNITY_ANDROID
        UnityEngine.Debug.LogWarning(message);
#endif
    }
    public static void LogWarning(object message, UnityEngine.Object context)
    {
#if !UNITY_IOS && UNITY_ANDROID
        UnityEngine.Debug.LogWarning(message, context);
#endif
    }
}