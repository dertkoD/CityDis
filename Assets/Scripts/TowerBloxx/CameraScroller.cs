using System.Collections;
using UnityEngine;

public class CameraScroller : MonoBehaviour
{
    [SerializeField] private TowerBloxxConfig config;
    [SerializeField] private Camera targetCamera;

    [Header("Follow Target")]
    [SerializeField] private float topBlockScreenOffset = 1.5f;

    private Coroutine _moveRoutine;

    public void MoveToTowerTop(float towerTopY)
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
        }

        Vector3 targetPosition = targetCamera.transform.position;
        targetPosition.y = towerTopY + topBlockScreenOffset;

        _moveRoutine = StartCoroutine(MoveRoutine(targetPosition));
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition)
    {
        Vector3 startPosition = targetCamera.transform.position;
        float timer = 0f;

        while (timer < config.cameraMoveDuration)
        {
            timer += Time.deltaTime;

            float t = timer / config.cameraMoveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            targetCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        targetCamera.transform.position = targetPosition;
    }
}