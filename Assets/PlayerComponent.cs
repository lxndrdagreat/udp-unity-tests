﻿using System.Collections;
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

	private int m_UUID = -1;
	public int uuid() {
		return m_UUID;
	}

	private SpriteRenderer m_Renderer;

	void Awake(){
		m_Renderer = GetComponent<SpriteRenderer> ();
	}

	public void Init(UDPTestPlayer manager, bool local, int uuid){
		m_SocketManager = manager;
		m_IsSocketInitialized = true;
		m_IsLocal = local;
		m_UUID = uuid;

		if (m_IsLocal) {
			Camera.main.GetComponent<FollowTarget> ().target = transform;
		}
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
		if (Input.GetKeyDown (KeyCode.UpArrow)) {
			y = 1;
		} else if (Input.GetKeyUp (KeyCode.UpArrow) && y == 1) {
			y = 0;
		}
		if (Input.GetKeyDown (KeyCode.DownArrow)) {
			y = -1;
		} else if (Input.GetKeyUp (KeyCode.DownArrow) && y == -1) {
			y = 0;
		}
        if (x != deltaX || y != deltaY)
        {
            deltaX = x;
            deltaY = y;
            int[] d = new int[2];
            d[0] = deltaX;
            d[1] = deltaY;
            var as_data = MessageProtocol.PackData(d);
            m_SocketManager.QueueMessage(PacketId.PLAYER_INPUT, as_data);
        }
	}

	public void SetPlayerColor(Color c){
		m_Color = c;
		m_Renderer.color = m_Color;
	}

	public void UpdateData(PlayerData data){
		var color = new Color ((float)data.colorRed / 255.0f, (float)data.colorGreen / 255.0f, (float)data.colorBlue / 255.0f);
		if (m_Color != color) {
			SetPlayerColor (color);
		}
		transform.position = new Vector3 ((float)data.position [0] / 1000.0f, (float)data.position [1] / 1000.0f);
	}
}
