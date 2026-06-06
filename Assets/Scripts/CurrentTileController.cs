using System;
using System.Collections.Generic;
using UnityEngine;

// Owns the player's tile "deck": the current tile to place, its rotation, and the
// queue of upcoming tiles.
//
// The deck can be FINITE (a fixed number of tiles set in the inspector, like
// Dorfromantik's stack) or INFINITE (endless tiles, for free play / testing).
//
// Rendering of the preview (the 3 upcoming tiles, the stack and the counter) is
// handled by TilePreviewPanel, which listens to DeckChanged.
public class CurrentTileController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject baseTilePrefab;
    [SerializeField] private TileDataGenerator tileDataGenerator;

    [Header("Deck")]
    [Tooltip("How many tiles the player gets to place (ignored when 'Infinite Deck' is on).")]
    [SerializeField] private int deckSize = 50;
    [Tooltip("Endless tiles. When on, 'Deck Size' is ignored and the counter shows infinity.")]
    [SerializeField] private bool infiniteDeck = false;
    [Tooltip("How many upcoming tiles are kept ready / shown in the preview panel (current + next).")]
    [SerializeField] private int previewCount = 3;

    // Materialized upcoming tiles. The front of the queue is the CURRENT tile.
    private readonly Queue<TileData> upcoming = new Queue<TileData>();

    // Tiles that still need to be generated (finite deck only).
    private int reserve;

    private int rotationSteps;

    public GameObject BaseTilePrefab => baseTilePrefab;
    public bool IsInfiniteDeck => infiniteDeck;
    public int PreviewCount => Mathf.Max(1, previewCount);

    public TileData CurrentTileData => upcoming.Count > 0 ? upcoming.Peek() : null;
    public bool HasCurrentTile => upcoming.Count > 0;

    public int RotationSteps => rotationSteps;
    public Quaternion CurrentRotation => Quaternion.Euler(0f, rotationSteps * 60f, 0f);

    // Tiles left to place, INCLUDING the current one. Returns -1 when infinite.
    public int RemainingCount => infiniteDeck ? -1 : upcoming.Count + reserve;

    // Raised whenever the deck content changes (reset / advanced), so previews refresh.
    public event Action DeckChanged;

    // A standalone tile used to seed the board (the auto-placed starting tile). It
    // does NOT consume the deck.
    public TileData GenerateRandomTileData()
    {
        return tileDataGenerator.GenerateTile();
    }

    // (Re)builds the deck from scratch. Call this when a new game starts.
    public void ResetDeck()
    {
        upcoming.Clear();
        reserve = infiniteDeck ? 0 : Mathf.Max(0, deckSize);
        rotationSteps = 0;

        FillUpcoming();

        DeckChanged?.Invoke();
    }

    // Consumes the current tile and moves to the next one in the deck.
    public void AdvanceToNextTile()
    {
        if (upcoming.Count > 0)
        {
            upcoming.Dequeue();
        }

        FillUpcoming();

        rotationSteps = 0;

        DeckChanged?.Invoke();
    }

    public void RotateRight()
    {
        rotationSteps = (rotationSteps + 1) % 6;
    }

    public void RotateLeft()
    {
        rotationSteps = (rotationSteps + 5) % 6;
    }

    // Returns up to `max` upcoming tiles (current first), for the preview panel.
    public List<TileData> GetUpcoming(int max)
    {
        List<TileData> result = new List<TileData>();

        foreach (TileData tile in upcoming)
        {
            if (result.Count >= max)
            {
                break;
            }

            result.Add(tile);
        }

        return result;
    }

    private void FillUpcoming()
    {
        // Keep at least PreviewCount tiles ready so the panel always has something
        // to show (until a finite deck runs dry).
        while (upcoming.Count < PreviewCount)
        {
            if (!infiniteDeck && reserve <= 0)
            {
                break;
            }

            upcoming.Enqueue(tileDataGenerator.GenerateTile());

            if (!infiniteDeck)
            {
                reserve--;
            }
        }
    }
}
