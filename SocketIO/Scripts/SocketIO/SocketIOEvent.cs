using System;

namespace SocketIO
{
	public class SocketIOEvent
	{
		private string _name;
		private object _data;

		public string name { get { return _name; }  }
		public object data { get { return _data; } }

		public SocketIOEvent(string name)
		{
			_name = name;
			_data = new object();
		}

		public SocketIOEvent(string name, object data)
		{
			_name = name;
			_data = data;
		}
	}
}
