using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// ================================================================
// AnimalSpawner.cs
// ================================================================
// Spawneaza animale de tipuri diferite pe teren, pe pozitii
// aleatore valide (pe NavMesh, nu pe stanca).
// Afiseaza statistici despre starea animalelor in timp real.
// ================================================================

public class AnimalSpawner : MonoBehaviour
{
    public FireSimulator fireSimulator;
    public TerrainGenerator terrainGen;

    [Header("Configuratie spawn")]
    public int totalAnimals = 8;

    // Prefab-urile pentru fiecare tip — lasa goale si vor fi create automat
    // ca primitive colorate diferit
    [Header("Prefaburi (optional - lasa gol pentru primitive)")]
    public GameObject deerPrefab;
    public GameObject boarPrefab;
    public GameObject rabbitPrefab;
    public GameObject wolfPrefab;
    public GameObject foxPrefab;

    // Lista cu toate animalele din scena
    private List<AnimalAI> _animals = new List<AnimalAI>();

    // Culorile pentru fiecare tip de animal (primitive)
    private Color[] _animalColors = {
        new Color(0.6f, 0.4f, 0.2f),  // Cerb - maro
        new Color(0.3f, 0.2f, 0.1f),  // Mistret - maro inchis
        new Color(0.9f, 0.8f, 0.7f),  // Iepure - crem
        new Color(0.2f, 0.2f, 0.2f),  // Lup - gri inchis
        new Color(0.8f, 0.4f, 0.1f)   // Vulpe - portocaliu
    };

    void Start()
    {
        // Asteptam ca terenul si NavMesh-ul sa fie gata
        Invoke("SpawnAnimals", 2.5f);
    }

    void SpawnAnimals()
    {
        // Distributie de animale: 2 cerbi, 2 mistreti, 2 iepuri, 1 lup, 1 vulpe
        AnimalType[] types = {
            AnimalType.Deer,   AnimalType.Deer,
            AnimalType.Boar,   AnimalType.Boar,
            AnimalType.Rabbit, AnimalType.Rabbit,
            AnimalType.Wolf,
            AnimalType.Fox
        };

        int spawned = 0;
        int attempts = 0;

        while (spawned < Mathf.Min(totalAnimals, types.Length) && attempts < 200)
        {
            attempts++;

            // Pozitie aleatoare pe teren
            float rx = terrainGen.transform.position.x
                     + Random.Range(10f, terrainGen.terrainWidth - 10f);
            float rz = terrainGen.transform.position.z
                     + Random.Range(10f, terrainGen.terrainLength - 10f);
            float ry = Terrain.activeTerrain.SampleHeight(new Vector3(rx, 0, rz))
                     + Terrain.activeTerrain.transform.position.y;

            Vector3 spawnPos = new Vector3(rx, ry, rz);

            // Verificam ca pozitia e pe NavMesh
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(spawnPos, out hit, 3f, NavMesh.AllAreas))
                continue;

            // Verificam ca nu e pe stanca (vegetatie Rock nu arde si e zona inaccesibila)
            Vector2Int gridPos = terrainGen.WorldToGrid(hit.position);
            if (terrainGen.vegetationGrid[gridPos.y, gridPos.x] == VegetationType.Rock)
                continue;

            // Cream animalul
            AnimalType type = types[spawned];
            GameObject animal = CreateAnimalObject(type, hit.position);

            // Adaugam componenta AI
            AnimalAI ai = animal.AddComponent<AnimalAI>();
            ai.animalType = type;
            ai.fireSimulator = fireSimulator;
            ai.terrainGen = terrainGen;

            _animals.Add(ai);
            spawned++;
        }

        Debug.Log("[AnimalSpawner] Spawnat " + spawned + " animale.");
    }

    // ================================================================
    // CREAM OBIECTUL 3D AL ANIMALULUI
    // Daca nu avem prefab, cream o primitiva colorata
    // ================================================================
    GameObject CreateAnimalObject(AnimalType type, Vector3 position)
    {
        GameObject prefab = type switch
        {
            AnimalType.Deer => deerPrefab,
            AnimalType.Boar => boarPrefab,
            AnimalType.Rabbit => rabbitPrefab,
            AnimalType.Wolf => wolfPrefab,
            AnimalType.Fox => foxPrefab,
            _ => null
        };

        GameObject animal;

        if (prefab != null)
        {
            animal = Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            // Cream o primitiva simpla ca placeholder
            animal = new GameObject(type.ToString());
            animal.transform.position = position;

            // Corp principal
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.parent = animal.transform;
            body.transform.localPosition = new Vector3(0, 0.5f, 0);
            body.transform.localScale = GetAnimalScale(type);

            // Culoare specifica tipului
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = _animalColors[(int)type];
            body.GetComponent<Renderer>().material = mat;

            // Stergem coliderul din corp (agentul NavMesh se ocupa de coliziuni)
            Destroy(body.GetComponent<Collider>());

            // Cap mic
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.parent = animal.transform;
            head.transform.localPosition = new Vector3(0, 1.2f, 0.3f);
            head.transform.localScale = Vector3.one * 0.4f;
            head.GetComponent<Renderer>().material = mat;
            Destroy(head.GetComponent<Collider>());
        }

        animal.transform.parent = this.transform;
        return animal;
    }

    Vector3 GetAnimalScale(AnimalType type)
    {
        return type switch
        {
            AnimalType.Deer => new Vector3(0.6f, 0.8f, 0.6f),
            AnimalType.Boar => new Vector3(0.7f, 0.5f, 0.7f),
            AnimalType.Rabbit => new Vector3(0.25f, 0.3f, 0.25f),
            AnimalType.Wolf => new Vector3(0.55f, 0.6f, 0.55f),
            AnimalType.Fox => new Vector3(0.35f, 0.45f, 0.35f),
            _ => Vector3.one * 0.5f
        };
    }

    // ================================================================
    // UI — statistici despre animale in timp real
    // ================================================================
    void OnGUI()
    {
        if (_animals.Count == 0) return;

        float x = 10f;
        float y = Screen.height - 180f;
        float w = 200f;
        float h = 170f;

        // Fundal
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;
        title.fontSize = 13;

        GUIStyle lbl = new GUIStyle(GUI.skin.label);
        lbl.normal.textColor = Color.white;
        lbl.fontSize = 12;

        GUI.Label(new Rect(x + 8, y + 8, w - 16, 20), "Animale", title);
        y += 28f;

        // Numaram animalele per stare
        int fleeing = 0, avoiding = 0, wandering = 0, resting = 0, dead = 0;
        foreach (var a in _animals)
        {
            if (a == null) continue;
            switch (a.CurrentState)
            {
                case AnimalState.Fleeing: fleeing++; break;
                case AnimalState.Avoiding: avoiding++; break;
                case AnimalState.Wandering: wandering++; break;
                case AnimalState.Resting: resting++; break;
                case AnimalState.Dead: dead++; break;
            }
        }

        GUI.color = Color.red;
        GUI.Label(new Rect(x + 8, y, w - 16, 20), "Fuga:     " + fleeing, lbl);
        GUI.color = Color.yellow;
        GUI.Label(new Rect(x + 8, y + 20, w - 16, 20), "Evita:    " + avoiding, lbl);
        GUI.color = Color.green;
        GUI.Label(new Rect(x + 8, y + 40, w - 16, 20), "Ratacire: " + wandering, lbl);
        GUI.color = Color.cyan;
        GUI.Label(new Rect(x + 8, y + 60, w - 16, 20), "Odihna:   " + resting, lbl);
        GUI.color = new Color(0.5f, 0f, 0f);
        GUI.Label(new Rect(x + 8, y + 80, w - 16, 20), "Morti:    " + dead, lbl);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 8, y + 100, w - 16, 20), "Total:    " + _animals.Count, lbl);
    }
}