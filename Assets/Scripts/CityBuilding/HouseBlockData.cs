using System;
using UnityEngine;

[Serializable]
public struct HouseBlockData
{
    [SerializeField] private Vector2 position;
    [SerializeField] private float angle;
    [SerializeField] private Vector2 size;

    public Vector2 Position => position;
    public float Angle => angle;
    public Vector2 Size => size;

    public HouseBlockData(Vector2 position, float angle, Vector2 size)
    {
        this.position = position;
        this.angle = angle;
        this.size = size;
    }
}
