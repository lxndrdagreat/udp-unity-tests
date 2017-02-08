using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Text;

[System.Serializable]
public struct Message
{
    public string t; // type
	public string p;
}

class MessageProtocol
{

    public byte[] CreateMessage(string eventType, string payload)
    {
        var message = new Message();
        message.t = eventType;
		message.p = payload;
        var jsonstring = JsonConvert.SerializeObject(message);
        jsonstring = jsonstring + "\n";
        return Encoding.UTF8.GetBytes(jsonstring);
    }

    public Message ParseMessage(byte[] data)
    {
        var stringData = Encoding.UTF8.GetString(data);
        var message = JsonConvert.DeserializeObject<Message>(stringData);
        return message;
    }
}
