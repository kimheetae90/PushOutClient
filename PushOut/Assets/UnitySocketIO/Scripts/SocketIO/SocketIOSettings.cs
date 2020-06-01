using UnityEngine;
using System.Collections;

namespace UnitySocketIO.SocketIO {
	[System.Serializable]
	public class SocketIOSettings {

		public string url;
		public int port;

		public bool sslEnabled;

		public int reconnectTime;

		public int timeToDropAck;
		
		public int pingTimeout;
		public int pingInterval;
        
        public SocketIOSettings Copy()
        {
            SocketIOSettings ioSetting = new SocketIOSettings();
            ioSetting.url = url;
            ioSetting.port = port;

            ioSetting.sslEnabled = sslEnabled;

            ioSetting.reconnectTime = reconnectTime;

            ioSetting.timeToDropAck = timeToDropAck;

            ioSetting.pingTimeout = pingTimeout;
            ioSetting.pingInterval = pingInterval;

            return ioSetting;
        }
	}
}