using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class PlayerComponent : MonoBehaviour {

	private Color m_Color;

	private UDPTestPlayer m_SocketManager;
	private bool m_IsSocketInitialized = false;
	private bool m_IsLocal = false;

    private int deltaX = 0;
    private int deltaY = 0;

	private string m_UUID = null;
	public string uuid() {
		return m_UUID;
	}

	private SpriteRenderer m_Renderer;

	void Awake(){
		m_Renderer = GetComponent<SpriteRenderer> ();
	}

	public void Init(UDPTestPlayer manager, bool local, string uuid){
		m_SocketManager = manager;
		m_IsSocketInitialized = true;
		m_IsLocal = local;
		m_UUID = uuid;
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (!m_IsSocketInitialized) {
			return;
		}

        int x = deltaX;
        int y = deltaY;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            x = -1;
        }
        else if (Input.GetKeyUp(KeyCode.LeftArrow) && x == -1)
        {
            x = 0;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            x = 1;
        }
        else if (Input.GetKeyUp(KeyCode.RightArrow) && x == 1)
        {
            x = 0;
        }
        if (x != deltaX || y != deltaY)
        {
            deltaX = x;
            deltaY = y;
            int[] d = new int[2];
            d[0] = deltaX;
            d[1] = deltaY;
            var as_json = JsonConvert.SerializeObject(d);
            m_SocketManager.Send("player_move", as_json);
        }
	}

	public void SetPlayerColor(Color c){
		m_Color = c;
		m_Renderer.color = m_Color;
	}

	public void UpdateData(PlayerData data){
		var color = new Color (data.colorRed, data.colorGreen, data.colorBlue);
		if (m_Color != color) {
			SetPlayerColor (color);
		}
		transform.position = new Vector3 (data.position [0], data.position [1]);
	}
}
