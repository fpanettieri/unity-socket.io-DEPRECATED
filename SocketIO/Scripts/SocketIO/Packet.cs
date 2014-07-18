
using System;
using System.Collections.Generic;

namespace SocketIO
{
	public class Packet : EventArgs
	{
		public EnginePacketType enginePacketType { get; set; }
		public SocketPacketType socketPacketType { get; set; }
		public int attachments { get; set; }
		public string nsp { get; set; }
		public int id { get; set; }
		public JSONObject json { get; set; }

		public Packet() : this(EnginePacketType.UNKNOWN, SocketPacketType.UNKNOWN, -1, "/", -1, null) { }

		public Packet(EnginePacketType enginePacketType, SocketPacketType socketPacketType, int attachments, string nsp, int id, JSONObject json)
		{
			this.enginePacketType = enginePacketType;
			this.socketPacketType = socketPacketType;
			this.attachments = attachments;
			this.nsp = nsp;
			this.id = id;
			this.json = json;
		}

		public override string ToString()
		{
			return string.Format("[Packet: enginePacketType={0}, socketPacketType={1}, attachments={2}, nsp={3}, id={4}, json={5}]", enginePacketType, socketPacketType, attachments, nsp, id, json);
		}

	}
}
