using UnityEngine;

public class TileDeck : MonoBehaviour
{
    [SerializeField] private GameObject[] tilePrefabs;

    public GameObject GetRandomTilePrefab()
    {
        if (tilePrefabs == null || tilePrefabs.Length == 0)
        {
            Debug.LogError("TileDeck has no tile prefabs assigned.");
            return null;
        }

        int index = Random.Range(0, tilePrefabs.Length);
        return tilePrefabs[index];
    }
}
