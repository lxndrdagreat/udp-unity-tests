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
    [MessagePackMember(0, Name = "t")]   
    public int t; // type
    [MessagePackMember(2, Name = "a")]
    public int a; // do we need to ACKnowledge?
    [MessagePackMember(1, Name = "s")]
    public int s; // sequence number
    [MessagePackMember(3, Name = "p")]
    public byte[] p; // payload
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

	public byte[] CreateMessage(PacketId eventType, byte[] payload)
    {
        var message = new Message();
        message.t = (int)eventType;
		message.p = payload;
        SerializationContext.Default.SerializationMethod = SerializationMethod.Array;
        var serializer = SerializationContext.Default.GetSerializer<Message>();        
        var stream = new MemoryStream ();
        serializer.Pack(stream, message);
		return stream.ToArray();
    }

    public Message ParseMessage(byte[] data)
    {
        SerializationContext.Default.SerializationMethod = SerializationMethod.Array;
        var serializer = SerializationContext.Default.GetSerializer<Message> ();
		var stream = new MemoryStream (data);
		var message = serializer.Unpack (stream);
		return message;
    }

    public static byte[] PackData(string data)
    {
        var serializer = SerializationContext.Default.GetSerializer<string>();
        var stream = new MemoryStream();
        serializer.Pack(stream, data);
        return stream.ToArray();
    }

    public static byte[] PackData(int data)
    {
        var serializer = SerializationContext.Default.GetSerializer<int>();
        var stream = new MemoryStream();
        serializer.Pack(stream, data);
        return stream.ToArray();
    }

    public static byte[] PackData(int[] data)
    {
        var serializer = SerializationContext.Default.GetSerializer<int[]>();
        var stream = new MemoryStream();
        serializer.Pack(stream, data);
        return stream.ToArray();
    }

    public static int ParseInt(byte[] data)
    {
        SerializationContext.Default.SerializationMethod = SerializationMethod.Map;
        var serializer = SerializationContext.Default.GetSerializer<int>();
        var stream = new MemoryStream(data);
        var result = serializer.Unpack(stream);
        return result;
    }
}

[System.Serializable]
public class PlayerData
{
    [MessagePackMember(0, Name = "uuid")]
    public int uuid;
    [MessagePackMember(1, Name = "position")]
    public int[] position;
    [MessagePackMember(2, Name = "coloRed")]
    public int colorRed;
    [MessagePackMember(3, Name = "colorBlue")]
    public int colorBlue;
    [MessagePackMember(4, Name = "colorGreen")]
    public int colorGreen;

    public static PlayerData FromBytes(byte[] data)
    {
        SerializationContext.Default.SerializationMethod = SerializationMethod.Map;
        var serializer = SerializationContext.Default.GetSerializer<PlayerData>();        
        var stream = new MemoryStream(data);
        var playerData = serializer.Unpack(stream);
        return playerData;
    }

    public static List<PlayerData> ListFromBytes(byte[] data)
    {
        SerializationContext.Default.SerializationMethod = SerializationMethod.Map;
        var serializer = SerializationContext.Default.GetSerializer<List<PlayerData>>();
        var stream = new MemoryStream(data);
        var playerData = serializer.Unpack(stream);
        return playerData;
    }

    public byte[] ToBytes()
    {
        SerializationContext.Default.SerializationMethod = SerializationMethod.Map;
        var serializer = SerializationContext.Default.GetSerializer<PlayerData>();
        var stream = new MemoryStream();
        serializer.Pack(stream, this);
        return stream.ToArray();
    }
}

[System.Serializable]
public class SizeDetail
{
    [MessagePackMember(0, Name = "width")]
    public int width;
    [MessagePackMember(1, Name = "height")]
    public int height;

    public static SizeDetail FromBytes(byte[] data)
    {
        SerializationContext.Default.SerializationMethod = SerializationMethod.Map;
        var serializer = SerializationContext.Default.GetSerializer<SizeDetail>();
        var stream = new MemoryStream(data);
        var playerData = serializer.Unpack(stream);
        return playerData;
    }

    public byte[] ToBytes()
    {
        SerializationContext.Default.SerializationMethod = SerializationMethod.Map;
        var serializer = SerializationContext.Default.GetSerializer<SizeDetail>();
        var stream = new MemoryStream();
        serializer.Pack(stream, this);
        return stream.ToArray();
    }
}