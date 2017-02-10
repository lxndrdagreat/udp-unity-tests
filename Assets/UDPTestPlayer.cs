using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using System.Text;

public class UDPTestPlayer : MonoBehaviour {

    [Header("Server Address")]
    [Tooltip("Must be a valid IP address.")]
    public string serverAddress = "127.0.0.1";
    public int serverPort = 9999;

    [Header("Heartbeat")]
    [SerializeField]
    [Tooltip("How often (in seconds) to send a heartbeat to make sure we don't get kicked from the server.")]
    private float m_HeartbeatRate = 30.0f;
    private float m_HeartbeatTimer = 0.0f;

    [Header("Player Handling")]
    [Tooltip("One of these will be spawned for each connected player.")]
    public GameObject playerPrefab;

	// All connected players (including local player)
	private Dictionary<string, PlayerComponent> m_ConnectedPlayers;

	// Local player
	private PlayerComponent m_LocalPlayer = null;

	// This Client's UUID, as received from the server
	private string m_UUID = null;

    private UdpClient m_Socket;

    private MessageProtocol m_Protocol;
    private IPEndPoint m_ServerEndpoint;

	private List<Message> m_MessageQueue;

    void Awake()
    {
		m_MessageQueue = new List<Message> ();
		m_ConnectedPlayers = new Dictionary<string, PlayerComponent> ();
        m_Protocol = new MessageProtocol();
        m_Socket = new UdpClient();        
        m_ServerEndpoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);

        // schedule the first receive operation:
        m_Socket.BeginReceive(new AsyncCallback(OnUdpData), null);

        // Send a hello message
        Send("message", "Hello there!");
        m_HeartbeatTimer = m_HeartbeatRate;
    }

	// Use this for initialization
	void Start () {
        
	}    
	
	// Update is called once per frame
	void Update () {
        m_HeartbeatTimer -= Time.deltaTime;
        if (m_HeartbeatTimer <= 0.0f)
        {
            m_HeartbeatTimer = m_HeartbeatRate;
            Send("heartbeat", "heartbeat");
        }

        lock (m_MessageQueue)
        {
            StartCoroutine(HandleMessageQueue());
        }

	}

    public void Send(string eventName, string data)
    {
        var message = m_Protocol.CreateMessage(eventName, data);
        // Debug.Log("sending: " + message.ToString());
        try
        {
            m_Socket.Send(message, message.Length, m_ServerEndpoint);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }        
    }

	void QueueMessage(Message m){
        lock (m_MessageQueue)
        {
            m_MessageQueue.Add(m);
        }		
	}

    IEnumerator HandleMessageQueue()
    {
        while (m_MessageQueue.Count > 0)
        {
            var message = m_MessageQueue[0];
            m_MessageQueue.RemoveAt(0);
            HandleMessage(message);
            yield return null;
        }
        yield return null;
    }

    void HandleMessage(Message message){
		//Debug.Log ("Message: " + message.t);
		//Debug.Log (message.p);

		if (message.t == "welcome") {
			// received welcome message from the server
			var playerData = JsonConvert.DeserializeObject<PlayerData> (message.p);
			m_UUID = playerData.uuid;
			var playerObject = (GameObject)Instantiate (playerPrefab);
			var playerComponent = playerObject.GetComponent<PlayerComponent> ();
			playerComponent.Init (this, true, playerData.uuid);
			playerComponent.UpdateData (playerData);
			m_ConnectedPlayers.Add (playerData.uuid, playerComponent);
			m_LocalPlayer = playerComponent;

		} else if (message.t == "players") {
			// received updates to players
			var list = JsonConvert.DeserializeObject<List<PlayerData>> (message.p);
			foreach (var p in list) {
				if (m_ConnectedPlayers.ContainsKey (p.uuid)) {
					var player = m_ConnectedPlayers [p.uuid];
					player.UpdateData (p);
				} else if (m_LocalPlayer != null && p.uuid != m_LocalPlayer.uuid ()) {
					// create new player
					var playerObject = (GameObject)Instantiate (playerPrefab);
					var playerComponent = playerObject.GetComponent<PlayerComponent> ();
					playerComponent.Init (this, false, p.uuid);
					playerComponent.UpdateData (p);
					m_ConnectedPlayers.Add (p.uuid, playerComponent);
				}
			}
		} else if (message.t == "player_left") {
            message.p = JsonConvert.DeserializeObject<string>(message.p);
            Debug.Log("Player left: " + message.p);
			if (m_ConnectedPlayers.ContainsKey (message.p)) {
                Debug.Log("Removing player: " + message.p);
				var deadPlayer = m_ConnectedPlayers [message.p];
				Destroy (deadPlayer.gameObject);
				m_ConnectedPlayers.Remove (message.p);
			}
		}
	}

    void OnUdpData(IAsyncResult result)
    {
        // Debug.Log("Got UDP Data");
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(IPAddress.Any, 0);
        // get the actual message and fill out the source:
        byte[] data = m_Socket.EndReceive(result, ref source);
        // do what you'd like with `message` here:
        var message = m_Protocol.ParseMessage(data);
		QueueMessage (message);
		// HandleMessage (message);
        //Debug.Log(message.p);
        // schedule the next receive operation once reading is done:
        m_Socket.BeginReceive(new AsyncCallback(OnUdpData), null);
    }

    void TestOnUdpData(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);
        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        // do what you'd like with `message` here:
        Debug.Log("Got " + message.Length + " bytes from " + source);

        socket.Close();
    }

    public void Test()
    {
        UdpClient socket = new UdpClient(); // `new UdpClient()` to auto-pick port

        // schedule the first receive operation:
        socket.BeginReceive(new AsyncCallback(TestOnUdpData), socket);

        // sending data (for the sake of simplicity, back to ourselves):
        IPEndPoint target = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);
        // send a couple of sample messages:
        var protocol = new MessageProtocol();

        var message = protocol.CreateMessage("message", "Hello, world!");

        socket.Send(message, message.Length, target);
    }
}

[System.Serializable]
public class PlayerData {
	public string uuid;
	public float[] position;
	public float rotation;
	public float colorRed;
	public float colorBlue;
	public float colorGreen;
}