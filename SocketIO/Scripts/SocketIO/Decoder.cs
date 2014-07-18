using UnityEngine;
using System.Collections;
using WebSocketSharp;
using System.Text;
using System;

namespace SocketIO
{
	public class Decoder
	{
		public Packet Decode(MessageEventArgs e)
		{
			Debug.Log("[SocketIO] - Decoding: " + e.Data);

			string data = e.Data;
			Packet packet = new Packet();
			int offset = 0;

			// look up packet type
			int enginePacketType = int.Parse(data.Substring(offset, 1));
			packet.enginePacketType = (EnginePacketType)enginePacketType;

			if (enginePacketType == (int)EnginePacketType.MESSAGE) {
				int socketPacketType = int.Parse(data.Substring(++offset, 1));
				packet.socketPacketType = (SocketPacketType)socketPacketType;
			}

			// connect message properly parsed
			if (data.Length <= 2) {
				Debug.Log("[SocketIO] - Decoded: " + packet);
				return packet;
			}

			// look up namespace (if any)
			if ('/' == data[offset + 1]) {
				StringBuilder builder = new StringBuilder();
				while (offset < data.Length - 1 && data[++offset] != ',') {
					builder.Append(data[offset]);
				}
				packet.nsp = builder.ToString();
			} else {
				packet.nsp = "/";
			}

			// look up id
			char next = data [offset + 1];
			if(next != ' ' && char.IsNumber(next)){
				StringBuilder builder = new StringBuilder();
				while(offset < data.Length - 1){
					char c = data [++offset];
					if (char.IsNumber(c)) {
						builder.Append(c);
					} else {
						--offset;
						break;
					}
				}
				packet.id = int.Parse(builder.ToString());
			}

			// look up json data
			if (++offset < data.Length - 1) {
				try {
					Debug.Log("[SocketIO]: Parsing JSON: " + data.Substring(offset));
					packet.json = new JSONObject(data.Substring(offset));
				} catch(Exception ex){
					Debug.LogException(ex);
				}
			}

			Debug.Log("[SocketIO] - Decoded: " + packet);
			return packet;
		}
	}
}
