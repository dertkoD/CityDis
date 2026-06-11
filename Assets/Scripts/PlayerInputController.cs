using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
     [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CurrentTileController currentTileController;
    [SerializeField] private TilePlacementController tilePlacementController;
    [SerializeField] private HoverTilePreviewController hoverTilePreviewController;
    [Tooltip("Optional. Highlights matching tile sides under the hovered cell.")]
    [SerializeField] private EdgeHighlightVisualizer edgeHighlightVisualizer;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private InputActionReference pointerPositionAction;
    [SerializeField] private InputActionReference rotateLeftAction;
    [SerializeField] private InputActionReference rotateRightAction;

    [Header("Raycast")]
    [SerializeField] private LayerMask availableCellLayerMask = ~0;
    [SerializeField] private float rayDistance = 100f;

    private AvailableCellMarker hoveredMarker;

    private void OnEnable()
    {
        clickAction.action.performed += OnClickPerformed;
        rotateLeftAction.action.performed += OnRotateLeftPerformed;
        rotateRightAction.action.performed += OnRotateRightPerformed;

        clickAction.action.Enable();
        pointerPositionAction.action.Enable();
        rotateLeftAction.action.Enable();
        rotateRightAction.action.Enable();
    }

    private void OnDisable()
    {
        clickAction.action.performed -= OnClickPerformed;
        rotateLeftAction.action.performed -= OnRotateLeftPerformed;
        rotateRightAction.action.performed -= OnRotateRightPerformed;

        clickAction.action.Disable();
        pointerPositionAction.action.Disable();
        rotateLeftAction.action.Disable();
        rotateRightAction.action.Disable();
    }

    private void Update()
    {
        UpdateHover();
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        if (hoveredMarker == null)
        {
            return;
        }

        hoveredMarker.PlaceTileHere();

        hoveredMarker = null;
        hoverTilePreviewController.HidePreview();

        if (edgeHighlightVisualizer != null)
        {
            edgeHighlightVisualizer.Hide();
        }
    }

    private void OnRotateLeftPerformed(InputAction.CallbackContext context)
    {
        currentTileController.RotateLeft();

        tilePlacementController.RefreshAvailableMarkers();

        hoverTilePreviewController.RefreshRotation();

        RefreshEdgeHighlight();
    }

    private void OnRotateRightPerformed(InputAction.CallbackContext context)
    {
        currentTileController.RotateRight();

        tilePlacementController.RefreshAvailableMarkers();

        hoverTilePreviewController.RefreshRotation();

        RefreshEdgeHighlight();
    }

    private void RefreshEdgeHighlight()
    {
        if (edgeHighlightVisualizer == null)
        {
            return;
        }

        if (hoveredMarker != null)
        {
            edgeHighlightVisualizer.ShowForCoord(hoveredMarker.Coord);
        }
        else
        {
            edgeHighlightVisualizer.Hide();
        }
    }

    private void UpdateHover()
    {
        AvailableCellMarker marker = RaycastAvailableMarker();

        hoveredMarker = marker;

        if (hoveredMarker == null)
        {
            hoverTilePreviewController.HidePreview();

            if (edgeHighlightVisualizer != null)
            {
                edgeHighlightVisualizer.Hide();
            }

            return;
        }

        hoverTilePreviewController.ShowPreviewAt(hoveredMarker.Coord);

        if (edgeHighlightVisualizer != null)
        {
            edgeHighlightVisualizer.ShowForCoord(hoveredMarker.Coord);
        }
    }

    private AvailableCellMarker RaycastAvailableMarker()
    {
        Vector2 pointerPosition = pointerPositionAction.action.ReadValue<Vector2>();

        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        bool hitSomething = Physics.Raycast(
            ray,
            out RaycastHit hit,
            rayDistance,
            availableCellLayerMask,
            QueryTriggerInteraction.Collide
        );

        if (!hitSomething)
        {
            return null;
        }

        return hit.collider.GetComponentInParent<AvailableCellMarker>();
    }
}
