using UnityEngine;
using System.Collections;
using System.Text;

namespace SocketIO
{
	public class Encoder
	{
		public string Encode(Packet packet)
		{
			Debug.Log("[SocketIO] - Encoding: " + packet.json);

			StringBuilder builder = new StringBuilder();

			// first is type
			builder.Append((int)packet.enginePacketType);
			builder.Append((int)packet.socketPacketType);

			// attachments if we have them
			if (packet.socketPacketType == SocketPacketType.BINARY_EVENT || packet.socketPacketType == SocketPacketType.BINARY_ACK) {
				builder.Append(packet.attachments);
				builder.Append('-');
			}

			// if we have a namespace other than '/'
			// we append it followed by a comma ','
			if (!string.IsNullOrEmpty(packet.nsp) && !packet.nsp.Equals("/")) {
				builder.Append(packet.nsp);
				builder.Append(',');
			}

			// immediately followed by the id
			if (packet.id > -1) {
				builder.Append(packet.id);
			}

			if (packet.json != null) {
				builder.Append(packet.json.ToString());
			}

			Debug.Log("[SocketIO] - Encoded: " + builder.ToString());
			return builder.ToString();
		}
	}
}
