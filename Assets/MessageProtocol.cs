using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Text;
using UnityEngine;
using MsgPack.Serialization;
using System.IO;

[System.Serializable]
public struct Message
{
	public PacketId t; // type
	public byte a; // do we need to ACKnowledge?
	public int s; // sequence number
	public string p; // payload
}

public enum PacketId {
	JOIN = 0,
	WELCOME = 1,
	ACK = 2,
	PLAYER_INFO = 10,
	PLAYER_UPDATES = 11,
	PLAYER_LEFT = 12,
	PLAYER_INPUT = 20,
	PLAYER_FIRE = 21,
	WORLD_INFO = 30,
	BULLETS = 35
}

class MessageProtocol
{

	public byte[] CreateMessage(PacketId eventType, string payload)
    {
        var message = new Message();
        message.t = eventType;
		message.p = payload;
		var serializer = SerializationContext.Default.GetSerializer<Message> ();
		var stream = new MemoryStream ();
		serializer.Pack (stream, message);
		return stream.ToArray();
    }

    public Message ParseMessage(byte[] data)
    {
		var serializer = SerializationContext.Default.GetSerializer<Message> ();
		var stream = new MemoryStream (data);
		var message = serializer.Unpack (stream);
		return message;
    }
}
