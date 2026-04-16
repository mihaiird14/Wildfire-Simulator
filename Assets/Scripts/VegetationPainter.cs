using UnityEngine;

// ================================================================
// VegetationPainter.cs
// ================================================================
// Plaseaza automat copaci si tufe pe teren in functie de tipul
// de vegetatie din grid-ul generat de TerrainGenerator.
//
// Cum se foloseste:
//   1. Ataseaza scriptul pe acelasi obiect ca TerrainGenerator
//   2. Asigneaza prefab-urile de copaci/tufe in Inspector
//   3. Apasa Play sau click dreapta → Paint Vegetation
// ================================================================

[RequireComponent(typeof(TerrainGenerator))]
public class VegetationPainter : MonoBehaviour
{
    // ================================================================
    // PREFAB-URI – trage din Project in Inspector
    // ================================================================

    [Header("Prefaburi copaci (drag din Project)")]
    public GameObject forestTreePrefab;   // copac mare pentru Forest
    public GameObject shrubPrefab;        // tuf/arbust pentru Shrub
    public GameObject grassDetailPrefab;  // optional: plante mici pentru Grass

    // ================================================================
    // DENSITATE – cat de des apar
    // ================================================================

    [Header("Densitate (1 = rar, 10 = des)")]
    [Range(1, 20)] public int forestDensity = 5;  // 1 copac la fiecare 5 celule
    [Range(1, 20)] public int shrubDensity = 3;  // 1 tuf la fiecare 3 celule
    [Range(1, 20)] public int grassDensity = 8;  // plante rare pe campie

    // ================================================================
    // VARIATIE – copacii nu sunt identici
    // ================================================================

    [Header("Variatie vizuala")]
    public float minScale = 0.8f;   // scala minima (copaci mai mici)
    public float maxScale = 1.4f;   // scala maxima (copaci mai mari)
    public bool randomRotation = true;  // rotatie aleatoare pe Y

    // ================================================================
    // REFERINTE INTERNE
    // ================================================================

    private TerrainGenerator _terrainGen;
    private Terrain _terrain;

    void Start()
    {
        _terrainGen = GetComponent<TerrainGenerator>();
        _terrain = GetComponent<Terrain>();

        // Asteptam un frame ca TerrainGenerator sa termine generarea
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

                // Verificam daca in celula asta trebuie plantat ceva
                // bazat pe densitate (modulo = 1 din N celule)
                bool shouldPlant = false;
                GameObject prefabToUse = null;

                if (type == VegetationType.Forest && forestTreePrefab != null)
                {
                    // Plantam un copac la fiecare forestDensity celule
                    // + offset aleator ca sa nu fie in grid perfect
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

    // ================================================================
    // PLANTEAZA UN OBIECT la coordonatele de grid (x, z)
    // ================================================================
    void PlantObject(GameObject prefab, int gridX, int gridZ, int res)
    {
        // Convertim coordonatele grid in pozitie normalizata [0, 1]
        float nx = (float)gridX / res;
        float nz = (float)gridZ / res;

        // Adaugam un offset aleator mic ca copacii sa nu fie in linie perfecta
        // (pana la jumatate din distanta dintre celule)
        float cellSize = (float)_terrainGen.terrainWidth / res;
        float offsetX = Random.Range(-cellSize * 0.4f, cellSize * 0.4f);
        float offsetZ = Random.Range(-cellSize * 0.4f, cellSize * 0.4f);

        // Pozitia in spatiul lumii
        float worldX = nx * _terrainGen.terrainWidth + offsetX;
        float worldZ = nz * _terrainGen.terrainLength + offsetZ;

        // Inaltimea exacta a terenului in acel punct
        float worldY = _terrain.SampleHeight(new Vector3(worldX, 0, worldZ));

        Vector3 pos = new Vector3(worldX, worldY, worldZ);

        // Instantiem prefab-ul
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);

        // Rotatie aleatoare pe axa Y (copacii nu sunt toti orientati la fel)
        if (randomRotation)
        {
            obj.transform.rotation = Quaternion.Euler(
                0f,
                Random.Range(0f, 360f),
                0f
            );
        }

        // Scala aleatoare intre min si max pentru varietate vizuala
        float scale = Random.Range(minScale, maxScale);
        obj.transform.localScale = Vector3.one * scale;

        // Facem obiectul copil al terenului ca sa fie organizat in Hierarchy
        obj.transform.parent = this.transform;
    }

    // ================================================================
    // STERGE TOTI COPACII PLANTATI ANTERIOR
    // ================================================================
    // Necesar ca la regenerarea terenului sa nu se adune copaci vechi
    // peste cei noi.
    // ================================================================
    [ContextMenu("Clear Vegetation")]
    public void ClearTrees()
    {
        // Stergem toti copiii acestui GameObject (copacii plantati anterior)
        // Mergem invers ca sa nu avem probleme cu indexarea in timp ce stergem
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        Debug.Log("[VegetationPainter] Vegetatie stearsa.");
    }
}