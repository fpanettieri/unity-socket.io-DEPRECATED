#region License
/*
 * SocketIO.cs
 *
 * The MIT License
 *
 * Copyright (c) 2014 Fabio Panettieri
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
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace SocketIO
{
	public class SocketIOComponent : MonoBehaviour
	{
		#region Public Properties

		public string url = "ws://127.0.0.1:4567/socket.io/?EIO=3&transport=websocket";
		public bool autoConnect = false;
		public float ackExpirationTime = 30f;
		public WebSocket socket { get { return ws; } }
		public string sid { get; set; }

		#endregion

		#region Private Properties

		private WebSocket ws;
		private Encoder encoder;
		private Decoder decoder;
		private Parser parser;
		private Dictionary<string, List<Action<SocketIOEvent>>> handlers;
		private Dictionary<int, Action<JSONObject>> acknowledges;
		private int packetId;

		#endregion

		#region Public Methods

		public void Awake()
		{
			ws = new WebSocket(url);
			encoder = new Encoder();
			decoder = new Decoder();
			parser = new Parser();
			handlers = new Dictionary<string, List<Action<SocketIOEvent>>>();
			acknowledges = new Dictionary<int, Action<JSONObject>>();
			sid = null;
			packetId = 1;

			ws.OnOpen += OnOpen;
			ws.OnMessage += OnMessage;
			ws.OnError += OnError;
			ws.OnClose += OnClose;

			// TODO: start acknowledges garbage collection coroutine in X seconds
		}

		public void Start()
		{
			if (autoConnect) { Connect(); }
		}

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
			EmitPacket(++packetId, "[\"" + ev + "\"]");
		}

		public void Emit(string ev, JSONObject data)
		{
			EmitPacket(++packetId, "[\"" + ev + "\"," + data.ToString() + "]");
		}

		public void Emit(string ev, JSONObject data, Action<JSONObject> ack)
		{
			acknowledges[++packetId] = ack;
			EmitPacket(packetId, "[\"" + ev + "\"," + data.ToString() + "]");
		}

		#endregion

		#region Private Methods

		private void EmitPacket(int id, string raw)
		{
			Packet packet = new Packet(EnginePacketType.MESSAGE, SocketPacketType.EVENT, 0, "/", id, new JSONObject(raw));
			try {
				ws.Send(encoder.Encode(packet));
			} catch(SocketIOException ex) {
				Debug.LogException(ex);
			}
		}

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
				Debug.Log("[SocketIO] Socket.IO sid: " + packet.json["sid"].str);
				#endif
				sid = packet.json ["sid"].str;
			}
			EmitEvent("open");
		}

		void HandleMessage(Packet packet)
		{
			if (packet.json == null) {
				return;
			}

			if(packet.socketPacketType == SocketPacketType.ACK){
				if(!acknowledges.ContainsKey(packet.id)){ 
					#if SOCKET_IO_DEBUG
					Debug.Log("[SocketIO] Ack received for invalid Action: " + packet.id);
					#endif
					return; 
				}

				Action<JSONObject> ack = acknowledges[packet.id];
				acknowledges.Remove(packet.id);
				ack.Invoke(packet.json);
				return;
			}

			if (packet.socketPacketType == SocketPacketType.EVENT) {
				SocketIOEvent e = parser.Parse (packet.json);
				EmitEvent (e);
			}
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
			if (!handlers.ContainsKey(ev.name)) { return; }
			foreach (Action<SocketIOEvent> handler in this.handlers[ev.name]) {
				try{
					handler(ev);
				} catch(Exception ex){
					Debug.LogException(ex);
				}
			}
		}

		#endregion
	}
}
