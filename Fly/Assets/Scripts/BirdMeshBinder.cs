using UnityEngine;

// Model mesh IDs are assigned by Unity's importer and can change between imports.
// Resolve the model assets by path so the editable bird hierarchy always renders.
[ExecuteAlways]
public class BirdMeshBinder : MonoBehaviour
{
    private void OnEnable()
    {
        BindMeshes();
    }

    private void Start()
    {
        BindMeshes();
    }

    private void BindMeshes()
    {
        foreach (MeshFilter target in GetComponentsInChildren<MeshFilter>(true))
        {
            if (target.transform == transform) continue; // Hidden prototype collider mesh.

            string name = target.gameObject.name;
            string assetName = name switch
            {
                "Bird Body" => "body",
                "Cream Belly" => "belly",
                "Eye Highlights" => "eye_glints",
                "Tail Feathers" => "tail",
                _ => name.ToLowerInvariant().Replace(' ', '_')
            };

            string path = "BirdArt/" + assetName;
            GameObject model = Resources.Load<GameObject>(path);
            MeshFilter source = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
            Mesh mesh = source != null ? source.sharedMesh : null;
            if (mesh == null)
            {
                Mesh[] subassets = Resources.LoadAll<Mesh>(path);
                if (subassets.Length > 0) mesh = subassets[0];
            }

            if (mesh != null)
                target.sharedMesh = mesh;
            else
                Debug.LogWarning($"Bird model mesh missing: {path}", target);
        }
    }
}
