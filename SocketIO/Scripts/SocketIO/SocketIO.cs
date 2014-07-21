#region License
/*
 * SocketIO.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2014 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

//#define SOCKET_IO_DEBUG			// Uncomment this for debug
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace SocketIO
{
	/**
	 * SocketIO Component
	 */
	// TODO: If disconnected queue outgoing msgs
	public class SocketIO : MonoBehaviour
	{
		public string url = "ws://127.0.0.1:4567/socket.io/?EIO=3&transport=websocket";
		public bool autoConnect = false;
		private WebSocket ws;
		private Encoder encoder;
		private Decoder decoder;
		private Parser parser;
		private Dictionary<string, List<Action<SocketIOEvent>>> handlers;
		private string sid;

		public WebSocket socket { get { return ws; } }

		public void Start()
		{
			ws = new WebSocket(url);
			encoder = new Encoder();
			decoder = new Decoder();
			parser = new Parser();
			handlers = new Dictionary<string, List<Action<SocketIOEvent>>>();
			sid = null;

			ws.OnOpen += OnOpen;
			ws.OnMessage += OnMessage;
			ws.OnError += OnError;
			ws.OnClose += OnClose;

			if (autoConnect) {
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
			Debug.Log("[SocketIO] Message received: " + e.name + " " + e.data);
		}

		public void TestOpen(SocketIOEvent e)
		{
			Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
		}

		public void TestBoop(SocketIOEvent e)
		{
			Debug.Log("[SocketIO] Boop received: " + e.name + " " + e.data);
		}

		public void TestError(SocketIOEvent e)
		{
			Debug.Log("[SocketIO] Error received: " + e.name + " " + e.data);
		}

		public void TestClose(SocketIOEvent e)
		{
			Debug.Log("[SocketIO] Close received: " + e.name + " " + e.data);
		}

		#region Public Methods

		public void Connect()
		{
			ws.Connect();
		}

		public void On(string ev, Action<SocketIOEvent> callback)
		{
			if (handlers.ContainsKey(ev)) {
				handlers [ev].Add(callback);
			} else {
				List<Action<SocketIOEvent>> l = new List<Action<SocketIOEvent>>();
				l.Add(callback);
				handlers [ev] = l;
			}
		}

		public void Off(string ev, Action<SocketIOEvent> callback)
		{
			if (!handlers.ContainsKey(ev)) {
				Debug.LogWarning("[SocketIO] No callbacks registered for event: " + ev);
				return;
			}

			List<Action<SocketIOEvent>> l = handlers [ev];
			if (!l.Contains(callback)) {
				Debug.LogWarning("[SocketIO] Couldn't remove callback action for event: " + ev);
				return;
			}

			l.Remove(callback);
			if (l.Count == 0) {
				handlers.Remove(ev);
			}
		}

		public void Emit(string ev)
		{
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

		private void OnOpen(object sender, EventArgs e)
		{
			EmitEvent("open");
		}

		private void OnMessage(object sender, MessageEventArgs e)
		{
			#if SOCKET_IO_DEBUG
			Debug.Log("[SocketIO] Raw message: " + e.Data);
			#endif
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
					Debug.LogError("[SocketIO] Unhandled engine packet type: " + packet);
					break;
			}
		}

		void HandleOpen(Packet packet)
		{
			if (sid == null) {
				#if SOCKET_IO_DEBUG
				Debug.Log("[SocketIO] Socket.IO sid: " + packet.json["sid"].ToString());
				#endif
				sid = packet.json ["sid"].ToString();
			}
			EmitEvent("open");
		}

		void HandleMessage(Packet packet)
		{
			if (packet.json == null) {
				return;
			}

			SocketIOEvent e = parser.Parse(packet.json);
			EmitEvent(e);
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
			EmitEvent(new SocketIOEvent(type));
		}

		private void EmitEvent(SocketIOEvent ev)
		{
			if (!handlers.ContainsKey(ev.name)) {
				return;
			}
			foreach (Action<SocketIOEvent> handler in this.handlers[ev.name]) {
				handler(ev);
			}
		}

		#endregion
	}
}
