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
	private Dictionary<int, PlayerComponent> m_ConnectedPlayers;

	// Local player
	private PlayerComponent m_LocalPlayer = null;
    private bool m_WasWelcomed = false;

	// This Client's UUID, as received from the server
	private int m_UUID = -1;

    private UdpClient m_Socket;

    private MessageProtocol m_Protocol;
    private IPEndPoint m_ServerEndpoint;

	private List<Message> m_MessageQueue;
    private List<AckInfo> m_AcksToSend;
    private List<QueuedMessage> m_OutboundQueue;

    private struct QueuedMessage
    {
        public PacketId id;
        public string data;
        public bool needsAck;
    }

    private struct AckInfo
    {
        public int sequenceNumber;
        public int attempts;
    }

    void Awake()
    {
        m_OutboundQueue = new List<QueuedMessage>();
        m_AcksToSend = new List<AckInfo>();
		m_MessageQueue = new List<Message> ();
		m_ConnectedPlayers = new Dictionary<int, PlayerComponent> ();
        m_Protocol = new MessageProtocol();
        m_Socket = new UdpClient();        
        m_ServerEndpoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);

        // schedule the first receive operation:
        m_Socket.BeginReceive(new AsyncCallback(OnUdpData), null);

        // Send a hello message
        Send(PacketId.JOIN, "Hello there!");
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
            // Send("heartbeat", "heartbeat");
        }

        lock (m_MessageQueue)
        {
            StartCoroutine(HandleMessageQueue());
        }

        lock (m_AcksToSend)
        {
            HandleAckQueue();
        }

        HandleOutboundQueue();
	}

    public void QueueMessage(PacketId eventName, string data, bool needsAck= false)
    {
        var qm = new QueuedMessage
        {
            id = eventName,
            data = data,
            needsAck = needsAck
        };
        lock (m_OutboundQueue)
        {
            m_OutboundQueue.Add(qm);
        }
    }

    private void Send(PacketId eventName, string data, bool needAck=false)
    {
        var message = m_Protocol.CreateMessage(eventName, data);        
        try
        {
            m_Socket.Send(message, message.Length, m_ServerEndpoint);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }        
    }

	void QueueIncomingMessage(Message m){
        lock (m_MessageQueue)
        {
            m_MessageQueue.Add(m);
        }		
	}

    void HandleOutboundQueue()
    {
        lock (m_OutboundQueue)
        {
            while (m_OutboundQueue.Count > 0)
            {
                var message = m_OutboundQueue[0];
                m_OutboundQueue.RemoveAt(0);
                Send(message.id, message.data, message.needsAck);
            }
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
    }

    void HandleAckQueue()
    {
        List<AckInfo> acksDone = new List<AckInfo>();
        List<int> finalAckList = new List<int>();
        for (var i = 0; i < m_AcksToSend.Count; ++i)        
        {
            var ack = m_AcksToSend[i];
            ack.attempts += 1;
            if (ack.attempts > 10)
            {
                acksDone.Add(ack);
                continue;
            }
            finalAckList.Add(ack.sequenceNumber);
        }

        foreach (var ack in acksDone)
        {
            m_AcksToSend.Remove(ack);
        }
        if (finalAckList.Count > 0)
        {
            Send(PacketId.ACK, JsonConvert.SerializeObject(finalAckList));
        }
    }

    void QueueAck(int sequenceNumber)
    {
        var info = new AckInfo
        {
            sequenceNumber = sequenceNumber,
            attempts = 0
        };
        m_AcksToSend.Add(info);
    }

    void HandleMessage(Message message){
        //Debug.Log ("Message: " + message.t);
        //Debug.Log (message.p);

        if (message.a == 1)
        {
            // send ACK back to server
            QueueAck(message.s);
        }

		if ((PacketId)message.t == PacketId.WELCOME && !m_WasWelcomed) {
            // received welcome message from the server
            m_WasWelcomed = true;           
			var playerData = JsonConvert.DeserializeObject<PlayerData> (message.p);
			m_UUID = playerData.uuid;
            Debug.Log("I have been welcomed! My Player ID is " + m_UUID);
            var playerObject = (GameObject)Instantiate (playerPrefab);
			var playerComponent = playerObject.GetComponent<PlayerComponent> ();
			playerComponent.Init (this, true, playerData.uuid);
			playerComponent.UpdateData (playerData);
			m_ConnectedPlayers.Add (playerData.uuid, playerComponent);
			m_LocalPlayer = playerComponent;

		} else if ((PacketId)message.t == PacketId.WORLD_INFO) {

		}
		else if ((PacketId)message.t == PacketId.PLAYER_UPDATES) {
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
		} else if ((PacketId)message.t == PacketId.PLAYER_LEFT) {
			var uuid = JsonConvert.DeserializeObject<int> (message.p);
			Debug.Log ("Player left: " + message.p);
			if (m_ConnectedPlayers.ContainsKey (uuid)) {
				Debug.Log ("Removing player: " + message.p);
				var deadPlayer = m_ConnectedPlayers [uuid];
				Destroy (deadPlayer.gameObject);
				m_ConnectedPlayers.Remove (uuid);
			}
		} else {
			Debug.Log ("Unknown message");
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
		QueueIncomingMessage (message);
		// HandleMessage (message);
        //Debug.Log(message.p);
        // schedule the next receive operation once reading is done:
        m_Socket.BeginReceive(new AsyncCallback(OnUdpData), null);
    }

	private IPAddress FirstDnsEntry(string hostName)
	{
		IPHostEntry IPHost = Dns.Resolve(hostName);
		IPAddress[] addr = IPHost.AddressList;
		if (addr.Length == 0) throw new Exception("No IP addresses");
		return addr[0];
	}

	public void TestDNS(){
		Debug.Log (FirstDnsEntry (serverAddress));
	}
}

[System.Serializable]
public class PlayerData {
	public int uuid;
	public int[] position;
	public int colorRed;
	public int colorBlue;
	public int colorGreen;
}