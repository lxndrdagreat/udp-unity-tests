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

	private List<GameObject> m_Walls;
	[SerializeField]
	private GameObject m_WallPrefab;

    private struct QueuedMessage
    {
        public PacketId id;
        public byte[] data;
        public bool needsAck;
    }

	public class AckInfo
    {
        public int sequenceNumber;
        public int attempts;
    }

    void Awake()
    {
		m_Walls = new List<GameObject> ();
        m_OutboundQueue = new List<QueuedMessage>();
        m_AcksToSend = new List<AckInfo>();
		m_MessageQueue = new List<Message> ();
		m_ConnectedPlayers = new Dictionary<int, PlayerComponent> ();
        m_Protocol = new MessageProtocol();
        m_Socket = new UdpClient();

        var serverIP = FirstDnsEntry (serverAddress);
		m_ServerEndpoint = new IPEndPoint(serverIP, serverPort);

        // schedule the first receive operation:
        m_Socket.BeginReceive(new AsyncCallback(OnUdpData), null);

        // Send a hello message
        Send(PacketId.JOIN, MessageProtocol.PackData("Hello, world!"));
        m_HeartbeatTimer = m_HeartbeatRate;
    }

	// Use this for initialization
	void Start () {
        
	}    
	
	// Update is called once per frame
	void Update () {

        // The job of the heartbeat is to keep our connection to the server alive,
        // even if we haven't had player input in a while.
        m_HeartbeatTimer -= Time.deltaTime;
        if (m_HeartbeatTimer <= 0.0f)
        {
            m_HeartbeatTimer = m_HeartbeatRate;
            QueueMessage(PacketId.HEARTBEAT, MessageProtocol.PackData(1));
        }

        lock (m_MessageQueue)
        {
            HandleMessageQueue();
        }

        lock (m_AcksToSend)
        {
            HandleAckQueue();
        }

        HandleOutboundQueue();
	}

    public void QueueMessage(PacketId eventName, byte[] data, bool needsAck= false)
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

    private void Send(PacketId eventName, byte[] data, bool needAck=false)
    {
        var message = m_Protocol.CreateMessage(eventName, data);        
        try
        {
//			 Debug.Log("sending " + eventName);
            m_Socket.Send(message, message.Length, m_ServerEndpoint);
//			 Debug.Log("sent " + eventName);
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
			foreach (var message in m_OutboundQueue){				
                Send(message.id, message.data, message.needsAck);
            }
			m_OutboundQueue.Clear ();
        }
    }

    void HandleMessageQueue()
    {
        while (m_MessageQueue.Count > 0)
        {
            var message = m_MessageQueue[0];
            m_MessageQueue.RemoveAt(0);
            HandleMessage(message);
        }
    }

    void HandleAckQueue()
    {
        List<AckInfo> acksDone = new List<AckInfo>();
        List<int> finalAckList = new List<int>();
        for (var i = 0; i < m_AcksToSend.Count; ++i)        
        {
			var ack = m_AcksToSend[i];
			ack.attempts = ack.attempts + 1;
			// Debug.Log ("ACK " + ack.sequenceNumber + " attempts: " + ack.attempts);
            if (ack.attempts > 10)
            {
                acksDone.Add(ack);
                continue;
            }
            finalAckList.Add(ack.sequenceNumber);
        }
		// Debug.Log ("dead acks: " + acksDone.Count);
        foreach (var ack in acksDone)
        {
            m_AcksToSend.Remove(ack);
        }
        if (finalAckList.Count > 0)
        {
            Send(PacketId.ACK, MessageProtocol.PackData(finalAckList.ToArray()));
        }
    }

    void QueueAck(int sequenceNumber)
    {
        if (m_AcksToSend.Find(a => a.sequenceNumber == sequenceNumber) != null)
        {
            // only queue an ack for a particular sequenceNumber once.
            return;
        }
        var info = new AckInfo
        {
            sequenceNumber = sequenceNumber,
            attempts = 0
        };
        m_AcksToSend.Add(info);
    }

    void HandleMessage(Message message){
//        Debug.Log ("Message: " + message.t);
        //Debug.Log (message.p);

        if (message.a == 1)
        {
            // send ACK back to server
            QueueAck(message.s);
        }

		if ((PacketId)message.t == PacketId.WELCOME && !m_WasWelcomed) {
            // received welcome message from the server
            m_WasWelcomed = true;
            var playerData = PlayerData.FromBytes(message.p);
			m_UUID = playerData.uuid;
            Debug.Log("I have been welcomed! My Player ID is " + m_UUID);
            var playerObject = (GameObject)Instantiate (playerPrefab);
			var playerComponent = playerObject.GetComponent<PlayerComponent> ();
			playerComponent.Init (this, true, playerData.uuid);
			playerComponent.UpdateData (playerData);
			m_ConnectedPlayers.Add (playerData.uuid, playerComponent);
			m_LocalPlayer = playerComponent;

		} else if ((PacketId)message.t == PacketId.WORLD_INFO) {
			foreach (var go in m_Walls) {
				Destroy (go);
			}
			m_Walls.Clear ();

            var worldSize = SizeDetail.FromBytes(message.p);
			for (var x = -worldSize.width; x <= worldSize.width; ++x) {
				var wall = (GameObject)Instantiate (m_WallPrefab);
				m_WallPrefab.transform.position = new Vector2 (x, -worldSize.height);
				m_Walls.Add (m_WallPrefab);

				wall = (GameObject)Instantiate (m_WallPrefab);
				m_WallPrefab.transform.position = new Vector2 (x, worldSize.height);
				m_Walls.Add (m_WallPrefab);
			}

			for (var y = -worldSize.height; y <= worldSize.height; ++y) {
				var wall = (GameObject)Instantiate (m_WallPrefab);
				m_WallPrefab.transform.position = new Vector2 (-worldSize.width, y);
				m_Walls.Add (m_WallPrefab);

				wall = (GameObject)Instantiate (m_WallPrefab);
				m_WallPrefab.transform.position = new Vector2 (worldSize.width, y);
				m_Walls.Add (m_WallPrefab);
			}
		}
		else if ((PacketId)message.t == PacketId.PLAYER_UPDATES) {
			//Debug.Log ("player updates!");
            // received updates to players
            var list = PlayerData.ListFromBytes(message.p);
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
            var uuid = MessageProtocol.ParseInt(message.p);
			Debug.Log ("Player left: " + uuid);
			if (m_ConnectedPlayers.ContainsKey (uuid)) {
				Debug.Log ("Removing player: " + uuid);
				var deadPlayer = m_ConnectedPlayers [uuid];
				Destroy (deadPlayer.gameObject);
				m_ConnectedPlayers.Remove (uuid);
			}
		} else {
			Debug.Log ("Unknown message: " + message.t);
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
		try {            
        	var message = m_Protocol.ParseMessage(data);
			QueueIncomingMessage (message);
		}
		catch (Exception e) {
			Debug.Log ("something went wrong with parsing the message: " + e);
		}
		// HandleMessage (message);
        //Debug.Log(message.p);
        // schedule the next receive operation once reading is done:
        m_Socket.BeginReceive(new AsyncCallback(OnUdpData), null);
    }

	private IPAddress FirstDnsEntry(string hostName)
	{
        IPHostEntry IPHost = Dns.GetHostEntry(serverAddress);
		IPAddress[] addr = IPHost.AddressList;
		if (addr.Length == 0) throw new Exception("No IP addresses");
		return addr[0];
	}

	public void TestDNS(){
		Debug.Log (FirstDnsEntry (serverAddress));
	}
}
