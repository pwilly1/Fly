using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(RiverPathAuthoring))]
public sealed class RiverPathAuthoringEditor : Editor
{
    private const float MinimumWidth = 2f;
    private const float RowSpacing = 4f;
    private readonly float[] across = { 0f, 0.12f, 0.5f, 0.88f, 1f };
    private readonly float[] edgeAlpha = { 0f, 0.86f, 0.96f, 0.86f, 0f };
    private int selectedPoint = 4;

    public override void OnInspectorGUI()
    {
        var path = (RiverPathAuthoring)target;
        var points = path.Points;
        EditorGUILayout.HelpBox(
            "Select a point here or click a numbered dot in Scene view. Drag its center to bend or raise the river. Drag either blue edge handle to change its width. Changes rebuild the saved mesh in the editor only.",
            MessageType.Info);

        if (points.Count < 2)
        {
            EditorGUILayout.HelpBox("The river needs at least two points.", MessageType.Warning);
            return;
        }

        selectedPoint = Mathf.Clamp(selectedPoint, 0, points.Count - 1);
        var labels = new string[points.Count];
        for (int i = 0; i < points.Count; i++)
            labels[i] = $"{i + 1:00}  •  z {points[i].center.z:0}  •  width {points[i].width:0}";
        int newSelection = EditorGUILayout.Popup("Control point", selectedPoint, labels);
        if (newSelection != selectedPoint)
        {
            selectedPoint = newSelection;
            SceneView.RepaintAll();
        }

        var point = points[selectedPoint];
        EditorGUI.BeginChangeCheck();
        Vector3 center = EditorGUILayout.Vector3Field("Center (local)", point.center);
        float width = EditorGUILayout.FloatField("Width", point.width);
        if (EditorGUI.EndChangeCheck())
        {
            RecordAndUpdate(path, "Edit river point", () =>
            {
                point.center = ClampZ(points, selectedPoint, center);
                point.width = Mathf.Max(MinimumWidth, width);
            });
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Insert point after"))
        {
            RecordAndUpdate(path, "Insert river point", () =>
            {
                var current = points[selectedPoint];
                Vector3 next = selectedPoint < points.Count - 1
                    ? points[selectedPoint + 1].center
                    : current.center + (current.center - points[selectedPoint - 1].center);
                float nextWidth = selectedPoint < points.Count - 1
                    ? points[selectedPoint + 1].width : current.width;
                points.Insert(selectedPoint + 1, new RiverPathAuthoring.Point
                {
                    center = Vector3.Lerp(current.center, next, 0.5f),
                    width = (current.width + nextWidth) * 0.5f
                });
                selectedPoint++;
            });
        }
        using (new EditorGUI.DisabledScope(points.Count <= 2))
        {
            if (GUILayout.Button("Delete selected"))
            {
                RecordAndUpdate(path, "Delete river point", () =>
                {
                    points.RemoveAt(selectedPoint);
                    selectedPoint = Mathf.Clamp(selectedPoint, 0, points.Count - 1);
                });
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Rebuild river mesh"))
            RecordAndUpdate(path, "Rebuild river mesh", () => { });
    }

    private void OnSceneGUI()
    {
        var path = (RiverPathAuthoring)target;
        var points = path.Points;
        if (points.Count < 2) return;
        selectedPoint = Mathf.Clamp(selectedPoint, 0, points.Count - 1);

        var centers = new Vector3[points.Count];
        var left = new Vector3[points.Count];
        var right = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            centers[i] = path.transform.TransformPoint(p.center);
            left[i] = path.transform.TransformPoint(p.center + Vector3.left * p.width * 0.5f);
            right[i] = path.transform.TransformPoint(p.center + Vector3.right * p.width * 0.5f);
        }

        Handles.color = new Color(0.12f, 0.94f, 0.95f, 1f);
        Handles.DrawAAPolyLine(4f, centers);
        Handles.color = new Color(0.25f, 0.8f, 0.95f, 0.75f);
        Handles.DrawAAPolyLine(2f, left);
        Handles.DrawAAPolyLine(2f, right);
        for (int i = 0; i < points.Count; i++)
        {
            float size = HandleUtility.GetHandleSize(centers[i]) * (i == selectedPoint ? 0.14f : 0.10f);
            Handles.color = i == selectedPoint ? Color.yellow : Color.cyan;
            if (Handles.Button(centers[i], Quaternion.identity, size, size * 1.3f, Handles.SphereHandleCap))
            {
                selectedPoint = i;
                Repaint();
            }
            Handles.Label(centers[i] + Vector3.up * size * 1.6f, (i + 1).ToString());
        }

        var selected = points[selectedPoint];
        EditorGUI.BeginChangeCheck();
        Vector3 movedWorld = Handles.PositionHandle(centers[selectedPoint], Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            RecordAndUpdate(path, "Move river point", () =>
                selected.center = ClampZ(points, selectedPoint, path.transform.InverseTransformPoint(movedWorld)));
        }

        Handles.color = new Color(0.25f, 0.8f, 1f, 1f);
        Handles.DrawLine(left[selectedPoint], right[selectedPoint]);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 edge = side < 0 ? left[selectedPoint] : right[selectedPoint];
            Vector3 direction = path.transform.TransformDirection(Vector3.right * side);
            float size = HandleUtility.GetHandleSize(edge) * 0.15f;
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.Slider(edge, direction, size, Handles.CubeHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 local = path.transform.InverseTransformPoint(moved);
                float newWidth = Mathf.Max(MinimumWidth, Mathf.Abs(local.x - selected.center.x) * 2f);
                RecordAndUpdate(path, "Resize river at point", () => selected.width = newWidth);
            }
        }
    }

    private static Vector3 ClampZ(List<RiverPathAuthoring.Point> points, int index, Vector3 center)
    {
        if (index > 0) center.z = Mathf.Max(center.z, points[index - 1].center.z + 2f);
        if (index < points.Count - 1) center.z = Mathf.Min(center.z, points[index + 1].center.z - 2f);
        return center;
    }

    private void RecordAndUpdate(RiverPathAuthoring path, string undoName, System.Action edit)
    {
        var mesh = path.GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh)))
        {
            Debug.LogError("River mesh must be a saved project asset before editing.", path);
            return;
        }
        Undo.RecordObject(path, undoName);
        Undo.RegisterCompleteObjectUndo(mesh, undoName);
        edit();
        Rebuild(path, mesh);
        EditorUtility.SetDirty(path);
        EditorUtility.SetDirty(mesh);
        EditorSceneManager.MarkSceneDirty(path.gameObject.scene);
        AssetDatabase.SaveAssetIfDirty(mesh);
        SceneView.RepaintAll();
    }

    private void Rebuild(RiverPathAuthoring path, Mesh mesh)
    {
        var points = path.Points;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var uv2 = new List<Vector2>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        float distance = 0f;
        Vector3 previousCenter = Vector3.zero;
        int row = 0;

        for (int segment = 0; segment < points.Count - 1; segment++)
        {
            var a = points[segment];
            var b = points[segment + 1];
            var before = points[Mathf.Max(0, segment - 1)];
            var after = points[Mathf.Min(points.Count - 1, segment + 2)];
            int steps = Mathf.Max(1, Mathf.CeilToInt((b.center.z - a.center.z) / RowSpacing));
            for (int step = 0; step <= steps; step++)
            {
                if (segment > 0 && step == 0) continue;
                float t = step / (float)steps;
                float z = Mathf.Lerp(a.center.z, b.center.z, t);
                float x = Catmull(before.center.x, a.center.x, b.center.x, after.center.x, t);
                float y = Catmull(before.center.y, a.center.y, b.center.y, after.center.y, t);
                float width = Mathf.Max(MinimumWidth, Catmull(before.width, a.width, b.width, after.width, t));
                var center = new Vector3(x, y, z);
                if (row > 0) distance += Vector3.Distance(previousCenter, center);
                previousCenter = center;
                for (int k = 0; k < 5; k++)
                {
                    float fraction = across[k];
                    vertices.Add(center + Vector3.right * (fraction - 0.5f) * width);
                    uv.Add(new Vector2(fraction * 1.8f, distance / 28f));
                    uv2.Add(new Vector2(fraction, 0f));
                    colors.Add(new Color(1f, 1f, 1f, edgeAlpha[k]));
                }
                if (row > 0)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        int prior = (row - 1) * 5 + k;
                        int current = row * 5 + k;
                        triangles.Add(prior); triangles.Add(current); triangles.Add(prior + 1);
                        triangles.Add(prior + 1); triangles.Add(current); triangles.Add(current + 1);
                    }
                }
                row++;
            }
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetUVs(1, uv2);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private static float Catmull(float p0, float p1, float p2, float p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (2f * p1 + (-p0 + p2) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
}
