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
	// TODO: on connect, store sid
	// If disconnected queue outgoing msgs
	public class SocketIO : MonoBehaviour
	{
		public string url = "ws://127.0.0.1:4567/socket.io/?EIO=3&transport=websocket";
		public bool autoConnect = false;

		private WebSocket ws;
		private Encoder encoder;
		private Decoder decoder;
		private Dictionary<string, List<Action<SocketIOEvent>>> handlers;
		private string sid;

		public WebSocket socket { get { return ws; } }

		public void Start()
		{
			ws = new WebSocket(url);
			encoder = new Encoder();
			decoder = new Decoder();
			handlers = new Dictionary<string, List<Action<SocketIOEvent>>>();
			sid = null;

			ws.OnOpen += OnOpen;
			ws.OnMessage += OnMessage;
			ws.OnError += OnError;
			ws.OnClose += OnClose;

			if(autoConnect){
				Connect();
			}

			On("open", TestOpen);
			On("boop", TestBoop);
			On("error", TestError);
			On("close", TestClose);

			StartCoroutine("BeepBoop");
		}

		private IEnumerator BeepBoop()
		{
			// wait 1 seconds and continue
			yield return new WaitForSeconds(1);

			Emit("beep");

			// wait 3 seconds and continue
			yield return new WaitForSeconds(3);
			
			Emit("beep");

			// wait 2 seconds and continue
			yield return new WaitForSeconds(2);
			
			Emit("beep");

			// wait ONE FRAME and continue
			yield return null;

			Emit("beep");
			Emit("beep");
		}
		
		public void MessageCallback(SocketIOEvent e)
		{
			Debug.Log("[SocketIO] - Message received: " + e.name + " " + e.data);
		}

		public void TestOpen(SocketIOEvent e)
		{
			Debug.Log("[SocketIO] - Open received: " + e.name + " " + e.data);
		}

		public void TestBoop(SocketIOEvent e)
		{
			Debug.Log("[SocketIO] - Pong received: " + e.name + " " + e.data);
		}

		public void TestError(SocketIOEvent e)
		{
			Debug.Log("[SocketIO] - Error received: " + e.name + " " + e.data);
		}

		public void TestClose(SocketIOEvent e)
		{
			Debug.Log("[SocketIO] - Close received: " + e.name + " " + e.data);
		}

		#region Public Methods

		public void Connect()
		{
			ws.Connect();
		}

		public void On(string ev, Action<SocketIOEvent> callback)
		{
			if (handlers.ContainsKey(ev)) {
				handlers[ev].Add(callback);
			} else {
				List<Action<SocketIOEvent>> h = new List<Action<SocketIOEvent>>();
				h.Add(callback);
				handlers[ev] = h;
			}
		}

		public void Off(string ev, Action callback)
		{
			// TODO: implement event notification
		}

		public void Emit(string ev)
		{
			Debug.Log(ev);
			Packet packet = new Packet(EnginePacketType.MESSAGE, SocketPacketType.EVENT, -1, "/", 100, new JSONObject("[\"" + ev + "\"]"));
			ws.Send(encoder.Encode(packet));
		}

		public void Emit(string ev, JSONObject data)
		{
			Packet packet = new Packet(EnginePacketType.MESSAGE, SocketPacketType.EVENT, -1, "/", 10, new JSONObject("[\"" + ev + "\"," + data.ToString() + "]"));
			ws.Send(encoder.Encode(packet));
		}

		public void SetCookie(Cookie cookie)
		{
			ws.SetCookie(cookie);
		}

		public void SetCredentials(string username, string password, bool preAuth)
		{
			ws.SetCredentials(username, password, preAuth);
		}

		#endregion

		#region Private Methods

		private void OnOpen (object sender, EventArgs e)
		{
			EmitEvent("open");
		}

		private void OnMessage(object sender, MessageEventArgs e)
		{
			Debug.Log("[SocketIO] - Raw message: " + e.Data);
			Packet packet = decoder.Decode(e);

			switch (packet.enginePacketType) {
				case EnginePacketType.OPEN:
					HandleOpen(packet);
					break;

				case EnginePacketType.CLOSE: 
					EmitEvent("close"); 
					break;

				case EnginePacketType.MESSAGE: 
					HandleMessage(packet); 
					break;
				
				default:
					Debug.Log("[SocketIO] - Unhandled engine packet type: " + packet);
					break;
			}
		}

		void HandleOpen(Packet packet)
		{
			if(sid == null ){
				Debug.Log("[SocketIO] - Socket.IO sid: " + packet.json["sid"].ToString());
				sid = packet.json["sid"].ToString();
			}
			EmitEvent("open");
		}

		void HandleMessage(Packet packet)
		{
			Debug.Log("TODO: Parse message and distribute to handlers\r\n" + packet);
		}
		
		private void OnError(object sender, ErrorEventArgs e)
		{
			EmitEvent("error");
		}

		private void OnClose(object sender, CloseEventArgs e)
		{
			EmitEvent("close");
		}

		private void EmitEvent(string type)
		{
			if (!handlers.ContainsKey(type)) { return; }
			SocketIOEvent e = new SocketIOEvent(type);
			foreach (Action<SocketIOEvent> handler in this.handlers[type]) {
				handler(e);
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
	}
}
