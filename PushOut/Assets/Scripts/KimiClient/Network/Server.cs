using UnityEngine;
using UnitySocketIO;
using UnitySocketIO.SocketIO;
using UnitySocketIO.Events;
using System;


[Serializable]
public class ServerErrorS2CPacket
{
    public string message;
}

public class Server : MonoBehaviour
{
    public static Server Instance;
    
    public SocketIOSettings setting;
    
    private SocketIOController socketIOComponet;

    private bool isPaused = false;

    private void Awake()
    {
        Instance = this;
    }
    
    public void Initiallize()
    {
        if (socketIOComponet == null)
        {
            socketIOComponet = this.gameObject.AddComponent<SocketIOController>();
            socketIOComponet.settings = setting.Copy();
            socketIOComponet.Initiallize();
        }

        On("disconnect", OnServerDisconnectProc);
        On("DisconnectS2C", OnServerDisconnectProc);
        On("ErrorS2C", OnError);
    }

    private void OnDestroy()
    {
        if(socketIOComponet != null)
        {
            Off("disconnect", OnServerDisconnectProc);
            Off("DisconnectS2C", OnServerDisconnectProc);
            Off("ErrorS2C", OnError);
        }
    }

    public void Connect()
    {
        if (socketIOComponet != null)
            socketIOComponet.Connect();
    }

    public void Disconnect()
    {
        if (socketIOComponet != null)
            socketIOComponet.Close();

#if ServerMonitor
        if (GameClient.Instance.Game != null && GameClient.Instance.Game.ModeName != "ServerMonitorMode")
        {
            GameClient.Instance.StartGame(new ServerMonitorMode());
        }
#else
        if (GameClient.Instance.Game != null && GameClient.Instance.Game.ModeName != "LobbyMode")
        {
            GameClient.Instance.StartGame(new LobbyMode());
        }
#endif
    }

    public void Emit(string packetName, string packet = "")
    {
        if (packetName.Equals(string.Empty))
        {
            Debug.LogError("[Server]Packet Name is Empty!");
            return;
        }

        if(packet.Equals(string.Empty))
        {
            socketIOComponet.Emit(packetName);
        }
        else
        {
            socketIOComponet.Emit(packetName, packet);
        }
    }

    public void On(string packetName, System.Action<SocketIOEvent> packetProc)
    {
        if(IsValid(packetName, packetProc))
        {
            socketIOComponet.On(packetName, packetProc);
        }
    }
    
    public void Off(string packetName, System.Action<SocketIOEvent> packetProc)
    {
        if (IsValid(packetName, packetProc))
        {
            socketIOComponet.Off(packetName, packetProc);
        }
    }

    public void OnApplicationPause(bool pause)
    {
        if(pause)
        {
            if(isPaused)
                return;

            if(!ADManager.Instance.IsRunningRewardAD)
            {
                Disconnect();
            }
            isPaused = true;
        }
        else
        {
            if (!isPaused)
                return;

            isPaused = false;
        }
    }

    private bool IsValid(string packetName, System.Action<SocketIOEvent> packetProc)
    {
        if (packetName.Equals(string.Empty))
        {
            Debug.LogError("[Server]Packet Name is Empty!");
            return false;
        }

        if (packetProc == null)
        {
            Debug.LogError("[Server]Packet Reciever is Null!");
            return false;
        }

        return true;
    }
    
    private void OnServerDisconnectProc(SocketIOEvent packetProc)
    {
        Disconnect();
    }

    private void OnError(SocketIOEvent e)
    {
        Debug.Log("[PacketReceive]Server Error : " + e.name + " " + e.data);
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]Server Error Data is Null!");
            return;
        }

        ServerErrorS2CPacket packet = JsonUtility.FromJson<ServerErrorS2CPacket>(e.data);

        UIMessageBox.Instance.Show(packet.message);
        Disconnect();
    }
}
