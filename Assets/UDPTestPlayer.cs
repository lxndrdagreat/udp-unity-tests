﻿using System.Collections;
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

    [Header("Player Handling")]
    [Tooltip("One of these will be spawned for each connected player.")]
    public GameObject playerPrefab;

    

    private UdpClient m_Socket;

    private MessageProtocol m_Protocol;
    private IPEndPoint m_ServerEndpoint;

    private string m_Test = "nope";

    void Awake()
    {
        m_Test = "yep";
        m_Protocol = new MessageProtocol();
        m_Socket = new UdpClient();
        m_ServerEndpoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);

        // schedule the first receive operation:
        m_Socket.BeginReceive(new AsyncCallback(OnUdpData), null);

        // Send a hello message
        Send("message", "Hello there!");
    }

	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Send(string eventName, object data)
    {
        var message = m_Protocol.CreateMessage(eventName, data);

        m_Socket.Send(message, message.Length, m_ServerEndpoint);
    }

    void OnUdpData(IAsyncResult result)
    {
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(IPAddress.Any, 0);
        // get the actual message and fill out the source:
        byte[] data = m_Socket.EndReceive(result, ref source);
        // do what you'd like with `message` here:
        var message = m_Protocol.ParseMessage(data);
        Debug.Log(message.p);
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