using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UDPTestPlayer))]
public class ServerManagerEditor : Editor
{

    public override void OnInspectorGUI()
    {
        // base.OnInspectorGUI();
        DrawDefaultInspector();

        if (GUILayout.Button("Test Connection"))
        {
            var manager = (UDPTestPlayer)target;
            manager.Test();
        }

		if (GUILayout.Button ("Test DNS")) {

			var manager = (UDPTestPlayer)target;
			manager.TestDNS ();
		}
    }
}