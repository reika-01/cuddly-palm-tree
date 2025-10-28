using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// An editor window to replace all instances of a specified material with another in the active scene.
/// </summary>
public class MaterialReplacer : EditorWindow
{
    // The material to be replaced
    private Material oldMaterial;
    // The material to replace with
    private Material newMaterial;
    // Scroll position for the window
    private Vector2 scrollPosition;

    /// <summary>
    /// Creates a menu item to open the Material Replacer window.
    /// The window will be accessible from the top menu bar under "Tools/Material Replacer".
    /// </summary>
    [MenuItem("Tools/Material Replacer")]
    public static void ShowWindow()
    {
        // Get existing open window or if none, make a new one.
        GetWindow<MaterialReplacer>("Material Replacer");
    }

    /// <summary>
    /// Renders the UI for the editor window.
    /// </summary>
    void OnGUI()
    {
        GUILayout.Label("Material Replacer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assign the material you want to replace (Old Material) and the material you want to use instead (New Material). This tool will search all Mesh Renderers and Skinned Mesh Renderers in the active scene.", MessageType.Info);

        // Creates a scrollable view for the window content
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Object fields for the user to drag and drop the materials
        oldMaterial = (Material)EditorGUILayout.ObjectField("Old Material", oldMaterial, typeof(Material), false);
        newMaterial = (Material)EditorGUILayout.ObjectField("New Material", newMaterial, typeof(Material), false);

        // Add some space before the button
        EditorGUILayout.Space();

        // Disable the button if either material slot is empty
        GUI.enabled = oldMaterial != null && newMaterial != null;

        if (GUILayout.Button("Replace All in Scene", GUILayout.Height(30)))
        {
            // Confirmation dialog to prevent accidental changes
            if (EditorUtility.DisplayDialog("Replace Materials?",
                $"Are you sure you want to replace all instances of '{oldMaterial.name}' with '{newMaterial.name}' in the current scene? This action cannot be undone.",
                "Yes, replace them", "Cancel"))
            {
                ReplaceMaterialsInScene();
            }
        }

        // Re-enable the GUI for other elements
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// The core logic for finding and replacing the materials.
    /// </summary>
    private void ReplaceMaterialsInScene()
    {
        if (oldMaterial == null || newMaterial == null)
        {
            Debug.LogError("Material Replacer: Old Material or New Material is not assigned.");
            return;
        }

        int replacementCount = 0;

        // Get all renderers in the active scene, including inactive/disabled ones.
        Renderer[] allRenderers = FindObjectsOfType<Renderer>(true);

        foreach (Renderer renderer in allRenderers)
        {
            // Use sharedMaterials to modify the assets directly, not just instances.
            Material[] sharedMaterials = renderer.sharedMaterials;
            bool materialsChanged = false;

            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                // Check if the current material is the one we want to replace
                if (sharedMaterials[i] == oldMaterial)
                {
                    sharedMaterials[i] = newMaterial;
                    replacementCount++;
                    materialsChanged = true;
                }
            }

            // If any materials were changed on this renderer, update its sharedMaterials array.
            if (materialsChanged)
            {
                renderer.sharedMaterials = sharedMaterials;
                // Mark the object as dirty to ensure the change is saved.
                EditorUtility.SetDirty(renderer);
            }
        }

        // Log the results to the console.
        Debug.Log($"Material Replacer: Successfully replaced {replacementCount} material(s) in the scene '{SceneManager.GetActiveScene().name}'.");

        // Mark the scene as dirty so the user is prompted to save the changes.
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}

