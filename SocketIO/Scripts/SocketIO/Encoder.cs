using UnityEngine;
using System.Collections;

namespace SocketIO
{
	public class Encoder : MonoBehaviour
	{
		public string Encode(string data)
		{
			Debug.Log("[SocketIO] - Encoding: " + data);
			return data;
		}
	}
}
