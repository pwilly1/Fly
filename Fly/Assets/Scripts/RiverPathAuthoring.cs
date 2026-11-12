using System;
using System.Collections.Generic;
using UnityEngine;

// Stores the river's editable shape. Mesh rebuilding lives in the Editor folder.
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class RiverPathAuthoring : MonoBehaviour
{
    [Serializable]
    public sealed class Point
    {
        public Vector3 center;
        [Min(2f)] public float width = 40f;
    }

    [SerializeField] private List<Point> points = new List<Point>();

    public List<Point> Points => points;
}
