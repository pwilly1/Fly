using UnityEngine;

// Generates the first canyon layout when the Forest scene enters Play mode.
// The bird and ring course remain separate, editable scene objects.
public class ForestCanyonLayout : MonoBehaviour
{
    private const int Resolution = 257;
    private const float Width = 120f;
    private const float Length = 120f;
    private const float Height = 20f;
    private static readonly Vector3 TerrainOrigin = new Vector3(-60f, -2f, -10f);

    private void Awake()
    {
        BuildTerrain();
        BuildRiver();
    }

    private void BuildTerrain()
    {
        var data = new TerrainData
        {
            heightmapResolution = Resolution,
            alphamapResolution = 128,
            size = new Vector3(Width, Height, Length)
        };

        var heights = new float[Resolution, Resolution];
        for (int z = 0; z < Resolution; z++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                float worldX = TerrainOrigin.x + x * Width / (Resolution - 1);
                float worldZ = TerrainOrigin.z + z * Length / (Resolution - 1);
                heights[z, x] = Mathf.Clamp01((GroundHeight(worldX, worldZ) - TerrainOrigin.y) / Height);
            }
        }
        data.SetHeights(0, 0, heights);

        data.terrainLayers = new[]
        {
            MakeLayer("Mossy grass", new Color(0.29f, 0.47f, 0.23f)),
            MakeLayer("Canyon stone", new Color(0.38f, 0.42f, 0.39f)),
            MakeLayer("River shore", new Color(0.61f, 0.58f, 0.43f))
        };

        var splats = new float[128, 128, 3];
        for (int z = 0; z < 128; z++)
        {
            for (int x = 0; x < 128; x++)
            {
                float worldX = TerrainOrigin.x + x * Width / 127f;
                float worldZ = TerrainOrigin.z + z * Length / 127f;
                float distance = Mathf.Abs(worldX - RiverCenter(worldZ));
                float stone = Mathf.SmoothStep(0f, 0.82f, Mathf.InverseLerp(15f, 23f, distance));
                float shore = (1f - Mathf.SmoothStep(3.7f, 8f, distance)) * 0.72f;
                splats[z, x, 0] = 1f - stone - shore;
                splats[z, x, 1] = stone;
                splats[z, x, 2] = shore;
            }
        }
        data.SetAlphamaps(0, 0, splats);

        var terrainObject = Terrain.CreateTerrainGameObject(data);
        terrainObject.name = "Canyon banks and riverbed";
        terrainObject.transform.SetParent(transform);
        terrainObject.transform.position = TerrainOrigin;
        terrainObject.GetComponent<Terrain>().drawInstanced = true;
    }

    private void BuildRiver()
    {
        var water = MakeMaterial(new Color(0.12f, 0.56f, 0.65f), 0.78f);
        var edge = MakeMaterial(new Color(0.72f, 0.88f, 0.82f), 0.32f);
        MakeRibbon("River water - placeholder", 0f, 4.1f, -0.22f, water);
        MakeRibbon("Left river edge", -4.13f, 0.16f, -0.17f, edge);
        MakeRibbon("Right river edge", 4.13f, 0.16f, -0.17f, edge);
    }

    private static float RiverCenter(float z)
    {
        return 2f * Mathf.Sin((z - 8f) * 0.055f);
    }

    private static float GroundHeight(float x, float z)
    {
        float distance = Mathf.Abs(x - RiverCenter(z));
        float shore = Mathf.SmoothStep(0f, 1.2f, Mathf.InverseLerp(3.6f, 9.5f, distance));
        float bank = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(14f, 31f, distance));
        float broadNoise = (Mathf.PerlinNoise(x * 0.037f + 10f, z * 0.043f + 3f) - 0.5f) * 3.2f;
        float fineNoise = (Mathf.PerlinNoise(x * 0.11f + 30f, z * 0.12f + 7f) - 0.5f) * 0.75f;
        return -0.75f + shore + bank * (11.5f + broadNoise + fineNoise);
    }

    private static TerrainLayer MakeLayer(string name, Color color)
    {
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, true) { name = name + " texture" };
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                float tint = ((x + y * 3) % 4 - 1.5f) * 0.018f;
                texture.SetPixel(x, y, color + new Color(tint, tint, tint, 0f));
            }
        }
        texture.Apply();
        return new TerrainLayer
        {
            name = name,
            diffuseTexture = texture,
            tileSize = new Vector2(8f, 8f),
            smoothness = 0.04f
        };
    }

    private static Material MakeMaterial(Color color, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var material = new Material(shader);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private void MakeRibbon(string name, float offset, float halfWidth, float y, Material material)
    {
        const int segments = 80;
        var vertices = new Vector3[(segments + 1) * 2];
        var triangles = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float z = TerrainOrigin.z + i * Length / segments;
            float center = RiverCenter(z) + offset;
            vertices[i * 2] = new Vector3(center - halfWidth, y, z);
            vertices[i * 2 + 1] = new Vector3(center + halfWidth, y, z);
            if (i == segments) continue;
            int t = i * 6;
            int v = i * 2;
            triangles[t] = v;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 1;
            triangles[t + 4] = v + 2;
            triangles[t + 5] = v + 3;
        }

        var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var ribbon = new GameObject(name);
        ribbon.transform.SetParent(transform);
        ribbon.AddComponent<MeshFilter>().sharedMesh = mesh;
        ribbon.AddComponent<MeshRenderer>().sharedMaterial = material;
    }
}
