using UnityEngine;

public class GridRenderer : MonoBehaviour
{
    public TerrainGenerator terrainGen;
    public int gridSize = 513;

    private Texture2D _gridTex;
    private GameObject _quad;

    void Start()
    {
        // Asteptam un frame ca TerrainGenerator sa termine in Start()
        Invoke("DrawGrid", 0.5f);
    }

    public void DrawGrid()
    {
        if (terrainGen == null || terrainGen.vegetationGrid == null)
        {
            Debug.LogError("[GridRenderer] vegetationGrid e null!");
            return;
        }

        // Citim dimensiunea reala din grid, nu din Inspector
        int size = terrainGen.vegetationGrid.GetLength(0);

        _gridTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _gridTex.filterMode = FilterMode.Point;

        for (int z = 0; z < size; z++)
            for (int x = 0; x < size; x++)
            {
                Color c = GetVegColor(terrainGen.vegetationGrid[z, x]);
                _gridTex.SetPixel(x, z, c);
            }

        _gridTex.Apply();

        if (_quad == null)
        {
            _quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _quad.transform.parent = transform;
            _quad.transform.position = new Vector3(50, 50, 50);
            _quad.transform.rotation = Quaternion.Euler(90, 0, 0);
            _quad.transform.localScale = new Vector3(100, 100, 1);
        }

        var mat = new Material(Shader.Find("Unlit/Texture"));
        mat.mainTexture = _gridTex;
        _quad.GetComponent<Renderer>().material = mat;

        Debug.Log("[GridRenderer] Grid desenat: " + size + "x" + size);
    }
    Color GetVegColor(VegetationType type)
    {
        return type switch
        {
            VegetationType.Grass => new Color(0.4f, 0.8f, 0.2f),
            VegetationType.Shrub => new Color(0.6f, 0.5f, 0.2f),
            VegetationType.Forest => new Color(0.1f, 0.4f, 0.1f),
            VegetationType.Rock => new Color(0.5f, 0.5f, 0.5f),
            _ => Color.black
        };
    }
}