using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TowerBlock : MonoBehaviour
{
    [SerializeField] private Transform bottomAnchor;
    [SerializeField] private Transform topAnchor;

    private Rigidbody2D _rigidbody;

    public Rigidbody2D Rigidbody => _rigidbody;

    public float BottomY => bottomAnchor.position.y;
    public float TopY => topAnchor.position.y;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    public void SetHeldState()
    {
        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
        _rigidbody.gravityScale = 0f;
    }

    public void SetDroppedState()
    {
        _rigidbody.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
        _rigidbody.gravityScale = 3f;
    }

    public void LockAsStackBlock()
    {
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
    }

    public bool IsSettled(float velocityThreshold, float angularVelocityThreshold)
    {
        bool lowLinearVelocity = _rigidbody.linearVelocity.magnitude <= velocityThreshold;
        bool lowAngularVelocity = Mathf.Abs(_rigidbody.angularVelocity) <= angularVelocityThreshold;

        return lowLinearVelocity && lowAngularVelocity;
    }
}