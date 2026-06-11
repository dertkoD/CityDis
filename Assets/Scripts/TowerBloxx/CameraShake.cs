using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float defaultDuration = 0.12f;
    [SerializeField] private float defaultStrength = 0.08f;

    private Coroutine _shakeRoutine;

    public void Shake()
    {
        Shake(defaultDuration, defaultStrength);
    }

    public void Shake(float duration, float strength)
    {
        if (!targetCamera) return;

        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);

        _shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength));
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        Vector3 originalPosition = targetCamera.transform.position;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            Vector2 randomOffset = Random.insideUnitCircle * strength;
            targetCamera.transform.position = originalPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);

            yield return null;
        }

        targetCamera.transform.position = originalPosition;
        _shakeRoutine = null;
    }
}