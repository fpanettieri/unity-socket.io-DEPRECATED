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
using System.Threading;
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
		// public float ackExpirationTime = 30f;  // NOT IMPLEMENTED YET
		public int reconnectDelay = 5;
		public WebSocket socket { get { return ws; } }
		public string sid { get; set; }

		#endregion

		#region Private Properties

		private Thread socketThread;
		private volatile bool connected;
		private WebSocket ws;

		private Encoder encoder;
		private Decoder decoder;
		private Parser parser;
		private Dictionary<string, List<Action<SocketIOEvent>>> handlers;
		private Dictionary<int, Action<JSONObject>> acknowledges;
		private int packetId;

		private object eventQueueLock;
		private Queue<SocketIOEvent> eventQueue;

		private object packetQueueLock;
		private Queue<Packet> packetQueue;

		#endregion

		#if SOCKET_IO_DEBUG
		public Action<string> debugMethod;
		#endif

		#region Unity interface

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

			eventQueueLock = new object();
			eventQueue = new Queue<SocketIOEvent>();

			packetQueueLock = new object();
			packetQueue = new Queue<Packet>();

			connected = false;
		}

		public void Start()
		{
			#if SOCKET_IO_DEBUG
			if(debugMethod == null) { debugMethod = Debug.Log; };
			#endif
			if (autoConnect) { Connect(); }
		}

		public void Update()
		{
			lock(eventQueueLock){ 
				while(eventQueue.Count > 0){
					EmitEvent(eventQueue.Dequeue());
				}
			}

			lock(packetQueueLock){
				while(packetQueue.Count > 0){
					Packet packet = packetQueue.Dequeue();
					Action<JSONObject> ack = acknowledges[packet.id];
					acknowledges.Remove(packet.id);
					ack.Invoke(packet.json);
				}
			}

			// TODO: acknowledges garbage collection coroutine every X seconds
		}

		public void OnDestroy()
		{
			if (socketThread == null) { return; }
			socketThread.Abort();
		}

		public void OnApplicationQuit()
		{
			Close();
		}

		#endregion

		#region Public Methods
		
		public void Connect()
		{
			connected = true;
			socketThread = new Thread(DoConnect);
			socketThread.Start(ws);
		}
		
		private void DoConnect(object obj)
		{
			WebSocket webSocket = (WebSocket)obj;
			while(connected){
				if(webSocket.IsConnected){
					Thread.Sleep(reconnectDelay);
				} else {
					webSocket.Connect();
				}
			}
			webSocket.Close();
		}

		public void Close()
		{
			EmitClose();
			connected = false;
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
				#if SOCKET_IO_DEBUG
				debugMethod.Invoke("[SocketIO] No callbacks registered for event: " + ev);
				#endif
				return;
			}

			List<Action<SocketIOEvent>> l = handlers [ev];
			if (!l.Contains(callback)) {
				#if SOCKET_IO_DEBUG
				debugMethod.Invoke("[SocketIO] Couldn't remove callback action for event: " + ev);
				#endif
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

		public void Emit(string ev, Action<JSONObject> ack)
		{
			acknowledges[++packetId] = ack;
			EmitPacket(packetId, "[\"" + ev + "\"]");
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
			#if SOCKET_IO_DEBUG
			debugMethod.Invoke("[SocketIO] Emit: " + raw);
			#endif

			Packet packet = new Packet(EnginePacketType.MESSAGE, SocketPacketType.EVENT, 0, "/", id, new JSONObject(raw));
			try {
				ws.Send(encoder.Encode(packet));
			} catch(SocketIOException ex) {
				#if SOCKET_IO_DEBUG
				debugMethod.Invoke(ex.ToString());
				#endif
			}
		}

		private void EmitClose()
		{
			#if SOCKET_IO_DEBUG
			debugMethod.Invoke("[SocketIO] Closing socket");
			#endif
			
			Packet closeSocketPacket = new Packet(EnginePacketType.MESSAGE, SocketPacketType.DISCONNECT, 0, "/", ++packetId, new JSONObject(""));
			Packet closeEnginePacket = new Packet(EnginePacketType.CLOSE, SocketPacketType.DISCONNECT, 0, "/", ++packetId, new JSONObject(""));
			try {
				ws.Send(encoder.Encode(closeSocketPacket));
				ws.Send(encoder.Encode(closeEnginePacket));
			} catch(SocketIOException ex) {
				#if SOCKET_IO_DEBUG
				debugMethod.Invoke(ex.ToString());
				#endif
			}
		}

		private void OnOpen(object sender, EventArgs e)
		{
			EmitEvent("open");
		}

		private void OnMessage(object sender, MessageEventArgs e)
		{
			#if SOCKET_IO_DEBUG
			debugMethod.Invoke("[SocketIO] Raw message: " + e.Data);
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
					#if SOCKET_IO_DEBUG
					debugMethod.Invoke("[SocketIO] Unhandled engine packet type: " + packet);
					#endif
					break;
			}
		}

		private void HandleOpen(Packet packet)
		{
			#if SOCKET_IO_DEBUG
			debugMethod.Invoke("[SocketIO] Socket.IO sid: " + packet.json["sid"].str);
			#endif
			sid = packet.json["sid"].str;
			EmitEvent("open");
		}

		private void HandleMessage(Packet packet)
		{
			if (packet.json == null) {
				return;
			}

			if(packet.socketPacketType == SocketPacketType.ACK){
				if(!acknowledges.ContainsKey(packet.id)){ 
					#if SOCKET_IO_DEBUG
					debugMethod.Invoke("[SocketIO] Ack received for invalid Action: " + packet.id);
					#endif
					return; 
				}
				lock(packetQueueLock){ packetQueue.Enqueue(packet); }
				return;
			}

			if (packet.socketPacketType == SocketPacketType.EVENT) {
				SocketIOEvent e = parser.Parse(packet.json);
				lock(eventQueueLock){ eventQueue.Enqueue(e); }
			}

			// TODO: queue this and dispatch them from the "update" method
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
					#if SOCKET_IO_DEBUG
					debugMethod.Invoke(ex.ToString());
					#endif
				}
			}
		}

		#endregion
	}
}
