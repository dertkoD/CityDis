using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TowerBlock : MonoBehaviour
{
    [Header("Anchors")]
    [SerializeField] private Transform bottomAnchor;
    [SerializeField] private Transform topAnchor;

    private Rigidbody2D _rigidbody;

    private bool _isDropped;
    private bool _hasLanded;
    private bool _shouldStayUpright;

    private float _uprightTorqueStrength;
    private float _uprightDamping;

    public Rigidbody2D Rigidbody => _rigidbody;

    public float BottomY => bottomAnchor.position.y;
    public float TopY => topAnchor.position.y;
    public Vector3 RopeAttachPosition => topAnchor.position;

    public event Action<TowerBlock> Landed;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (_shouldStayUpright) KeepUpright();
    }

    public void SetHeldState()
    {
        _isDropped = false;
        _hasLanded = false;
        _shouldStayUpright = false;

        SetKinematicPhysics();

        transform.rotation = Quaternion.identity;
    }

    public void SetDroppedState(float xVelocity, TowerBloxxConfig config)
    {
        _isDropped = true;
        _hasLanded = false;

        transform.rotation = Quaternion.identity;

        _rigidbody.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody.linearVelocity = new Vector2(xVelocity, 0f);
        _rigidbody.angularVelocity = 0f;
        _rigidbody.gravityScale = 3f;

        _shouldStayUpright = config.keepBlockUprightWhileFalling;
        _uprightTorqueStrength = config.uprightTorqueStrength;
        _uprightDamping = config.uprightDamping;
    }

    public void LockAsStackBlock()
    {
        _isDropped = false;
        _shouldStayUpright = false;

        transform.rotation = Quaternion.identity;

        SetKinematicPhysics();
    }

    public void ReturnToCrane(Vector3 position, Quaternion rotation)
    {
        _isDropped = false;
        _hasLanded = false;
        _shouldStayUpright = false;

        SetKinematicPhysics();

        transform.position = position;
        transform.rotation = rotation;
    }

    private void SetKinematicPhysics()
    {
        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
        _rigidbody.gravityScale = 0f;
    }

    private void KeepUpright()
    {
        float angle = Mathf.DeltaAngle(transform.eulerAngles.z, 0f);

        float torque = angle * _uprightTorqueStrength;
        torque -= _rigidbody.angularVelocity * _uprightDamping;

        _rigidbody.AddTorque(torque);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_isDropped || _hasLanded) return;

        _hasLanded = true;
        Landed?.Invoke(this);
    }
}