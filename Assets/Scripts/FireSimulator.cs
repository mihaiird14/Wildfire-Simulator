using UnityEngine;

public class FireSimulator : MonoBehaviour
{
    public TerrainGenerator terrainGen;
    public GridRenderer gridRenderer;

    [Header("Parametrii foc")]
    public float windX = 1f;
    public float windZ = 0f;
    public float windStrength = 0.3f;
    public float simSpeed = 0.2f;
    public float globalMoisture = 0.1f;
    private CellState[,] _states;
    public CellState[,] States => _states;
    private float[,] _burnTimer;
    private int _res;
    private float _timer;
    private bool _running;

    void Start()
    {
        StartCoroutine(InitAfterTerrain());
    }

    System.Collections.IEnumerator InitAfterTerrain()
    {
        yield return new WaitForSeconds(2f);
        _res = terrainGen.vegetationGrid.GetLength(0);
        _states = new CellState[_res, _res];
        _burnTimer = new float[_res, _res];
        _running = false;
        Debug.Log("[FireSimulator] Gata! Click pe harta ca sa pornesti focul.");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryIgnite();

        if (!_running) return;

        _timer += Time.deltaTime;
        if (_timer >= simSpeed)
        {
            _timer = 0f;
            SimulationTick();
            gridRenderer.DrawGrid(_states);
        }
    }

    void TryIgnite()
    {
        if (_states == null) return;

        Camera cam = null;
        foreach (Camera c in Camera.allCameras)
            if (c.gameObject.activeSelf) { cam = c; break; }
        if (cam == null) return;

        if (!cam.orthographic) return;

        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = cam.nearClipPlane;
        Vector3 worldPos = cam.ScreenToWorldPoint(mouseScreen);

        float terrainOriginX = terrainGen.transform.position.x;
        float terrainOriginZ = terrainGen.transform.position.z;

        float fx = (worldPos.x - terrainOriginX) / terrainGen.terrainWidth;
        float fz = (worldPos.z - terrainOriginZ) / terrainGen.terrainLength;

        if (fx < 0 || fx > 1 || fz < 0 || fz > 1) return;

        int gx = Mathf.Clamp(Mathf.RoundToInt(fx * (_res - 1)), 0, _res - 1);
        int gz = Mathf.Clamp(Mathf.RoundToInt(fz * (_res - 1)), 0, _res - 1);

        if (_states[gz, gx] == CellState.Unburned)
        {
            _states[gz, gx] = CellState.Burning;
            _running = true;
            Debug.Log("[FireSimulator] FOC PORNIT la (" + gx + ", " + gz + ")");
        }
    }

    void SimulationTick()
    {
        CellState[,] next = (CellState[,])_states.Clone();

        for (int z = 0; z < _res; z++)
        {
            for (int x = 0; x < _res; x++)
            {
                if (_states[z, x] != CellState.Burning) continue;

                VegetationData vd = VegetationData.Get(terrainGen.vegetationGrid[z, x]);
                _burnTimer[z, x] += simSpeed;

                if (_burnTimer[z, x] >= vd.burnDuration)
                    next[z, x] = CellState.Burned;

                float myWx = terrainGen.transform.position.x
                           + (float)x / (_res - 1) * terrainGen.terrainWidth;
                float myWz = terrainGen.transform.position.z
                           + (float)z / (_res - 1) * terrainGen.terrainLength;
                float myHeight = terrainGen.GetNormalizedHeight(new Vector3(myWx, 0, myWz));

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;

                        int nx = x + dx;
                        int nz = z + dz;
                        if (nx < 0 || nx >= _res || nz < 0 || nz >= _res) continue;
                        if (_states[nz, nx] != CellState.Unburned) continue;

                        VegetationData nvd = VegetationData.Get(
                            terrainGen.vegetationGrid[nz, nx]);

                        float windFactor = 1f + windStrength *
                            (dx * windX + dz * windZ) /
                            Mathf.Sqrt(dx * dx + dz * dz + 0.001f);

                        float nWx = terrainGen.transform.position.x
                                  + (float)nx / (_res - 1) * terrainGen.terrainWidth;
                        float nWz = terrainGen.transform.position.z
                                  + (float)nz / (_res - 1) * terrainGen.terrainLength;
                        float neighborHeight = terrainGen.GetNormalizedHeight(
                            new Vector3(nWx, 0, nWz));

                        float heightDiff = neighborHeight - myHeight;
                        float slopeFactor = Mathf.Clamp(1f + heightDiff * 5f, 0.3f, 2.5f);

                        float moistureFactor = 1f - (globalMoisture * 0.8f);

                        float prob = nvd.ignitionChance
                                   * nvd.spreadMultiplier
                                   * windFactor
                                   * slopeFactor
                                   * moistureFactor
                                   * simSpeed;
                        if (Random.value < prob)
                            next[nz, nx] = CellState.Burning;
                    }
                }
            }
        }

        _states = next;
    }

    public void ResetFire()
    {
        _states = new CellState[_res, _res];
        _burnTimer = new float[_res, _res];
        _running = false;
        gridRenderer.DrawGrid(_states);
        Debug.Log("[FireSimulator] Reset.");
    }
}

public enum CellState { Unburned, Burning, Burned }