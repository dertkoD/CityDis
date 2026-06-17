using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
    [SerializeField] private float mapPlaneY = 0f;

    [Header("Scene")]
    [SerializeField] private string towerBloxxSceneName = "TowerBloxxScene";
    [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Additive;
    [SerializeField] private GameObject[] objectsToDisableWhileBuilding;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

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
        if (clickAction == null)
        {
            Log("Click Action is not assigned. Mouse fallback can still handle clicks.");
            return;
        }

        clickAction.action.performed += OnClickPerformed;
        clickAction.action.Enable();

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
        TryOpenTowerBloxxScene("InputAction");
    }

    private void Update()
    {
        if (!useMouseButtonFallback || Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryOpenTowerBloxxScene("Mouse.current.leftButton");
        }
    }

    private void TryOpenTowerBloxxScene(string source)
    {
        Log($"Click received from {source}.");

        if (CityBuildSession.HasPendingTile)
        {
            Log("Ignored click because a city build session is already pending.");
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera is not assigned on EmptyTerrainSceneLoader.");
            return;
        }

        PlacedTile placedTile = GetClickedEmptyTile(out Vector3 hitPoint);

        if (placedTile == null)
        {
            Log("No Empty center click area was hit.");
            return;
        }

        TerrainType centerTerrain = placedTile.GetCenterTerrain();

        if (centerTerrain != TerrainType.Empty)
        {
            Log($"Hit tile {placedTile.name}, but center terrain is {centerTerrain}, not Empty.");
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

    private PlacedTile GetClickedEmptyTile(out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        Vector2 pointerPosition = ReadPointerPosition();
        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        EmptyTerrainClickArea.RefreshAllTerrainStates();

        if (EmptyTerrainClickArea.TryRaycastEmptyArea(
                ray,
                rayDistance,
                out EmptyTerrainClickArea raycastArea,
                out hitPoint))
        {
            if (!raycastArea.TryGetPlacedTile(out PlacedTile raycastTile))
            {
                Log("Direct Empty-area raycast hit an area, but it has no PlacedTile.");
                return null;
            }

            Log($"Direct Empty-area raycast hit {raycastTile.name} at {hitPoint}.");
            return raycastTile;
        }

        if (TryGetEmptyTileFromMapPlane(ray, out PlacedTile planeTile, out hitPoint))
        {
            return planeTile;
        }

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            rayDistance,
            ~0,
            QueryTriggerInteraction.Collide
        );

        if (hits.Length == 0)
        {
            Log("Physics raycast returned 0 hits after map-plane check.");
            return null;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        Log($"Physics raycast returned {hits.Length} hit(s). First hit: {hits[0].collider.name}");

        foreach (RaycastHit hit in hits)
        {
            EmptyTerrainClickArea emptyTerrainClickArea =
                hit.collider.GetComponentInParent<EmptyTerrainClickArea>();

            if (emptyTerrainClickArea != null &&
                emptyTerrainClickArea.TryGetPlacedTile(out PlacedTile placedTile))
            {
                hitPoint = hit.point;
                return placedTile;
            }

            if (emptyTerrainClickArea != null)
            {
                Log($"Hit EmptyTerrainClickArea on {hit.collider.name}, but no PlacedTile was found in parents.");
            }
        }

        return null;
    }

    private bool TryGetEmptyTileFromMapPlane(Ray ray, out PlacedTile placedTile, out Vector3 hitPoint)
    {
        placedTile = null;
        hitPoint = Vector3.zero;

        Plane mapPlane = new Plane(Vector3.up, new Vector3(0f, mapPlaneY, 0f));

        if (!mapPlane.Raycast(ray, out float enter))
        {
            Log("Mouse ray did not intersect map plane.");
            return false;
        }

        hitPoint = ray.GetPoint(enter);

        if (!EmptyTerrainClickArea.TryGetEmptyAreaAtWorldPoint(
                hitPoint,
                out EmptyTerrainClickArea emptyTerrainClickArea))
        {
            Log($"Map-plane point {hitPoint} is outside all active Empty click areas.");
            return false;
        }

        if (!emptyTerrainClickArea.TryGetPlacedTile(out placedTile))
        {
            Log("Map-plane hit EmptyTerrainClickArea, but it has no PlacedTile.");
            return false;
        }

        Log($"Map-plane hit Empty click area on {placedTile.name} at {hitPoint}.");
        return true;
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

        if (pointerPositionAction == null)
        {
            return false;
        }

        InputAction action = pointerPositionAction.action;

        if (action == null)
        {
            return false;
        }

        foreach (InputControl control in action.controls)
        {
            if (control.valueType == typeof(Vector2))
            {
                pointerPosition = action.ReadValue<Vector2>();
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
