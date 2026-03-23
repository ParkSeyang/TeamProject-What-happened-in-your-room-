using UnityEngine;

public class ARSpaceManagerView : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private ARSpaceManager manager;

    private void Awake()
    {
        if (manager == null) manager = GetComponent<ARSpaceManager>();
    }

    private void OnGUI()
    {
        if (manager == null) return;

        GUILayout.BeginArea(new Rect(20, 20, 250, 800));
        GUILayout.BeginVertical();

        GUI.skin.button.fontSize = 14;

        GUILayout.Label("<b>[ Plane Detection ]</b>", new GUIStyle { richText = true, fontSize = 15 });
        
        string horizontalText = manager.IsHorizontalDetectionEnabled ? "■ Stop Horizontal" : "▶ Start Horizontal";
        if (GUILayout.Button(horizontalText, GUILayout.Height(45)))
        {
            manager.ToggleHorizontal();
        }

        string verticalText = manager.IsVerticalDetectionEnabled ? "■ Stop Vertical" : "▶ Start Vertical";
        if (GUILayout.Button(verticalText, GUILayout.Height(45)))
        {
            manager.ToggleVertical();
        }

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Delete All Trackables", GUILayout.Height(40)))
        {
            manager.ResetARSession();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Clear All Boxes", GUILayout.Height(40)))
        {
            manager.ClearAllBoxes();
        }

        GUILayout.Space(20);

        GUILayout.Label("<b>[ Box Generator ]</b>", new GUIStyle { richText = true, fontSize = 15 });
        string generateBtnText = manager.IsScanning ? "Scanning & Grouping..." : "Generate Integrated Box";
        
        bool canGenerate = !manager.IsScanning && (manager.IsHorizontalDetectionEnabled || manager.IsVerticalDetectionEnabled);
        GUI.enabled = canGenerate;
        if (GUILayout.Button(generateBtnText, GUILayout.Height(50)))
        {
            manager.StartBoxGeneration();
        }
        GUI.enabled = true;

        GUILayout.Space(30);

        GUILayout.Label("<b>[ Util Controls ]</b>", new GUIStyle { richText = true, fontSize = 15 });

        string cubeVisText = manager.AreCubesVisible ? "Hide Boxes" : "Show Boxes";
        if (GUILayout.Button(cubeVisText, GUILayout.Height(40)))
        {
            manager.ToggleCubeVisibility();
        }

        string planeVisText = manager.ArePlanesVisible ? "Hide Planes" : "Show Planes";
        if (GUILayout.Button(planeVisText, GUILayout.Height(40)))
        {
            manager.TogglePlaneVisibility();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
