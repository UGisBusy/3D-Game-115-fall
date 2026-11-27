using UnityEditor;
using UnityEngine;

/// <summary>
/// When Play starts, removes leftover Inspector panels (Editor objects) whose objects were deleted.
/// Unity keeps such panels across the Play-mode script reload; they then rebuild themselves for nothing and throw
/// "MissingReferenceException: The variable m_Targets of GameObjectInspector doesn't exist anymore" and
/// "SerializedObjectNotCreatableException: Object at index 0 is null". Panels of live objects are never touched.
/// </summary>
[InitializeOnLoad]
public static class OrphanedEditorCleanup
{
    static OrphanedEditorCleanup()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                RemoveOrphans();
        };
    }

    private static void RemoveOrphans()
    {
        foreach (Editor editor in Resources.FindObjectsOfTypeAll<Editor>())
        {
            if (editor == null)
                continue;

            Object[] targets = editor.targets;
            bool orphaned = targets.Length > 0;
            foreach (Object t in targets)
                if (t != null)
                    orphaned = false;

            if (orphaned)
                Object.DestroyImmediate(editor);
        }
    }
}
