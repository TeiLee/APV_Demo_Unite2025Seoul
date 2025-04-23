using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkyManager))]
public class SkyManagerEditor : Editor
{
    private Texture2D[] gradientTextures = new Texture2D[3];
    private const int BAR_WIDTH = 20;
    private const int BAR_HEIGHT = 150;
    private int selectedGradient = -1;
    private Texture2D rotatedDebugTexture = null;
    
    // updateInterval
    private double lastUpdateTime = 0;
    private const double updateInterval = 0.1; // 1초

    // Purge Cache~
    void InvalidateGradientTextures()
    {
        for (int i = 0; i < gradientTextures.Length; i++)
        {
            if (gradientTextures[i] != null)
            {
                DestroyImmediate(gradientTextures[i]);
                gradientTextures[i] = null;
            }
        }
    }

    void CreateGradientTexture(Gradient gradient, int index)
    {
        if (gradientTextures[index] == null)
        {
            gradientTextures[index] = new Texture2D(BAR_WIDTH, BAR_HEIGHT, TextureFormat.RGBA32, false);
        }

        Color[] pixels = new Color[BAR_WIDTH * BAR_HEIGHT];
        for (int y = 0; y < BAR_HEIGHT; y++)
        {
            Color color = gradient.Evaluate((float)y / BAR_HEIGHT);
            for (int x = 0; x < BAR_WIDTH; x++)
            {
                pixels[y * BAR_WIDTH + x] = color;
            }
        }
        gradientTextures[index].SetPixels(pixels);
        gradientTextures[index].Apply();
    }

    // Tex rot CCW
    private Texture2D RotateTexture90CounterClockwise(Texture2D originalTexture)
    {
        int width = originalTexture.width;
        int height = originalTexture.height;
        Texture2D rotatedTexture = new Texture2D(height, width, originalTexture.format, false);
        Color[] originalPixels = originalTexture.GetPixels();
        Color[] rotatedPixels = new Color[originalPixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int newX = height - 1 - y;
                int newY = x;
                rotatedPixels[newY * height + newX] = originalPixels[y * width + x];
            }
        }
        rotatedTexture.SetPixels(rotatedPixels);
        rotatedTexture.Apply();
        return rotatedTexture;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SkyManager skyManager = (SkyManager)target;
        bool hasChanged = false;

        // ChangeCheck
        EditorGUI.BeginChangeCheck();

        // Lights
        EditorGUILayout.LabelField("Dominant Lights", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_Sun"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_Moon"));

        // Gradient
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Sky Color Gradient", EditorStyles.boldLabel);

        // Gradient Editor
        if (selectedGradient == 0)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_NightSkyGradient"), new GUIContent("Night Gradient"));
        else if (selectedGradient == 1)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_SunupSkyGradient"), new GUIContent("Sunup Gradient"));
        else if (selectedGradient == 2)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_DaySkyGradient"), new GUIContent("Day Gradient"));

        // Debug
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugTexture"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugTexture"));

        // EndChangeCheck
        if (EditorGUI.EndChangeCheck())
        {
            hasChanged = true;
            InvalidateGradientTextures();
            if (rotatedDebugTexture != null)
            {
                DestroyImmediate(rotatedDebugTexture);
                rotatedDebugTexture = null;
            }
        }

        // updateInterval
        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastUpdateTime >= updateInterval)
        {
            // Gradients Tex Update
            CreateGradientTexture(skyManager._NightSkyGradient, 0);
            CreateGradientTexture(skyManager._SunupSkyGradient, 1);
            CreateGradientTexture(skyManager._DaySkyGradient, 2);

            // Debug Tex Rotation 90 (When it needs!)
            if (skyManager.showDebugTexture && skyManager.debugTexture != null &&
                (rotatedDebugTexture == null || rotatedDebugTexture.width != skyManager.debugTexture.height))
            {
                if (rotatedDebugTexture != null)
                    DestroyImmediate(rotatedDebugTexture);
                rotatedDebugTexture = RotateTexture90CounterClockwise(skyManager.debugTexture);
            }
            lastUpdateTime = currentTime;
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Choose&Click Gradient", EditorStyles.boldLabel);
        
        // Vertical Bar
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace(); // 중앙 정렬

        // Night
        EditorGUILayout.BeginVertical(GUILayout.Width(BAR_WIDTH + 20));
        EditorGUILayout.LabelField("Night", EditorStyles.boldLabel, GUILayout.Width(BAR_WIDTH + 20));
        Rect nightRect = GUILayoutUtility.GetRect(BAR_WIDTH, BAR_HEIGHT);
        GUI.DrawTexture(nightRect, gradientTextures[0]);
        if (Event.current.type == EventType.MouseDown && nightRect.Contains(Event.current.mousePosition))
        {
            selectedGradient = 0;
            Event.current.Use();
            hasChanged = true;
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(20);

        // Sunset
        EditorGUILayout.BeginVertical(GUILayout.Width(BAR_WIDTH + 20));
        EditorGUILayout.LabelField("Sunup", EditorStyles.boldLabel, GUILayout.Width(BAR_WIDTH + 20));
        Rect sunsetRect = GUILayoutUtility.GetRect(BAR_WIDTH, BAR_HEIGHT);
        GUI.DrawTexture(sunsetRect, gradientTextures[1]);
        if (Event.current.type == EventType.MouseDown && sunsetRect.Contains(Event.current.mousePosition))
        {
            selectedGradient = 1;
            Event.current.Use();
            hasChanged = true;
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(20);

        // Day
        EditorGUILayout.BeginVertical(GUILayout.Width(BAR_WIDTH + 20));
        EditorGUILayout.LabelField("Day", EditorStyles.boldLabel, GUILayout.Width(BAR_WIDTH + 20));
        Rect dayRect = GUILayoutUtility.GetRect(BAR_WIDTH, BAR_HEIGHT);
        GUI.DrawTexture(dayRect, gradientTextures[2]);
        if (Event.current.type == EventType.MouseDown && dayRect.Contains(Event.current.mousePosition))
        {
            selectedGradient = 2;
            Event.current.Use();
            hasChanged = true;
        }
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace(); // Center align

        // Debug Texture - Keep Square & Rotation
        if (skyManager.showDebugTexture && rotatedDebugTexture != null)
        {
            GUILayout.Space(20);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("", EditorStyles.boldLabel);
            int debugSize = 150; // Square size fix!
            Rect debugRect = GUILayoutUtility.GetRect(debugSize, debugSize);
            GUI.DrawTexture(debugRect, rotatedDebugTexture, ScaleMode.ScaleToFit);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();

        if (serializedObject.ApplyModifiedProperties() || hasChanged)
        {
            skyManager.Validate();
            EditorUtility.SetDirty(target);
            Repaint();
        }
    }

    private void OnDisable()
    {
        if (rotatedDebugTexture != null)
        {
            DestroyImmediate(rotatedDebugTexture);
            rotatedDebugTexture = null;
        }
        InvalidateGradientTextures();
    }
}
