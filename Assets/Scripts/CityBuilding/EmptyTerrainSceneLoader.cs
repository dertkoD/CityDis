using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Lives in the 3D map scene and owns the whole transition to/from the 2D Tower
// Bloxx mini game.
//
// The 3D scene is kept loaded (additively) so the map state is preserved, but
// while the player is in the 2D game it is FULLY hidden:
//   - every root object of the map scene is deactivated (except this controller),
//     so the 2D camera cannot see any 3D geometry,
//   - the 2D scene is made the ACTIVE scene as soon as it loads (before its
//     scripts' Start runs), so everything the 2D game spawns (e.g. crane blocks)
//     lives in the 2D scene and is destroyed when it unloads,
//   - the whole thing is wrapped in a fade so it looks like a real transition.
public class EmptyTerrainSceneLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Input")]
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private InputActionReference pointerPositionAction;
    [SerializeField] private bool useMouseButtonFallback = true;

    [Header("Raycast")]
    [SerializeField] private float rayDistance = 100f;

    [Header("Scene")]
    [SerializeField] private string towerBloxxSceneName = "TowerBloxxScene";

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private Scene mapScene;
    private readonly List<GameObject> deactivatedRoots = new();

    private bool clickRequested;
    private bool isBusy;
    private bool ownsSession;

    private void Awake()
    {
        mapScene = gameObject.scene;
        CityBuildSession.HouseCompleted += OnHouseCompleted;
    }

    private void OnDestroy()
    {
        CityBuildSession.HouseCompleted -= OnHouseCompleted;
    }

    private void OnEnable()
    {
        if (clickAction != null)
        {
            clickAction.action.performed += OnClickPerformed;
            clickAction.action.Enable();
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (clickAction != null)
        {
            clickAction.action.performed -= OnClickPerformed;
            clickAction.action.Disable();
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.action.Disable();
        }
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        clickRequested = true;
    }

    private void Update()
    {
        if (useMouseButtonFallback &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            clickRequested = true;
        }
    }

    // Processed in LateUpdate so it runs AFTER tile placement has been handled
    // this frame; that lets us reliably ignore the click that placed a tile.
    private void LateUpdate()
    {
        if (!clickRequested)
        {
            return;
        }

        clickRequested = false;
        TryOpenTowerBloxxScene();
    }

    private void TryOpenTowerBloxxScene()
    {
        if (isBusy || CityBuildSession.HasPendingTile)
        {
            return;
        }

        if (TilePlacementController.LastPlacementFrame == Time.frameCount)
        {
            Log("Ignored click because a tile was placed this frame.");
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError("EmptyTerrainSceneLoader: Main Camera is not assigned.", this);
            return;
        }

        if (!TryGetClickedEmptyTile(out PlacedTile placedTile))
        {
            return;
        }

        if (placedTile.GetCenterTerrain() != TerrainType.Empty)
        {
            Log($"Hit tile {placedTile.name}, but its center terrain is not Empty.");
            return;
        }

        Log($"Opening {towerBloxxSceneName} for tile {placedTile.Coord}.");
        StartCoroutine(OpenRoutine(placedTile.Coord));
    }

    private IEnumerator OpenRoutine(HexCoord coord)
    {
        isBusy = true;
        ownsSession = true;
        CityBuildSession.StartBuilding(coord);

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeOut();
        }

        HideMapScene();

        // Make the 2D scene active the moment it is loaded (before its Start
        // runs), so crane blocks spawn there and not in the hidden map scene.
        SceneManager.sceneLoaded += OnTowerSceneLoaded;

        AsyncOperation load = SceneManager.LoadSceneAsync(towerBloxxSceneName, LoadSceneMode.Additive);

        while (load != null && !load.isDone)
        {
            yield return null;
        }

        SceneManager.sceneLoaded -= OnTowerSceneLoaded;

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeIn();
        }

        isBusy = false;
    }

    private void OnTowerSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == towerBloxxSceneName)
        {
            SceneManager.SetActiveScene(scene);
        }
    }

    // The 2D game raises CityBuildSession.HouseCompleted when it is done. The
    // 3D House3DBuilder (still alive on this object) builds the house parented to
    // the tile, so it ends up in the map scene (inactive for now) and becomes
    // visible again when the map scene is shown.
    private void OnHouseCompleted(HexCoord tileCoord, HouseBuildData houseBuildData)
    {
        if (!ownsSession)
        {
            return;
        }

        ownsSession = false;
        StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        isBusy = true;

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeOut();
        }

        Scene towerScene = SceneManager.GetSceneByName(towerBloxxSceneName);

        if (towerScene.IsValid() && towerScene.isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(towerScene);

            while (unload != null && !unload.isDone)
            {
                yield return null;
            }
        }

        if (mapScene.IsValid())
        {
            SceneManager.SetActiveScene(mapScene);
        }

        ShowMapScene();

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeIn();
        }

        isBusy = false;
    }

    private void HideMapScene()
    {
        deactivatedRoots.Clear();

        GameObject self = transform.root.gameObject;

        foreach (GameObject root in mapScene.GetRootGameObjects())
        {
            if (root == self || !root.activeSelf)
            {
                continue;
            }

            root.SetActive(false);
            deactivatedRoots.Add(root);
        }
    }

    private void ShowMapScene()
    {
        foreach (GameObject root in deactivatedRoots)
        {
            if (root != null)
            {
                root.SetActive(true);
            }
        }

        deactivatedRoots.Clear();
    }

    // Strict: the click must land directly on the enabled Empty-center collider.
    private bool TryGetClickedEmptyTile(out PlacedTile placedTile)
    {
        placedTile = null;

        Vector2 pointerPosition = ReadPointerPosition();
        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        EmptyTerrainClickArea.RefreshAllTerrainStates();

        if (EmptyTerrainClickArea.TryRaycastEmptyArea(
                ray,
                rayDistance,
                out EmptyTerrainClickArea emptyTerrainClickArea,
                out _) &&
            emptyTerrainClickArea.TryGetPlacedTile(out placedTile))
        {
            return true;
        }

        return false;
    }

    private Vector2 ReadPointerPosition()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        if (Touchscreen.current != null)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (TryReadPointerPositionAction(out Vector2 pointerPosition))
        {
            return pointerPosition;
        }

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private bool TryReadPointerPositionAction(out Vector2 pointerPosition)
    {
        pointerPosition = Vector2.zero;

        if (pointerPositionAction == null || pointerPositionAction.action == null)
        {
            return false;
        }

        foreach (InputControl control in pointerPositionAction.action.controls)
        {
            if (control.valueType == typeof(Vector2))
            {
                pointerPosition = pointerPositionAction.action.ReadValue<Vector2>();
                return true;
            }
        }

        return false;
    }

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log($"[EmptyTerrainSceneLoader] {message}", this);
        }
    }
}
