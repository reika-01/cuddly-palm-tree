using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// A data structure to hold a pair of materials for replacement.
/// Making it serializable allows Unity to display and save it in the editor window.
/// </summary>
[System.Serializable]
public class MaterialReplacementPair
{
    public Material oldMaterial;
    public Material newMaterial;
}

/// <summary>
/// An editor window to replace multiple materials at once throughout the active scene.
/// </summary>
public class AdvancedMaterialReplacer : EditorWindow
{
    // A list to hold all the material replacement pairs.
    public List<MaterialReplacementPair> replacementPairs = new List<MaterialReplacementPair>();

    // We use SerializedObject and SerializedProperty to get a better editor UI for lists,
    // including add/remove buttons and proper undo/redo support.
    private SerializedObject serializedObject;
    private SerializedProperty serializedPairsProperty;
    private Vector2 scrollPosition;

    /// <summary>
    /// Creates a menu item to open the Advanced Material Replacer window.
    /// The window will be accessible from "Tools/Advanced Material Replacer".
    /// </summary>
    [MenuItem("Tools/Advanced Material Replacer")]
    public static void ShowWindow()
    {
        GetWindow<AdvancedMaterialReplacer>("Advanced Replacer");
    }

    /// <summary>
    /// Called when the window is enabled. Sets up the SerializedObject and Property.
    /// </summary>
    private void OnEnable()
    {
        // 'this' refers to the EditorWindow instance.
        serializedObject = new SerializedObject(this);
        serializedPairsProperty = serializedObject.FindProperty("replacementPairs");
    }

    /// <summary>
    /// Renders the UI for the editor window.
    /// </summary>
    void OnGUI()
    {
        // It's important to update the serializedObject at the beginning of OnGUI.
        serializedObject.Update();

        GUILayout.Label("Advanced Material Replacer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Add pairs of materials to replace. The tool will find all instances of an 'Old Material' and replace it with the corresponding 'New Material'.", MessageType.Info);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // This single line will draw the entire list with controls.
        EditorGUILayout.PropertyField(serializedPairsProperty, true);

        EditorGUILayout.EndScrollView();

        // We must apply changes back to the object.
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        // Disable the button if the list is empty.
        GUI.enabled = replacementPairs.Count > 0;

        if (GUILayout.Button("Replace All in Scene", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Replace Materials?",
                $"Are you sure you want to perform {replacementPairs.Count} replacement operations in the current scene? This action cannot be undone.",
                "Yes, replace them", "Cancel"))
            {
                ReplaceMaterialsInScene();
            }
        }
        
        GUI.enabled = true;
    }

    /// <summary>
    /// The core logic for finding and replacing all specified materials.
    /// </summary>
    private void ReplaceMaterialsInScene()
    {
        int totalReplacements = 0;
        if (replacementPairs == null || replacementPairs.Count == 0)
        {
            Debug.LogWarning("Material Replacer: No replacement pairs have been specified.");
            return;
        }

        // Create a dictionary for faster lookups.
        var replacementMap = new Dictionary<Material, Material>();
        foreach (var pair in replacementPairs)
        {
            if (pair.oldMaterial != null && pair.newMaterial != null)
            {
                if (!replacementMap.ContainsKey(pair.oldMaterial))
                {
                    replacementMap.Add(pair.oldMaterial, pair.newMaterial);
                }
            }
        }

        if(replacementMap.Count == 0)
        {
            Debug.LogError("Material Replacer: No valid material pairs were provided (check for empty slots).");
            return;
        }

        // Find all renderers in the scene, including inactive ones.
        Renderer[] allRenderers = FindObjectsOfType<Renderer>(true);

        foreach (Renderer renderer in allRenderers)
        {
            var sharedMaterials = renderer.sharedMaterials;
            bool materialsChanged = false;

            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                Material currentMat = sharedMaterials[i];
                if (currentMat != null && replacementMap.ContainsKey(currentMat))
                {
                    sharedMaterials[i] = replacementMap[currentMat];
                    totalReplacements++;
                    materialsChanged = true;
                }
            }

            if (materialsChanged)
            {
                renderer.sharedMaterials = sharedMaterials;
                EditorUtility.SetDirty(renderer);
            }
        }

        Debug.Log($"Material Replacer: Successfully performed {totalReplacements} material replacements in the scene '{SceneManager.GetActiveScene().name}'.");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
