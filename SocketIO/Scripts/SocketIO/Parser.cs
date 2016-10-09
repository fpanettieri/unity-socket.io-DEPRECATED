#region License
/*
 * Parser.cs
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
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SocketIO
{
	public class Parser
	{
		public SocketIOEvent Parse(JToken json)
		{
            var array = json.ToObject<JArray>();
			if (array.Count < 1 || array.Count > 2) {
				throw new SocketIOException("Invalid number of parameters received: " + array.Count);
			}
            
			if (json[0].Type != JTokenType.String) {
				throw new SocketIOException("Invalid parameter type. " + json[0].Type + " received while expecting " + JTokenType.String);
			}
            
			if (array.Count == 1) {
				return new SocketIOEvent(json[0].ToString());
			} 

			if (json[1].Type == JTokenType.Object || json[1].Type == JTokenType.String) {
				return new SocketIOEvent(json[0].ToString(), json[1]);
			} else {
				throw new SocketIOException("Invalid argument type. " + json[1].Type + " received");
			}
		}
	}
}
