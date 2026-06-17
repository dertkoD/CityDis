using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Lives in the 3D map scene. Opens the 2D Tower Bloxx scene ONLY when the player
// clicks directly on the Empty center of an already placed tile.
//
// It deliberately does NOT react to:
//   - clicks that place a tile (placing an Empty tile must not open the builder),
//   - clicks on empty ground near (but not on) an Empty tile.
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
    [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Additive;
    [SerializeField] private GameObject[] objectsToDisableWhileBuilding;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private bool clickRequested;

    private void Awake()
    {
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
    // this frame (placement happens in input callbacks / Update). This is what
    // lets us reliably ignore the click that placed a tile.
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
        if (CityBuildSession.HasPendingTile)
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

        CityBuildSession.StartBuilding(placedTile.Coord);
        SetObjectsEnabled(false);

        try
        {
            SceneManager.LoadScene(towerBloxxSceneName, loadSceneMode);
        }
        catch (System.Exception exception)
        {
            CityBuildSession.CancelBuilding();
            SetObjectsEnabled(true);
            Debug.LogException(exception);
        }
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

    private void OnHouseCompleted(HexCoord tileCoord, HouseBuildData houseBuildData)
    {
        SetObjectsEnabled(true);
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
