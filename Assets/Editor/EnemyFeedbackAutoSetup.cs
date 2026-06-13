using UnityEditor;
using UnityEngine;

public static class EnemyFeedbackAutoSetup
{
    [MenuItem("Tools/Enemies/Setup Selected Enemy Feedbacks")]
    private static void SetupSelectedEnemyFeedbacks()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("Aucun ennemi sélectionné.");
            return;
        }

        int configuredCount = 0;

        foreach (GameObject selectedObject in selectedObjects)
        {
            EnemyFeedback feedback =
                selectedObject.GetComponent<EnemyFeedback>();

            if (feedback == null)
            {
                Debug.LogWarning(
                    selectedObject.name +
                    " ignoré : aucun EnemyFeedback trouvé.");

                continue;
            }

            SpriteRenderer enemyRenderer =
                FindEnemyRenderer(selectedObject);

            GameObject alertIcon =
                FindChildByName(selectedObject.transform, "AlertIcon");

            GameObject searchIcon =
                FindChildByName(selectedObject.transform, "SearchIcon");

            if (enemyRenderer == null)
            {
                Debug.LogWarning(
                    selectedObject.name +
                    " : aucun SpriteRenderer valide trouvé.");
            }

            if (alertIcon == null)
            {
                Debug.LogWarning(
                    selectedObject.name +
                    " : AlertIcon introuvable.");
            }

            if (searchIcon == null)
            {
                Debug.LogWarning(
                    selectedObject.name +
                    " : SearchIcon introuvable.");
            }

            Undo.RecordObject(
                feedback,
                "Setup Enemy Feedback References");

            feedback.SetupReferences(
                enemyRenderer,
                alertIcon,
                searchIcon);

            EditorUtility.SetDirty(feedback);

            if (alertIcon != null)
            {
                Undo.RecordObject(
                    alertIcon,
                    "Disable Alert Icon");

                alertIcon.SetActive(false);
                EditorUtility.SetDirty(alertIcon);
            }

            if (searchIcon != null)
            {
                Undo.RecordObject(
                    searchIcon,
                    "Disable Search Icon");

                searchIcon.SetActive(false);
                EditorUtility.SetDirty(searchIcon);
            }

            configuredCount++;

            Debug.Log(
                selectedObject.name +
                " configuré avec ses propres références.");
        }

        Debug.Log(
            "Configuration terminée. Ennemis configurés : " +
            configuredCount);
    }

    private static SpriteRenderer FindEnemyRenderer(GameObject enemy)
    {
        Transform visual =
            enemy.transform.Find("Visual");

        if (visual != null)
        {
            SpriteRenderer visualRenderer =
                visual.GetComponent<SpriteRenderer>();

            if (visualRenderer != null)
                return visualRenderer;
        }

        SpriteRenderer rootRenderer =
            enemy.GetComponent<SpriteRenderer>();

        if (rootRenderer != null)
            return rootRenderer;

        SpriteRenderer[] renderers =
            enemy.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.gameObject.name == "AlertIcon")
                continue;

            if (renderer.gameObject.name == "SearchIcon")
                continue;

            return renderer;
        }

        return null;
    }

    private static GameObject FindChildByName(
        Transform parent,
        string childName)
    {
        Transform[] children =
            parent.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }
}