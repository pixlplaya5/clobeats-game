using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;
using UnityEngine;
using System.Threading.Tasks;

public class DebugSender : MonoBehaviour
{
    TcpClient client;
    StreamWriter writer;

    public float connectionAttempts = 0f;
    public float connectionAttemptLimit = 5f;
    bool isConnected = false;

    public float attemptEverySeconds = 1f;


    void Start()
    {
        StartCoroutine(AttemptConnectionLoop());
    }

    public IEnumerator AttemptConnectionLoop()
    {
        while (!isConnected && connectionAttempts != connectionAttemptLimit)
        {
            float elapsedTime = 0f;
            while (elapsedTime < attemptEverySeconds)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            elapsedTime = 0f;
            yield return TryConnect();
            yield return null;
        }
        yield return null;
        UnityEngine.Debug.LogWarning("Debug console connection time out.");
    }

    void OnLog(string condition, string stackTrace, LogType type)
    {
        if (writer == null)
            return;
        if (condition.StartsWith("$beep"))
        {
            writer.WriteLine(condition);
        }
        else
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                writer.WriteLine($"[{Time.time}]: [{type}]: \"{condition}\"\r\n{stackTrace}");
            }
            else
            {
                writer.WriteLine($"[{Time.time}]: [{type}]: \"{condition}\""); // dont write stack trace if severity is low to keep debug console clean
            }
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;

        writer?.Dispose();
        client?.Close();
    }

    private async Task TryConnect()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 5000);
            if (client != null)
            {
                writer = new StreamWriter(client.GetStream());
                writer.AutoFlush = true;
                Application.logMessageReceived += OnLog;
            }
            await Task.Yield();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning("Failed to connect: " + ex.Message); // warning to allow other scripts to execute
            connectionAttempts++;
        }
    }

}