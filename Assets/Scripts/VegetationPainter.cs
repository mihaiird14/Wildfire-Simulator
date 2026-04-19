using UnityEngine;
[RequireComponent(typeof(TerrainGenerator))]
public class VegetationPainter : MonoBehaviour
{

    [Header("Prefaburi copaci (drag din Project)")]
    public GameObject forestTreePrefab;  
    public GameObject shrubPrefab;       
    public GameObject grassDetailPrefab; 
    [Header("Densitate (1 = rar, 10 = des)")]
    [Range(1, 20)] public int forestDensity = 5; 
    [Range(1, 20)] public int shrubDensity = 3;  
    [Range(1, 20)] public int grassDensity = 8;  


    [Header("Variatie vizuala")]
    public float minScale = 0.8f;   // scala minima 
    public float maxScale = 1.4f;   // scala maxima
    public bool randomRotation = true;  // rotatie aleatoare pe Y

    private TerrainGenerator _terrainGen;
    private Terrain _terrain;

    void Start()
    {
        _terrainGen = GetComponent<TerrainGenerator>();
        _terrain = GetComponent<Terrain>();
        Invoke("PaintVegetation", 0.1f);
    }

    [ContextMenu("Paint Vegetation")]
    public void PaintVegetation()
    {
        _terrainGen = GetComponent<TerrainGenerator>();
        _terrain = GetComponent<Terrain>();

        if (_terrainGen.vegetationGrid == null)
        {
            Debug.LogError("[VegetationPainter] vegetationGrid e null! " +
                           "Ruleaza mai intai TerrainGenerator.");
            return;
        }

        // Stergem copacii vechi ca sa nu se adune la fiecare regenerare
        ClearTrees();

        int res = _terrainGen.vegetationGrid.GetLength(0);
        int planted = 0;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                VegetationType type = _terrainGen.vegetationGrid[z, x];

                bool shouldPlant = false;
                GameObject prefabToUse = null;

                if (type == VegetationType.Forest && forestTreePrefab != null)
                {

                    shouldPlant = (x % forestDensity == 0 && z % forestDensity == 0);
                    prefabToUse = forestTreePrefab;
                }
                else if (type == VegetationType.Shrub && shrubPrefab != null)
                {
                    shouldPlant = (x % shrubDensity == 0 && z % shrubDensity == 0);
                    prefabToUse = shrubPrefab;
                }
                else if (type == VegetationType.Grass && grassDetailPrefab != null)
                {
                    shouldPlant = (x % grassDensity == 0 && z % grassDensity == 0);
                    prefabToUse = grassDetailPrefab;
                }

                if (shouldPlant && prefabToUse != null)
                {
                    PlantObject(prefabToUse, x, z, res);
                    planted++;
                }
            }
        }

        Debug.Log("[VegetationPainter] Plantat " + planted + " obiecte.");
    }

    void PlantObject(GameObject prefab, int gridX, int gridZ, int res)
    {

        float nx = (float)gridX / res;
        float nz = (float)gridZ / res;

        float cellSize = (float)_terrainGen.terrainWidth / res;
        float offsetX = Random.Range(-cellSize * 0.4f, cellSize * 0.4f);
        float offsetZ = Random.Range(-cellSize * 0.4f, cellSize * 0.4f);

        float worldX = nx * _terrainGen.terrainWidth + offsetX;
        float worldZ = nz * _terrainGen.terrainLength + offsetZ;

        float worldY = _terrain.SampleHeight(new Vector3(worldX, 0, worldZ));

        Vector3 pos = new Vector3(worldX, worldY, worldZ);

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);

        if (randomRotation)
        {
            obj.transform.rotation = Quaternion.Euler(
                0f,
                Random.Range(0f, 360f),
                0f
            );
        }

        float scale = Random.Range(minScale, maxScale);
        obj.transform.localScale = Vector3.one * scale;

        obj.transform.parent = this.transform;
    }

    [ContextMenu("Clear Vegetation")]
    public void ClearTrees()
    {

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        Debug.Log("[VegetationPainter] Vegetatie stearsa.");
    }
}