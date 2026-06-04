using UnityEngine;

public class HexSizeDebugger : MonoBehaviour
{
    [ContextMenu("Log Suggested Hex Size")]
    private void LogSuggestedHexSize()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found.");
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float widthX = bounds.size.x;
        float depthZ = bounds.size.z;

        float pointyFromDepth = depthZ / 2f;
        float pointyFromWidth = widthX / Mathf.Sqrt(3f);

        float flatFromWidth = widthX / 2f;
        float flatFromDepth = depthZ / Mathf.Sqrt(3f);

        Debug.Log(
            $"Bounds size: X={widthX}, Z={depthZ}\n" +
            $"Suggested PointyTop hexSize from Z: {pointyFromDepth}\n" +
            $"Suggested PointyTop hexSize from X: {pointyFromWidth}\n" +
            $"Suggested FlatTop hexSize from X: {flatFromWidth}\n" +
            $"Suggested FlatTop hexSize from Z: {flatFromDepth}"
        );
    }
}
