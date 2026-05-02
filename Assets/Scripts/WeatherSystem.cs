using UnityEngine;

public class WeatherSystem : MonoBehaviour
{
    public FireSimulator fireSimulator;
    public Material rainMaterial;   // Material pentru picaturi de ploaie

    [Header("Setari ploaie")]
    public float rainHeight = 30f;   // inaltimea de la care cad picaturile
    public float rainAreaSize = 120f;  // zona acoperita de ploaie
    public int maxRainParticles = 2000; // numarul maxim de particule

    private GameObject _rainObject;
    private ParticleSystem _rainPS;

    void Start()
    {
        CreateRainSystem();
    }

    void Update()
    {
        if (fireSimulator == null || _rainPS == null) return;

        float moisture = fireSimulator.globalMoisture;

        // Ploaia apare progresiv cu umiditatea
        // Sub 20% = fara ploaie, peste 20% = incepe ploaia
        if (moisture < 0.2f)
        {
            // Oprim ploaia
            if (_rainPS.isPlaying)
            {
                var em = _rainPS.emission;
                em.enabled = false;
            }
            return;
        }

        // Activam ploaia
        var emission = _rainPS.emission;
        emission.enabled = true;

        // Calculam intensitatea progresiv intre 0.2 si 1.0
        float intensity = Mathf.InverseLerp(0.2f, 1.0f, moisture);

        // Numarul de particule creste cu umiditatea
        var main = _rainPS.main;
        main.maxParticles = Mathf.RoundToInt(maxRainParticles * intensity);

        // Rata de emisie creste cu umiditatea
        emission.rateOverTime = Mathf.Lerp(50f, 800f, intensity);

        // Viteza picaturilor creste cu umiditatea
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(8f, 15f, intensity),
            Mathf.Lerp(12f, 20f, intensity)
        );

        // Pozitionam sistemul de ploaie deasupra centrului terenului
        if (Terrain.activeTerrain != null)
        {
            TerrainData td = Terrain.activeTerrain.terrainData;
            float cx = Terrain.activeTerrain.transform.position.x + td.size.x / 2f;
            float cz = Terrain.activeTerrain.transform.position.z + td.size.z / 2f;
            float cy = Terrain.activeTerrain.transform.position.y
                            + Terrain.activeTerrain.SampleHeight(new Vector3(cx, 0, cz))
                            + rainHeight;

            _rainObject.transform.position = new Vector3(cx, cy, cz);
        }

        if (!_rainPS.isPlaying)
            _rainPS.Play();
    }

    void CreateRainSystem()
    {
        _rainObject = new GameObject("RainSystem");
        _rainObject.transform.parent = transform;

        _rainPS = _rainObject.AddComponent<ParticleSystem>();

        var main = _rainPS.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(
                                 new Color(0.7f, 0.8f, 1.0f, 0.4f),
                                 new Color(0.8f, 0.9f, 1.0f, 0.6f));
        main.maxParticles = maxRainParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.5f;
        main.startRotation = new ParticleSystem.MinMaxCurve(
                                 10f * Mathf.Deg2Rad, 15f * Mathf.Deg2Rad);

        // Emisie initiala oprita
        var emission = _rainPS.emission;
        emission.enabled = false;
        emission.rateOverTime = 0f;

        // Forma — cutie mare deasupra terenului
        var shape = _rainPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(rainAreaSize, 0.1f, rainAreaSize);

        // Picaturile cad drept in jos
        var vel = _rainPS.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(-1f);

        // Picaturile devin transparente la final (lovesc pamantul)
        var col = _rainPS.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.7f, 0.85f, 1f), 0f),
                new GradientColorKey(new Color(0.7f, 0.85f, 1f), 0.8f),
                new GradientColorKey(new Color(0.5f, 0.7f, 1f),  1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f,    0f),
                new GradientAlphaKey(0.55f, 0.1f),
                new GradientAlphaKey(0.5f,  0.7f),
                new GradientAlphaKey(0f,    1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Picaturile sunt alungite (Stretch) ca sa arate ca ploaie reala
        var renderer = _rainPS.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.08f;
        renderer.lengthScale = 1.5f;
        renderer.material = rainMaterial != null
                                 ? rainMaterial
                                 : CreateRainMaterial();
        renderer.sortingFudge = 5f;
    }

    Material CreateRainMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.color = new Color(0.75f, 0.88f, 1f, 0.5f);
        mat.renderQueue = 3002;
        return mat;
    }
}