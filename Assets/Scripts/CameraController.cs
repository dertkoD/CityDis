using UnityEngine;
using UnityEngine.InputSystem;

// Dorfromantik-style camera rig. Attach this to the Main Camera.
//
// Controls:
//   - Pan:   WASD / arrow keys, or hold RIGHT mouse button and drag.
//   - Zoom:  mouse scroll wheel.
//   - Orbit: hold MIDDLE mouse button and drag (horizontal = turn, vertical = tilt).
//
// Uses the new Input System's device polling, so no Input Action assets need to
// be wired up. (Q / E are intentionally left free for rotating the tile.)
public class CameraController : MonoBehaviour
{
    [Header("Focus")]
    [Tooltip("Height of the ground plane the camera orbits around.")]
    [SerializeField] private float groundHeight = 0f;
    [SerializeField] private Vector3 fallbackFocusPoint = Vector3.zero;

    [Header("Pan")]
    [SerializeField] private float keyboardPanSpeed = 1.5f;
    [SerializeField] private float dragPanSpeed = 1.5f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float minDistance = 0.4f;
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private float defaultDistance = 1.2f;

    [Header("Orbit")]
    [SerializeField] private float orbitSpeed = 0.2f;
    [SerializeField] private float minPitch = 25f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Smoothing")]
    [Tooltip("0 = instant. Higher = smoother / laggier.")]
    [SerializeField] private float smoothing = 10f;

    private Vector3 focusPoint;
    private float distance;
    private float yaw;
    private float pitch;

    private Vector3 targetFocus;
    private float targetDistance;
    private float targetYaw;
    private float targetPitch;

    private void Start()
    {
        InitializeFromCurrentTransform();
    }

    private void InitializeFromCurrentTransform()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        if (forward.y < -0.001f)
        {
            float travel = (origin.y - groundHeight) / -forward.y;
            focusPoint = origin + forward * travel;
            distance = Mathf.Clamp(travel, minDistance, maxDistance);
        }
        else
        {
            focusPoint = fallbackFocusPoint;
            distance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
        }

        focusPoint.y = groundHeight;

        targetFocus = focusPoint;
        targetDistance = distance;
        targetYaw = yaw;
        targetPitch = pitch;
    }

    private void Update()
    {
        HandleZoom();
        HandleOrbit();
        HandlePan();
    }

    private void LateUpdate()
    {
        ApplySmoothing();
        ApplyTransform();
    }

    private void HandleZoom()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        float scroll = mouse.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) < 0.01f)
        {
            return;
        }

        targetDistance = Mathf.Clamp(
            targetDistance - scroll * zoomSpeed * 0.01f,
            minDistance,
            maxDistance
        );
    }

    private void HandleOrbit()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || !mouse.middleButton.isPressed)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();

        targetYaw += delta.x * orbitSpeed;
        targetPitch = Mathf.Clamp(targetPitch - delta.y * orbitSpeed, minPitch, maxPitch);
    }

    private void HandlePan()
    {
        Vector3 move = Vector3.zero;

        Quaternion flatRotation = Quaternion.Euler(0f, targetYaw, 0f);
        Vector3 forward = flatRotation * Vector3.forward;
        Vector3 right = flatRotation * Vector3.right;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            float x = 0f;
            float z = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) z -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) z += 1f;

            move += (right * x + forward * z) * keyboardPanSpeed * distance * Time.deltaTime;
        }

        Mouse mouse = Mouse.current;

        if (mouse != null && mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            move += (right * -delta.x + forward * -delta.y) * dragPanSpeed * distance * 0.001f;
        }

        targetFocus += move;
        targetFocus.y = groundHeight;
    }

    private void ApplySmoothing()
    {
        if (smoothing <= 0f)
        {
            focusPoint = targetFocus;
            distance = targetDistance;
            yaw = targetYaw;
            pitch = targetPitch;
            return;
        }

        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);

        focusPoint = Vector3.Lerp(focusPoint, targetFocus, t);
        distance = Mathf.Lerp(distance, targetDistance, t);
        yaw = Mathf.LerpAngle(yaw, targetYaw, t);
        pitch = Mathf.Lerp(pitch, targetPitch, t);
    }

    private void ApplyTransform()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 position = focusPoint - rotation * Vector3.forward * distance;

        transform.SetPositionAndRotation(position, rotation);
    }

    private float NormalizePitch(float rawPitch)
    {
        if (rawPitch > 180f)
        {
            rawPitch -= 360f;
        }

        return Mathf.Clamp(rawPitch, minPitch, maxPitch);
    }
}
