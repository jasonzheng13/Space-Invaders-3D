using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor-only helper (never included in builds). Adds the menu item
/// Tools > Space Invaders > Set Up Scene, which creates the GameObjects and UI that the
/// GameManager and Starfield scripts expect, wires their Inspector slots, and saves the scene.
/// Safe to run more than once: anything that already exists is reused, not duplicated.
/// </summary>
public static class SceneSetup
{
    const string StarMaterialPath = "Assets/StarMaterial.mat";

    [MenuItem("Tools/Space Invaders/Set Up Scene")]
    static void SetUpScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Set Up Scene", "Stop Play mode first. Changes made during Play mode are discarded.", "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "SampleScene")
        {
            EditorUtility.DisplayDialog("Set Up Scene", "Open Assets/Scenes/SampleScene first (currently open: " + scene.name + ").", "OK");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Set Up Scene", "No Canvas found in the scene.", "OK");
            return;
        }

        // --- GameManager ---
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gameManager == null)
        {
            gameManager = CreateObject("GameManager").AddComponent<GameManager>();
        }

        // --- Starfield (+ a saved unlit material so stars still render in builds) ---
        Starfield starfield = Object.FindFirstObjectByType<Starfield>(FindObjectsInactive.Include);
        if (starfield == null)
        {
            starfield = CreateObject("Starfield").AddComponent<Starfield>();
        }
        Material starMaterial = GetOrCreateStarMaterial();
        SetIfEmpty(starfield, "starMaterial", starMaterial);

        // --- UI under the Canvas ---
        Transform canvasT = canvas.transform;

        GameObject startPanel = FindChild(canvasT, "StartPanel");
        if (startPanel == null)
        {
            startPanel = CreatePanel(canvasT, "StartPanel");
            CreateText(startPanel.transform, "StartText", "SPACE INVADERS 3D\nPress ENTER to start", 48, TextAlignmentOptions.Center, stretch: true);
        }

        GameObject endPanel = FindChild(canvasT, "EndPanel");
        if (endPanel == null)
        {
            endPanel = CreatePanel(canvasT, "EndPanel");
            CreateText(endPanel.transform, "ResultText", "", 48, TextAlignmentOptions.Center, stretch: true);
        }
        endPanel.SetActive(false);   // GameManager shows it when the round ends
        TextMeshProUGUI resultText = endPanel.GetComponentInChildren<TextMeshProUGUI>(true);

        GameObject livesGO = FindChild(canvasT, "LivesText");
        TextMeshProUGUI livesText = livesGO != null
            ? livesGO.GetComponent<TextMeshProUGUI>()
            : CreateLivesText(canvasT);

        // --- Wire GameManager's private [SerializeField] slots ---
        SetIfEmpty(gameManager, "startPanel", startPanel);
        SetIfEmpty(gameManager, "endPanel", endPanel);
        SetIfEmpty(gameManager, "resultText", resultText);
        SetIfEmpty(gameManager, "livesText", livesText);

        GameObject scoreGO = FindChild(canvasT, "ScoreText");
        TextMeshProUGUI scoreText = scoreGO != null
            ? scoreGO.GetComponent<TextMeshProUGUI>()
            : CreateScoreText(canvasT);
        SetIfEmpty(gameManager, "scoreText", scoreText);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = gameManager.gameObject;
        Debug.Log("Space Invaders scene set up and saved. Check the GameManager Inspector: all five UI slots should be filled.");
    }

    // ---------- helpers ----------

    static GameObject CreateObject(string name, params System.Type[] components)
    {
        GameObject go = new GameObject(name, components);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    static GameObject FindChild(Transform parent, string name)
    {
        Transform t = parent.Find(name);   // Transform.Find also finds inactive children
        return t != null ? t.gameObject : null;
    }

    /// <summary>Full-screen, semi-transparent black panel.</summary>
    static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = CreateObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = parent.gameObject.layer;
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
        return panel;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size,
                                      TextAlignmentOptions alignment, bool stretch)
    {
        GameObject go = CreateObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        if (stretch) Stretch(go.GetComponent<RectTransform>());

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    /// <summary>"Lives: 3" anchored to the top-right corner.</summary>
    static TextMeshProUGUI CreateLivesText(Transform canvas)
    {
        TextMeshProUGUI text = CreateText(canvas, "LivesText", "Lives: 3", 36, TextAlignmentOptions.TopRight, stretch: false);
        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(300f, 60f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        return text;
    }

    /// <summary>"SCORE 000000" anchored to the bottom-left corner.</summary>
    static TextMeshProUGUI CreateScoreText(Transform canvas)
    {
        TextMeshProUGUI text = CreateText(canvas, "ScoreText", "SCORE 000000", 36, TextAlignmentOptions.BottomLeft, stretch: false);
        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.sizeDelta = new Vector2(400f, 60f);
        rt.anchoredPosition = new Vector2(20f, 20f);
        return text;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Material GetOrCreateStarMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(StarMaterialPath);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return null;
        mat = new Material(shader) { color = Color.white };
        AssetDatabase.CreateAsset(mat, StarMaterialPath);
        return mat;
    }

    /// <summary>Assigns a serialized field (works for private [SerializeField] fields) unless it is already set.</summary>
    static void SetIfEmpty(Object target, string fieldName, Object value)
    {
        if (value == null) return;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null || prop.objectReferenceValue != null) return;
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
