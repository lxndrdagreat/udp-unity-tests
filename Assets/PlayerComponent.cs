using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComponent : MonoBehaviour {

	private Color m_Color;

	private UDPTestPlayer m_SocketManager;
	private bool m_IsSocketInitialized = false;
	private bool m_IsLocal = false;

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
		transform.position = data.position;
	}
}
