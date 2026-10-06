using System;
using System.Net.Sockets;
using UnityEngine;

public class StreamTransmitter : MonoBehaviour {

	[Header("Connection")]
	public string hostURI = "127.0.0.1";
	public TMPro.TMP_InputField hostTextInput;
	public int port = 9000;
	public bool autoConnectOnStart = false;

	[Header("Video")]
	public Camera sourceCamera;
	public int targetWidth = 640;
	public int targetHeight = 480;
	public int frameRate = 30;
	public int jpegQuality = 75;

	[Header("Audio")]
	public bool transmitAudio = true;
	public int audioSampleRate = 22050;
	public string microphoneDeviceName = "";

	private TcpClient client;
	private NetworkStream stream;
	private readonly object connectionLock = new object();
	private float nextFrameTime;
	private AudioClip microphoneClip;
	private int microphonePosition;
	private bool microphoneStarted;

	void Start () {
		if (autoConnectOnStart)
			Connect();
	}

	void Update () {
		if (client == null || stream == null || !client.Connected)
			return;

		if (Time.unscaledTime >= nextFrameTime) {
			nextFrameTime = Time.unscaledTime + (1f / Mathf.Max(1, frameRate));
			SendVideoFrame();
		}

		if (transmitAudio)
			SendMicrophoneAudio();
	}

	void OnDisable () {
		Disconnect();
	}

	void OnDestroy () {
		Disconnect();
	}

	public void Connect () {
		Disconnect();

		string host = hostURI != string.Empty ? hostURI.Trim() : "";
		if (host.Length == 0 && hostTextInput != null)
			host = hostTextInput.text.Trim();

		if (host.Length == 0) {
			Debug.LogWarning("StreamTransmitter: enter a host IP address.");
			return;
		}
		if (port < 1 || port > 65535) {
			Debug.LogWarning("StreamTransmitter: port must be between 1 and 65535.");
			return;
		}

		try {
			client = new TcpClient();
			client.Connect(host, port);
			stream = client.GetStream();
			Debug.Log("StreamTransmitter: connected to " + host + ":" + port);
			if (transmitAudio)
				StartMicrophone();
		} catch (Exception ex) {
			Debug.LogWarning("StreamTransmitter: connection failed - " + ex.Message);
			Disconnect();
		}
	}

	public void Disconnect () {
		lock (connectionLock) {
			if (microphoneClip != null) {
				if (string.IsNullOrEmpty(microphoneDeviceName) == false)
					Microphone.End(microphoneDeviceName);
				microphoneClip = null;
				microphoneStarted = false;
			}

			if (stream != null) {
				stream.Close();
				stream = null;
			}

			if (client != null) {
				client.Close();
				client = null;
			}
		}
	}

	private void StartMicrophone () {
		if (microphoneDeviceName == null)
			microphoneDeviceName = "";

		if (Microphone.devices == null || Microphone.devices.Length == 0) {
			Debug.LogWarning("StreamTransmitter: no microphone detected.");
			return;
		}

		if (string.IsNullOrEmpty(microphoneDeviceName))
			microphoneDeviceName = Microphone.devices[0];

		if (microphoneStarted)
			Microphone.End(microphoneDeviceName);

		microphoneClip = Microphone.Start(microphoneDeviceName, true, 1, audioSampleRate);
		microphonePosition = 0;
		microphoneStarted = true;
	}

	private void SendVideoFrame () {
		if (sourceCamera == null)
			sourceCamera = Camera.main;
		if (sourceCamera == null)
			return;

		RenderTexture previousTarget = sourceCamera.targetTexture;
		RenderTexture tempRenderTexture = RenderTexture.GetTemporary(targetWidth, targetHeight, 24, RenderTextureFormat.Default);
		RenderTexture previousActive = RenderTexture.active;

		try {
			sourceCamera.targetTexture = tempRenderTexture;
			sourceCamera.Render();
			RenderTexture.active = tempRenderTexture;

			Texture2D frameTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
			frameTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0, false);
			frameTexture.Apply();

			byte[] encodedFrame = frameTexture.EncodeToJPG(jpegQuality);
			SendPacket((byte)'V', encodedFrame);

			Destroy(frameTexture);
		} finally {
			sourceCamera.targetTexture = previousTarget;
			RenderTexture.active = previousActive;
			RenderTexture.ReleaseTemporary(tempRenderTexture);
		}
	}

	private void SendMicrophoneAudio () {
		if (microphoneClip == null)
			return;
		if (stream == null || client == null || !client.Connected)
			return;

		int currentPosition = Microphone.GetPosition(microphoneDeviceName);
		if (currentPosition <= microphonePosition)
			return;

		int sampleCount = currentPosition - microphonePosition;
		float[] samples = new float[sampleCount];
		microphoneClip.GetData(samples, microphonePosition);

		byte[] rawAudio = new byte[sampleCount * 2];
		for (int index = 0; index < sampleCount; index++) {
			short sample = (short)Mathf.Clamp(samples[index] * 32767f, short.MinValue, short.MaxValue);
			byte[] littleEndian = BitConverter.GetBytes(sample);
			rawAudio[index * 2] = littleEndian[0];
			rawAudio[index * 2 + 1] = littleEndian[1];
		}

		SendPacket((byte)'A', rawAudio);
		microphonePosition = currentPosition;
	}

	private void SendPacket (byte packetType, byte[] payload) {
		if (stream == null || client == null || !client.Connected)
			return;
		if (payload == null)
			return;

		byte[] header = new byte[5];
		header[0] = packetType;
		header[1] = (byte)((payload.Length >> 24) & 0xFF);
		header[2] = (byte)((payload.Length >> 16) & 0xFF);
		header[3] = (byte)((payload.Length >> 8) & 0xFF);
		header[4] = (byte)(payload.Length & 0xFF);

		lock (connectionLock) {
			try {
				stream.Write(header, 0, header.Length);
				if (payload.Length > 0)
					stream.Write(payload, 0, payload.Length);
				stream.Flush();
			} catch (Exception ex) {
				Debug.LogWarning("StreamTransmitter: send failed - " + ex.Message);
				Disconnect();
			}
		}
	}
}
