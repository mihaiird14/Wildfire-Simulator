using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FireParticleManager : MonoBehaviour
{
    public FireSimulator fireSimulator;
    public TerrainGenerator terrainGen;

    [Header("Setari")]
    public float updateInterval = 0.4f;
    public int maxActiveFires = 60;

    private Dictionary<Vector2Int, GameObject> _fireObjects
        = new Dictionary<Vector2Int, GameObject>();

    private float _timer = 0f;

    void Update()
    {
        if (fireSimulator == null || !fireSimulator.enabled) return;
        if (fireSimulator.States == null) return;

        _timer += Time.deltaTime;
        if (_timer < updateInterval) return;
        _timer = 0f;

        SyncParticles();
    }

    void SyncParticles()
    {
        if (fireSimulator == null || fireSimulator.States == null) return;

        int res = fireSimulator.States.GetLength(0);

        List<Vector2Int> allBurning = new List<Vector2Int>();
        List<Vector2Int> toExtinguish = new List<Vector2Int>();

        // 1. Verificăm starea fiecărei celule
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                CellState state = fireSimulator.States[z, x];
                Vector2Int cell = new Vector2Int(x, z);

                if (state == CellState.Burning)
                {
                    // Adăugăm la lista de focuri active
                    allBurning.Add(cell);
                }
                else if (_fireObjects.ContainsKey(cell))
                {
                    // Dacă celula NU mai arde (ex: e Burned) dar are un obiect 3D, trebuie stins
                    toExtinguish.Add(cell);
                }
            }
        }

        // 2. Stingem focurile care și-au terminat ciclul
        foreach (var cell in toExtinguish)
        {
            StartCoroutine(ExtinguishFire(cell));
        }

        // 3. Spawnăm foc pentru TOATE celulele care ard în simulare (FĂRĂ LIMITE)
        foreach (var cell in allBurning)
        {
            if (!_fireObjects.ContainsKey(cell))
            {
                SpawnFire(cell, cell.x, cell.y, res);
            }
        }
    }

    Vector3 GridToWorld(int gx, int gz, int res)
    {
        Terrain terrain = Terrain.activeTerrain;
        float originX = terrain.transform.position.x;
        float originZ = terrain.transform.position.z;
        float fx = (float)gx / (res - 1);
        float fz = (float)gz / (res - 1);
        float wx = originX + fx * terrainGen.terrainWidth;
        float wz = originZ + fz * terrainGen.terrainLength;
        float wy = terrain.SampleHeight(new Vector3(wx, 0, wz))
                        + terrain.transform.position.y;
        return new Vector3(wx, wy, wz);
    }

    void SpawnFire(Vector2Int cell, int gx, int gz, int res)
    {
        float cellSize = terrainGen.terrainWidth / (float)(res - 1);
        Vector3 pos = GridToWorld(gx, gz, res);

        GameObject fireObj = new GameObject("Fire_" + gx + "_" + gz);
        fireObj.transform.position = pos;

        // ── FOC ──────────────────────────────────────────────────
        GameObject fireGO = new GameObject("Fire");
        fireGO.transform.parent = fireObj.transform;
        fireGO.transform.localPosition = Vector3.zero;

        ParticleSystem ps = fireGO.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(cellSize * 0.6f, cellSize * 1.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
                                 new Color(1f, 0.4f, 0.0f, 0.95f),
                                 new Color(1f, 0.85f, 0.0f, 0.85f));
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.2f;

        var fireEmission = ps.emission;
        fireEmission.rateOverTime = 30f;

        var fireShape = ps.shape;
        fireShape.enabled = true;
        fireShape.shapeType = ParticleSystemShapeType.Circle;
        fireShape.radius = cellSize * 0.4f;

        var fireCol = ps.colorOverLifetime;
        fireCol.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.2f), 0.0f),
                new GradientColorKey(new Color(1f, 0.35f, 0.0f), 0.5f),
                new GradientColorKey(new Color(0.5f, 0.1f, 0.0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.95f, 0.1f),
                new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0.0f,  1.0f)
            }
        );
        fireCol.color = new ParticleSystem.MinMaxGradient(grad);

        var fireSizeLife = ps.sizeOverLifetime;
        fireSizeLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f); sizeCurve.AddKey(0.3f, 1.0f); sizeCurve.AddKey(1f, 0.1f);
        fireSizeLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var fireNoise = ps.noise;
        fireNoise.enabled = true;
        fireNoise.strength = 0.5f;
        fireNoise.frequency = 0.9f;
        fireNoise.scrollSpeed = 0.6f;

        var fireR = fireGO.GetComponent<ParticleSystemRenderer>();
        fireR.material = CreateFireMaterial();
        fireR.renderMode = ParticleSystemRenderMode.Billboard;

        // ── FUM ───────────────────────────────────────────────────
        // ── FUM ───────────────────────────────────────────────────
        GameObject smokeGO = new GameObject("Smoke");
        smokeGO.transform.parent = fireObj.transform;
        smokeGO.transform.localPosition = new Vector3(0, cellSize * 0.8f, 0);

        ParticleSystem sm = smokeGO.AddComponent<ParticleSystem>();

        var smMain = sm.main;
        smMain.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); // Se disipă puțin mai repede
        smMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);  // Se ridică puțin mai repede
        smMain.startSize = new ParticleSystem.MinMaxCurve(cellSize * 1.5f, cellSize * 3.5f);

        // Alpha mult mai mic (0.3 - 0.4 în loc de 0.85) pentru a nu fi un "zid" negru
        smMain.startColor = new ParticleSystem.MinMaxGradient(
                                    new Color(0.2f, 0.2f, 0.2f, 0.4f),
                                    new Color(0.4f, 0.4f, 0.4f, 0.3f));

        smMain.maxParticles = 15; // Redus de la 40 (crucial pentru performanță)
        smMain.simulationSpace = ParticleSystemSimulationSpace.World;
        smMain.gravityModifier = -0.08f; // Trage fumul natural în sus

        var smEmission = sm.emission;
        smEmission.rateOverTime = 3f; // Redus drastic de la 10. Se compensează cu restul focurilor vecine

        var smShape = sm.shape;
        smShape.enabled = true;
        smShape.shapeType = ParticleSystemShapeType.Circle;
        smShape.radius = cellSize * 0.6f; // Răspândit pe o arie puțin mai mare

        var smSizeLife = sm.sizeOverLifetime;
        smSizeLife.enabled = true;
        AnimationCurve smSizeCurve = new AnimationCurve();
        smSizeCurve.AddKey(0f, 0.2f); smSizeCurve.AddKey(0.3f, 1.0f); smSizeCurve.AddKey(1f, 2.5f);
        smSizeLife.size = new ParticleSystem.MinMaxCurve(1f, smSizeCurve);

        var smColLife = sm.colorOverLifetime;
        smColLife.enabled = true;
        Gradient smGrad = new Gradient();
        smGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.15f, 0.15f, 0.15f), 0.0f),
                new GradientColorKey(new Color(0.4f,  0.4f,  0.4f),  0.5f),
                new GradientColorKey(new Color(0.7f,  0.7f,  0.7f),  1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f,  0.0f),
                new GradientAlphaKey(0.35f, 0.2f), // Vârful de opacitate e redus de la 0.85 la 0.35
                new GradientAlphaKey(0.15f, 0.6f),
                new GradientAlphaKey(0.0f,  1.0f)
            }
        );
        smColLife.color = new ParticleSystem.MinMaxGradient(smGrad);

        var smNoise = sm.noise;
        smNoise.enabled = true;
        smNoise.strength = 0.5f;     // Mai puțin agresiv
        smNoise.frequency = 0.15f;
        smNoise.scrollSpeed = 0.1f;

        var smR = smokeGO.GetComponent<ParticleSystemRenderer>();
        smR.material = CreateSmokeMaterial();
        smR.renderMode = ParticleSystemRenderMode.Billboard;
        smR.renderMode = ParticleSystemRenderMode.Billboard;

        // ── SCANTEI ───────────────────────────────────────────────
        GameObject sparkGO = new GameObject("Sparks");
        sparkGO.transform.parent = fireObj.transform;
        sparkGO.transform.localPosition = Vector3.zero;

        ParticleSystem sp = sparkGO.AddComponent<ParticleSystem>();

        var spMain = sp.main;
        spMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        spMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        spMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        spMain.startColor = new ParticleSystem.MinMaxGradient(
                                    new Color(1f, 0.95f, 0.3f),
                                    new Color(1f, 0.5f, 0.1f));
        spMain.maxParticles = 30;
        spMain.simulationSpace = ParticleSystemSimulationSpace.World;
        spMain.gravityModifier = 0.4f;

        var spEmission = sp.emission;
        spEmission.rateOverTime = 10f;

        var spShape = sp.shape;
        spShape.enabled = true;
        spShape.shapeType = ParticleSystemShapeType.Cone;
        spShape.angle = 40f;
        spShape.radius = cellSize * 0.2f;

        var spColLife = sp.colorOverLifetime;
        spColLife.enabled = true;
        Gradient spGrad = new Gradient();
        spGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.6f),   0f),
                new GradientColorKey(new Color(1f, 0.4f, 0f),   0.5f),
                new GradientColorKey(new Color(0.4f, 0.1f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        spColLife.color = new ParticleSystem.MinMaxGradient(spGrad);

        var spR = sparkGO.GetComponent<ParticleSystemRenderer>();
        spR.material = CreateFireMaterial();
        spR.renderMode = ParticleSystemRenderMode.Stretch;
        spR.velocityScale = 0.25f;

        // ── LUMINA ────────────────────────────────────────────────
        GameObject lightGO = new GameObject("FireLight");
        lightGO.transform.parent = fireObj.transform;
        lightGO.transform.localPosition = new Vector3(0, cellSize, 0);

        Light fl = lightGO.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(1f, 0.45f, 0.1f);
        fl.intensity = 2f;
        fl.range = cellSize * 5f;
        lightGO.AddComponent<FireLightPulse>();

        _fireObjects[cell] = fireObj;
    }

    IEnumerator ExtinguishFire(Vector2Int cell)
    {
        if (!_fireObjects.ContainsKey(cell)) yield break;

        GameObject obj = _fireObjects[cell];
        _fireObjects.Remove(cell);
        if (obj == null) yield break;

        foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>())
        {
            var em = ps.emission;
            em.enabled = false;
            ps.Clear();
        }

        Light lt = obj.GetComponentInChildren<Light>();
        if (lt != null) lt.enabled = false;

        Destroy(obj, 0.5f);
    }

    public void ResetAllFire()
    {
        foreach (var kvp in _fireObjects)
            if (kvp.Value != null) Destroy(kvp.Value);
        _fireObjects.Clear();
    }

    Material CreateFireMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        mat.color = new Color(1f, 0.45f, 0.1f, 0.9f);
        mat.renderQueue = 3000;
        return mat;
    }

    Material CreateSmokeMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        mat.color = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        mat.renderQueue = 3001;
        return mat;
    }
}

public class FireLightPulse : MonoBehaviour
{
    private Light _light;
    private float _baseIntensity;
    private float _offset;

    void Start()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light != null ? _light.intensity : 2f;
        _offset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (_light == null) return;
        float pulse = Mathf.Sin(Time.time * 3.2f + _offset) * 0.4f
                    + Mathf.Sin(Time.time * 7.7f + _offset) * 0.2f
                    + Mathf.Sin(Time.time * 13f + _offset) * 0.1f;
        _light.intensity = Mathf.Max(0.1f, _baseIntensity + pulse);
    }
}