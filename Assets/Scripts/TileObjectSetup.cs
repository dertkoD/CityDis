using UnityEngine;

public static class TileObjectSetup
{
    public static void ApplyData(GameObject tileObject, TileData tileData)
    {
        if (tileObject == null)
        {
            Debug.LogError("Tile object is null.");
            return;
        }

        if (tileData == null)
        {
            Debug.LogError("TileData is null.");
            return;
        }

        TileDefinition definition = tileObject.GetComponent<TileDefinition>();

        if (definition == null)
        {
            Debug.LogError($"TileDefinition is missing on {tileObject.name}");
            return;
        }

        definition.SetData(tileData);

        TileVisualApplier visualApplier = tileObject.GetComponent<TileVisualApplier>();

        if (visualApplier == null)
        {
            Debug.LogError($"TileVisualApplier is missing on {tileObject.name}");
            return;
        }

        visualApplier.Apply(tileData);
    }
}