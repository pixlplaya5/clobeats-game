using System.Collections;
using System.Collections.Generic;
using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class StreamReceiver : MonoBehaviour {

	public string statusText;
	public TMPro.TextMeshProUGUI statusTextMesh;
	public string hostURI;
	public TMPro.TMP_InputField hostTextInput;
	public int port = 9000;
	public Renderer videoRenderer;
	public UnityEngine.UI.RawImage videoImage;
	public AudioSource audioSource;
	public int audioSampleRate = 22050;

	private readonly object stateLock = new object();
	private readonly object frameLock = new object();
	private readonly object audioLock = new object();
	private TcpClient activeClient;
	private Thread receiverThread;
	private int connectionId;
	private string pendingStatus;
	private byte[] pendingFrame;
	private Texture2D videoTexture;
	private AudioClip streamingClip;
	private float[] audioBuffer;
	private int audioReadPosition;
	private int audioWritePosition;
	private int audioBufferedSamples;

	void OnEnable () {
		if (audioSource == null)
			audioSource = GetComponent<AudioSource>();
		Button connectButton = FindAnyObjectByType<Button>();
		connectButton.onClick.AddListener(Connect);
	}

	void Update () {
		statusTextMesh.text = statusText;
		hostURI = hostTextInput.text;
		lock (stateLock) {
			if (pendingStatus != null) {
				statusText = pendingStatus;
				pendingStatus = null;
			}
		}

		byte[] frame = null;
		lock (frameLock) {
			frame = pendingFrame;
			pendingFrame = null;
		}

		if (frame != null) {
			if (videoTexture == null)
				videoTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);

			if (videoTexture.LoadImage(frame)) {
				if (videoRenderer != null)
					videoRenderer.material.mainTexture = videoTexture;
				if (videoImage != null)
					videoImage.texture = videoTexture;
			}
		}
	}

	public void Connect () {
		string host = hostURI != string.Empty ? hostURI.Trim() : "";
		if (host.Length == 0) {
			SetStatus("Enter the host IP address.");
			return;
		}
		if (port < 1 || port > 65535) {
			SetStatus("Port must be between 1 and 65535.");
			return;
		}
		if (audioSampleRate < 1) {
			SetStatus("Audio sample rate must be positive.");
			return;
		}

		Disconnect();
		int id = ++connectionId;
		ResetAudioBuffer();
		if (audioSource != null) {
			streamingClip = AudioClip.Create("Network Audio", audioSampleRate * 2, 1, audioSampleRate, true, ReadAudio);
			audioSource.loop = true;
			audioSource.clip = streamingClip;
			audioSource.Play();
		}

		SetStatus("Connecting to " + host + ":" + port + "...");
		receiverThread = new Thread(delegate() { ReceiveStream(host, port, id); });
		receiverThread.IsBackground = true;
		receiverThread.Start();
	}

	public void Disconnect () {
		++connectionId;
		TcpClient client = activeClient;
		activeClient = null;
		if (client != null)
			client.Close();
		if (audioSource != null)
			audioSource.Stop();
		statusText = "Disconnected.";
	}

	void OnDestroy () {
		Disconnect();
	}

	private void ReceiveStream(string host, int hostPort, int id) {
		TcpClient client = new TcpClient();
		string endStatus = "Stream ended.";
		try {
			client.Connect(host, hostPort);
			if (id != connectionId) {
				client.Close();
				return;
			}

			activeClient = client;
			SetStatus("Connected to " + host + ":" + hostPort + ". Waiting for stream...");
			NetworkStream stream = client.GetStream();
			byte[] header = new byte[5];
			while (id == connectionId && ReadExactly(stream, header, 5)) {
				int length = ((header[1] & 255) << 24) | ((header[2] & 255) << 16) |
					((header[3] & 255) << 8) | (header[4] & 255);
				if (length <= 0 || length > 4194304)
					throw new Exception("Invalid stream packet size.");

				byte[] payload = new byte[length];
				if (!ReadExactly(stream, payload, length))
					break;

				if (header[0] == (byte)'V') {
					lock (frameLock)
						pendingFrame = payload;
				} else if (header[0] == (byte)'A') {
					if (length > 262144)
						throw new Exception("Audio packet is too large.");
					BufferAudio(payload, length);
				} else {
					throw new Exception("Unknown stream packet type.");
				}
			}
		} catch (Exception exception) {
			endStatus = "Stream error: " + exception.Message;
		} finally {
			client.Close();
			if (activeClient == client)
				activeClient = null;
			if (id == connectionId)
				SetStatus(endStatus);
		}
	}

	private bool ReadExactly(NetworkStream stream, byte[] buffer, int count) {
		int offset = 0;
		while (offset < count) {
			int received = stream.Read(buffer, offset, count - offset);
			if (received == 0)
				return false;
			offset += received;
		}
		return true;
	}

	private void BufferAudio(byte[] data, int length) {
		if (audioBuffer == null || audioSampleRate <= 0)
			return;

		lock (audioLock) {
			for (int index = 0; index + 1 < length; index += 2) {
				float sample = (short)(data[index] | (data[index + 1] << 8)) / 32768.0f;
				if (audioBufferedSamples == audioBuffer.Length) {
					audioReadPosition = (audioReadPosition + 1) % audioBuffer.Length;
					audioBufferedSamples--;
				}
				audioBuffer[audioWritePosition] = sample;
				audioWritePosition = (audioWritePosition + 1) % audioBuffer.Length;
				audioBufferedSamples++;
			}
		}
	}

	private void ReadAudio(float[] data) {
		lock (audioLock) {
			for (int index = 0; index < data.Length; index++) {
				if (audioBufferedSamples == 0) {
					data[index] = 0.0f;
				} else {
					data[index] = audioBuffer[audioReadPosition];
					audioReadPosition = (audioReadPosition + 1) % audioBuffer.Length;
					audioBufferedSamples--;
				}
			}
		}
	}

	private void ResetAudioBuffer() {
		lock (audioLock) {
			audioBuffer = new float[Mathf.Max(audioSampleRate * 2, 1)];
			audioReadPosition = 0;
			audioWritePosition = 0;
			audioBufferedSamples = 0;
		}
	}

	private void SetStatus(string message) {
		lock (stateLock)
			pendingStatus = message;
		NotificationHandler notification = FindAnyObjectByType<NotificationHandler>();
		if (!notification.notifShowing)
		{
			notification.ShowNotif("StreamReceiver:<br>" + message.ToString());
		}
		else
		{
			notification.SetNotifText("StreamReceiver:<br>" + message.ToString());
		}
        
	}
}
