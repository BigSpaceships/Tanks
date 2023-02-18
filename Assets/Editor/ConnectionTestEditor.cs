using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ConnectionTesting))]
public class ConnectionTestEditor : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        if (GUILayout.Button("Connect")) {
            ((ConnectionTesting)target).ConnectSignalingServer();
        }

        if (GUILayout.Button("Send Message")) {
            ((ConnectionTesting)target).SendMessage();
        }
    }
}
