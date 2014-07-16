using UnityEngine;
using System.Collections;
using WebSocketSharp;
using System;
using WebSocketSharp.Net;
using System.Collections.Generic;
using System.Net.Security;

namespace SocketIO
{
	/**
	 * SocketIO Component
	 */ 
	public class SocketIO : MonoBehaviour
	{
		public string url = "ws://127.0.0.1:4567/socket.io/?EIO=2&transport=websocket";

		private WebSocket ws;
		private Encoder encoder;
		private Decoder decoder;
		private bool handlingMessages;

		public WebSocket socket { get { return ws; } }

		public void Start()
		{
			ws = new WebSocket(url);
			encoder = new Encoder();
			decoder = new Decoder();
			msgHandlers = new List<EventHandler<Packet>>();
				
			// Test purpose
			OnMessage += MessageCallback;
			Connect();
			Send("this is a test");
		}

		public void MessageCallback(object sender, Packet p)
		{
			Debug.Log("[SocketIO] - Message received: " + p);
		}

		#region Public Methods

		public void Close()
		{
			ws.Close();
		}

		public void CloseAsync()
		{
			ws.CloseAsync();
		}

		public void Connect()
		{
			ws.Connect();
		}

		public void ConnectAsync()
		{
			ws.ConnectAsync();
		}

		public void Ping()
		{
			ws.Ping();
		}

		public void Send(string data)
		{	
			ws.Send(encoder.Encode(data));
		}

		public void SendAsync (string data, Action<bool> completed)
		{
			ws.SendAsync(data, completed);
		}

		public void SetCookie (Cookie cookie)
		{
			ws.SetCookie(cookie);
		}

		public void SetCredentials (string username, string password, bool preAuth)
		{
			ws.SetCredentials(username, password, preAuth);
		}

		public void On(string ev, Action callback)
		{
			// TODO: implement event notification
		}

		public void Off(string ev, Action callback)
		{
			// TODO: implement event notification
		}

		#endregion

		#region Private Methods

		private void DecodeMessage(object sender, MessageEventArgs e)
		{
			Packet packet = decoder.Decode(e);

			for (int i = 0; i < msgHandlers.Count; i++) {
				msgHandlers[i].Invoke(sender, packet);
			}
		}

		#endregion

		#region Public Properties

		public CompressionMethod Compression {
			get { return ws.Compression; }
			set { ws.Compression = value; }
		}

		public IEnumerable<Cookie> Cookies {
			get { return ws.Cookies; }
		}

		public NetworkCredential Credentials {
			get { return ws.Credentials; }
		}

		public string Extensions {
			get { return ws.Extensions; }
		}

		public bool IsAlive {
			get { return ws.IsAlive; }
		}

		public bool IsSecure {
			get { return ws.IsSecure; }
		}

		public Logger Log {
			get { return ws.Log; }
		}

		public string Origin {
			get { return ws.Origin; }
			set { ws.Origin = value; }
		}

		public string Protocol {
			get { return ws.Protocol; }
		}

		public WebSocketState ReadyState {
			get { return ws.ReadyState; }
		}

		public RemoteCertificateValidationCallback ServerCertificateValidationCallback {
			get { return ws.ServerCertificateValidationCallback; }
			set { ws.ServerCertificateValidationCallback = value; }
		}

		public Uri Url {
			get { return ws.Url; }
		}

		#endregion
		
		#region Public Events

		public event EventHandler<CloseEventArgs> OnClose {
			add { ws.OnClose += value; }
			remove { ws.OnClose -= value; }
		}

		public event EventHandler<ErrorEventArgs> OnError {
			add { ws.OnError += value; }
			remove { ws.OnError -= value; }
		}

		public event EventHandler<Packet> OnMessage {
			add {
				msgHandlers.Add(value);
				if(handlingMessages){ return; }
				ws.OnMessage += DecodeMessage;
				handlingMessages = true;

			}
			remove {
				msgHandlers.Remove(value);
				if(!handlingMessages){ return; }
				ws.OnMessage -= DecodeMessage;
				handlingMessages = false;
			}
		}

		public event EventHandler OnOpen {
			add { ws.OnOpen += value; }
			remove { ws.OnOpen -= value; }
		}
		
		#endregion

		#region Private Events

		private List<EventHandler<Packet>> msgHandlers;

		#endregion
	}
}
