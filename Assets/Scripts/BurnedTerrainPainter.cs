using UnityEngine;

// ================================================================
// BurnedTerrainPainter.cs
// ================================================================
// Cand o celula devine Burned, coloreaza zona corespunzatoare
// pe terenul 3D cu o textura de pamant ars (negru/maro inchis).
// Foloseste alphamap-ul terenului — acelasi sistem cu care
// TerrainGenerator a aplicat texturile de vegetatie.
// ================================================================

public class BurnedTerrainPainter : MonoBehaviour
{
    public FireSimulator fireSimulator;
    public TerrainGenerator terrainGen;

    [Header("Textura teren ars")]
    public Texture2D burnedTexture;  // textura neagra/maro inchis

    [Header("Setari")]
    public float updateInterval = 0.3f;
    public float burnBlend = 0.92f;  // cat de mult acopera textura arsa (0-1)

    private TerrainData _terrainData;
    private float[,,] _alphamap;
    private int _alphaRes;
    private int _burnedLayerIndex = -1;
    private bool _initialized = false;
    private float _timer = 0f;

    void Start()
    {
        StartCoroutine(InitAfterTerrain());
    }

    System.Collections.IEnumerator InitAfterTerrain()
    {
        yield return new WaitForSeconds(1.5f);
        Init();
    }

    void Init()
    {
        if (Terrain.activeTerrain == null) return;

        _terrainData = Terrain.activeTerrain.terrainData;
        _alphaRes = _terrainData.alphamapResolution;
        _alphamap = _terrainData.GetAlphamaps(0, 0, _alphaRes, _alphaRes);

        // Adaugam un layer nou pentru terenul ars
        if (burnedTexture != null)
        {
            TerrainLayer[] existingLayers = _terrainData.terrainLayers;
            TerrainLayer burnedLayer = new TerrainLayer();
            burnedLayer.diffuseTexture = burnedTexture;
            burnedLayer.tileSize = new Vector2(5, 5);

            // Adaugam layerul ars la sfarsitul listei existente
            TerrainLayer[] newLayers = new TerrainLayer[existingLayers.Length + 1];
            existingLayers.CopyTo(newLayers, 0);
            newLayers[newLayers.Length - 1] = burnedLayer;
            _terrainData.terrainLayers = newLayers;

            _burnedLayerIndex = newLayers.Length - 1;

            // Refacem alphamap-ul cu noul layer adaugat
            float[,,] newAlpha = new float[_alphaRes, _alphaRes, newLayers.Length];
            for (int z = 0; z < _alphaRes; z++)
                for (int x = 0; x < _alphaRes; x++)
                    for (int l = 0; l < existingLayers.Length; l++)
                        newAlpha[z, x, l] = _alphamap[z, x, l];
            _alphamap = newAlpha;
            _terrainData.SetAlphamaps(0, 0, _alphamap);
        }
        else
        {
            // Daca nu avem textura, folosim ultimul layer existent si il inchidem la culoare
            _burnedLayerIndex = _terrainData.terrainLayers.Length - 1;
            Debug.LogWarning("[BurnedTerrainPainter] Nu ai asignat burnedTexture! " +
                             "Asigneaza o textura neagra in Inspector.");
        }

        _initialized = true;
        Debug.Log("[BurnedTerrainPainter] Initializat. Layer ars: " + _burnedLayerIndex);
    }

    void Update()
    {
        if (!_initialized) return;
        if (fireSimulator == null || fireSimulator.States == null) return;

        _timer += Time.deltaTime;
        if (_timer < updateInterval) return;
        _timer = 0f;

        UpdateBurnedAreas();
    }

    // ================================================================
    // ACTUALIZEAZA ZONELE ARSE PE TEREN
    // ================================================================
    void UpdateBurnedAreas()
    {
        int res = fireSimulator.States.GetLength(0);
        bool modified = false;

        for (int gz = 0; gz < res; gz++)
        {
            for (int gx = 0; gx < res; gx++)
            {
                if (fireSimulator.States[gz, gx] != CellState.Burned) continue;

                // Convertim coordonate grid in coordonate alphamap
                int ax = Mathf.RoundToInt((float)gx / (res - 1) * (_alphaRes - 1));
                int az = Mathf.RoundToInt((float)gz / (res - 1) * (_alphaRes - 1));

                // Verificam daca celula e deja pictata ca arsa
                if (_alphamap[az, ax, _burnedLayerIndex] >= burnBlend) continue;

                // Pictam o zona mica in jurul celulei pentru margini mai naturale
                int radius = Mathf.Max(1, _alphaRes / res);
                for (int dz = -radius; dz <= radius; dz++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int px = Mathf.Clamp(ax + dx, 0, _alphaRes - 1);
                        int pz = Mathf.Clamp(az + dz, 0, _alphaRes - 1);

                        // Blend mai slab la margini pentru tranzitie naturala
                        float dist = Mathf.Sqrt(dx * dx + dz * dz) / (radius + 1f);
                        float blend = burnBlend * (1f - dist * 0.4f);

                        PaintBurned(px, pz, blend);
                    }
                }

                modified = true;
            }
        }

        if (modified)
            _terrainData.SetAlphamaps(0, 0, _alphamap);
    }

    // ================================================================
    // PICTEAZA UN PIXEL CA ARS
    // Reduce toate celelalte layere proportional si creste layerul ars
    // ================================================================
    void PaintBurned(int x, int z, float burnAmount)
    {
        int numLayers = _alphamap.GetLength(2);

        // Calculam cat din celelalte layere ramane
        float remaining = 1f - burnAmount;

        // Suma curenta a celorlalte layere (fara cel ars)
        float otherSum = 0f;
        for (int l = 0; l < numLayers; l++)
            if (l != _burnedLayerIndex)
                otherSum += _alphamap[z, x, l];

        // Redistributim
        if (otherSum > 0f)
        {
            for (int l = 0; l < numLayers; l++)
            {
                if (l == _burnedLayerIndex)
                    _alphamap[z, x, l] = burnAmount;
                else
                    _alphamap[z, x, l] = (_alphamap[z, x, l] / otherSum) * remaining;
            }
        }
        else
        {
            _alphamap[z, x, _burnedLayerIndex] = 1f;
        }
    }

    // ================================================================
    // RESETEAZA TERENUL LA STAREA INITIALA
    // ================================================================
    public void ResetBurnedTerrain()
    {
        if (!_initialized) return;

        // Setam layerul ars la 0 peste tot
        for (int z = 0; z < _alphaRes; z++)
            for (int x = 0; x < _alphaRes; x++)
                _alphamap[z, x, _burnedLayerIndex] = 0f;

        // Refacem distributia initiala a celorlalte layere
        int numLayers = _alphamap.GetLength(2);
        for (int z = 0; z < _alphaRes; z++)
        {
            for (int x = 0; x < _alphaRes; x++)
            {
                float sum = 0f;
                for (int l = 0; l < numLayers - 1; l++)
                    sum += _alphamap[z, x, l];

                if (sum < 0.01f)
                    _alphamap[z, x, 0] = 1f;
            }
        }

        _terrainData.SetAlphamaps(0, 0, _alphamap);
        Debug.Log("[BurnedTerrainPainter] Teren resetat.");
    }
}