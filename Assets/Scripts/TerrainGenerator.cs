using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Dimensiuni teren")]
    public int terrainWidth = 1000;  // marit la 1000 pentru spatiu mai mare
    public int terrainLength = 1000;
    public int terrainHeight = 300;   // inaltime maxima mare, dar doar in varfuri
    public int heightmapRes = 513;

    [Header("Relief")]
    // Scale foarte mic = forme FOARTE mari si rare = vai largi, dealuri rare
    public float baseScale = 0.0008f;  // forma generala - vai si campii largi
    public float midScale = 0.003f;   // dealuri de marime medie, rare
    public float detailScale = 0.008f;   // mici denivelari

    public float baseWeight = 0.70f;   // forma generala domina
    public float midWeight = 0.22f;
    public float detailWeight = 0.08f;   // detalii fine, minime

    // Putere mare = majoritatea terenului e JOS (campie), doar varfurile sunt inalte
    // 3.0 = ~80% din teren e campie, doar 20% are relief inalt
    public float heightCurve = 3.0f;

    public int seed = 42;

    [Header("Vegetatie")]
    public float vegNoiseScale = 0.015f;

    [Range(0f, 1f)] public float grassMaxHeight = 0.25f;
    [Range(0f, 1f)] public float shrubMaxHeight = 0.50f;
    [Range(0f, 1f)] public float forestMaxHeight = 0.75f;

    [Header("Texturi")]
    public Texture2D grassTexture;
    public Texture2D shrubTexture;
    public Texture2D forestTexture;
    public Texture2D rockTexture;

    private Terrain _terrain;
    private TerrainData _data;

    [HideInInspector] public VegetationType[,] vegetationGrid;

    void Start()
    {
        GenerateTerrain();
    }

    [ContextMenu("Generate Terrain")]
    public void GenerateTerrain()
    {
        _terrain = GetComponent<Terrain>();
        _data = _terrain.terrainData;

        SetTerrainSize();
        float[,] heights = GenerateHeights();
        _data.SetHeights(0, 0, heights);

        if (HasTextures())
            ApplyTextures(heights);

        BuildVegetationGrid(heights);
        // Anuntam GridRenderer ca e gata
        GridRenderer gr = GetComponent<GridRenderer>();
        if (gr != null)
            gr.DrawGrid();
        Debug.Log("[TerrainGenerator] Gata! " + terrainWidth + "x" + terrainLength);
    }

    void SetTerrainSize()
    {
        _data.heightmapResolution = heightmapRes;
        _data.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
    }

    float[,] GenerateHeights()
    {
        int res = _data.heightmapResolution;
        float[,] heights = new float[res, res];

        System.Random rng = new System.Random(seed);
        float ox = (float)rng.NextDouble() * 1000f;
        float oz = (float)rng.NextDouble() * 1000f;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / res * 3f;
                float nz = (float)z / res * 3f;

                float h = 0f;
                h += Mathf.PerlinNoise(nx + ox, nz + oz) * 0.5f;
                h += Mathf.PerlinNoise(nx * 2f + ox, nz * 2f + oz) * 0.3f;
                h += Mathf.PerlinNoise(nx * 4f + ox, nz * 4f + oz) * 0.2f;

                h = Mathf.Clamp01(h);
                h = Mathf.Pow(h, heightCurve);
                heights[z, x] = h;
            }
        }
        return heights;
    }

    bool HasTextures() =>
        grassTexture != null && shrubTexture != null &&
        forestTexture != null && rockTexture != null;

    void ApplyTextures(float[,] heights)
    {
        TerrainLayer[] layers = new TerrainLayer[4];
        layers[0] = MakeLayer(grassTexture, new Vector2(8, 8));   // era 40
        layers[1] = MakeLayer(shrubTexture, new Vector2(8, 8));   // era 35
        layers[2] = MakeLayer(forestTexture, new Vector2(8, 8));   // era 40
        layers[3] = MakeLayer(rockTexture, new Vector2(5, 5));   // era 20
        _data.terrainLayers = layers;

        int res = _data.alphamapResolution;
        float[,,] maps = new float[res, res, 4];

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float h = SampleHeight(heights, x, z, res);

                float nx = (float)x / res;
                float nz = (float)z / res;
                float vn = Mathf.PerlinNoise(
                    (nx + seed * 0.13f) / vegNoiseScale,
                    (nz + seed * 0.19f) / vegNoiseScale);
                float hMod = h + (vn - 0.5f) * 0.06f;

                float w0, w1, w2, w3;
                GetWeights(hMod, out w0, out w1, out w2, out w3);

                maps[z, x, 0] = w0;
                maps[z, x, 1] = w1;
                maps[z, x, 2] = w2;
                maps[z, x, 3] = w3;
            }
        }

        _data.SetAlphamaps(0, 0, maps);
    }

    TerrainLayer MakeLayer(Texture2D tex, Vector2 tileSize)
    {
        var l = new TerrainLayer();
        l.diffuseTexture = tex;
        l.tileSize = tileSize;
        return l;
    }

    float SampleHeight(float[,] heights, int ax, int az, int alphaRes)
    {
        int hmRes = _data.heightmapResolution;
        int hx = Mathf.RoundToInt((float)ax / alphaRes * (hmRes - 1));
        int hz = Mathf.RoundToInt((float)az / alphaRes * (hmRes - 1));
        return heights[hz, hx];
    }

    void GetWeights(float h, out float w0, out float w1, out float w2, out float w3)
    {
        w0 = w1 = w2 = w3 = 0f;

        if (h < grassMaxHeight)
        {
            w0 = 1f;
        }
        else if (h < shrubMaxHeight)
        {
            float t = Mathf.InverseLerp(grassMaxHeight, shrubMaxHeight, h);
            w0 = 1f - t;
            w1 = t;
        }
        else if (h < forestMaxHeight)
        {
            float t = Mathf.InverseLerp(shrubMaxHeight, forestMaxHeight, h);
            w1 = 1f - t;
            w2 = t;
        }
        else
        {
            float t = Mathf.InverseLerp(forestMaxHeight, 1f, h);
            w2 = 1f - t;
            w3 = t;
        }
    }

    void BuildVegetationGrid(float[,] heights)
    {
        int res = _data.heightmapResolution;
        vegetationGrid = new VegetationType[res, res];

        System.Random rng = new System.Random(seed);
        float ox = (float)rng.NextDouble() * 500f;
        float oz = (float)rng.NextDouble() * 500f;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / res * 4f;
                float nz = (float)z / res * 4f;

                // Noise complet independent de inaltime
                float v = Mathf.PerlinNoise(nx + ox, nz + oz);
                v += Mathf.PerlinNoise(nx * 2f + ox, nz * 2f + oz) * 0.5f;
                v /= 1.5f;

                if (v < 0.25f)
                    vegetationGrid[z, x] = VegetationType.Grass;
                else if (v < 0.50f)
                    vegetationGrid[z, x] = VegetationType.Shrub;
                else if (v < 0.75f)
                    vegetationGrid[z, x] = VegetationType.Forest;
                else
                    vegetationGrid[z, x] = VegetationType.Rock;
            }
        }

        Debug.Log("[TerrainGenerator] VegetationGrid gata: " + res + "x" + res);
        int cGrass = 0, cShrub = 0, cForest = 0, cRock = 0;
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                switch (vegetationGrid[z, x])
                {
                    case VegetationType.Grass: cGrass++; break;
                    case VegetationType.Shrub: cShrub++; break;
                    case VegetationType.Forest: cForest++; break;
                    case VegetationType.Rock: cRock++; break;
                }
            }
        Debug.Log("Grass:" + cGrass + " Shrub:" + cShrub +
                  " Forest:" + cForest + " Rock:" + cRock);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int res = _data.heightmapResolution;
        float px = (worldPos.x - transform.position.x) / terrainWidth;
        float pz = (worldPos.z - transform.position.z) / terrainLength;
        return new Vector2Int(
            Mathf.Clamp(Mathf.RoundToInt(px * (res - 1)), 0, res - 1),
            Mathf.Clamp(Mathf.RoundToInt(pz * (res - 1)), 0, res - 1)
        );
    }

    public float GetNormalizedHeight(Vector3 worldPos)
    {
        return _terrain.SampleHeight(worldPos) / terrainHeight;
    }
}