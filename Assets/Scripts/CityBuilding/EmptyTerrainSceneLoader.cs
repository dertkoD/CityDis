using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Lives in the 3D map scene and owns the whole transition to/from the 2D Tower
// Bloxx mini game.
//
// The 3D scene is kept loaded (additively) the entire time so the map state is
// preserved, but while the player is in the 2D game it is fully hidden:
//   - the 2D scene becomes the ACTIVE scene (so things the 2D game spawns, like
//     crane blocks, live in the 2D scene and are destroyed when it unloads, and
//     Camera.main resolves to the 2D camera),
//   - the 3D camera / canvas / input objects are disabled (no bleed-through),
//   - the change is wrapped in a fade so it looks like a real scene transition.
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

    [Tooltip("3D objects to hide while the 2D game is open. Assign the 3D Main " +
             "Camera, the 3D Canvas and the 3D EventSystem here (and anything " +
             "else that should not show through). Do NOT add this object itself.")]
    [SerializeField] private GameObject[] objectsToDisableWhileBuilding;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private Scene mapScene;
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

        SetObjectsEnabled(false);

        AsyncOperation load = SceneManager.LoadSceneAsync(towerBloxxSceneName, LoadSceneMode.Additive);

        while (load != null && !load.isDone)
        {
            yield return null;
        }

        Scene towerScene = SceneManager.GetSceneByName(towerBloxxSceneName);

        if (towerScene.IsValid())
        {
            SceneManager.SetActiveScene(towerScene);
        }

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeIn();
        }

        isBusy = false;
    }

    // The 2D game raises CityBuildSession.HouseCompleted when it is done. The
    // 3D House3DBuilder (still alive in this scene) builds the house parented to
    // the tile, so it ends up in the map scene regardless of the active scene.
    private void OnHouseCompleted(HexCoord tileCoord, HouseBuildData houseBuildData)
    {
        if (!ownsSession)
        {
            // Not a session this loader started, nothing to unload here.
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

        SetObjectsEnabled(true);

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeIn();
        }

        isBusy = false;
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

    private void SetObjectsEnabled(bool isEnabled)
    {
        if (objectsToDisableWhileBuilding == null)
        {
            return;
        }

        foreach (GameObject sceneObject in objectsToDisableWhileBuilding)
        {
            if (sceneObject != null)
            {
                sceneObject.SetActive(isEnabled);
            }
        }
    }

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log($"[EmptyTerrainSceneLoader] {message}", this);
        }
    }
}
