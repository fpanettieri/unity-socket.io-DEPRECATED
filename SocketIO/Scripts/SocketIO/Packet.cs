
using System;
using System.Collections.Generic;

namespace SocketIO
{
	public class Packet : EventArgs
	{
		public PacketType type { get; set; }
		public int attachments { get; set; }
		public string nsp { get; set; }
		public int id { get; set; }
		public Object json { get; set; }

		public Packet()
		{
			type = PacketType.UNKNOWN;
			attachments = -1;
			nsp = "/";
			id = -1;
			json = new Object();
		}

		public override string ToString()
		{
			return string.Format("[Packet: type={0}, attachments={1}, nsp={2}, id={3}, json={4}]", type, attachments, nsp, id, json);
		}

	}
}
