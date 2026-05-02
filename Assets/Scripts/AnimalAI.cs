using UnityEngine;
using UnityEngine.AI;

// ================================================================
// AnimalAI.cs
// ================================================================
// Implementeaza un Decision Tree pentru comportamentul animalelor.
//
// Arborele de decizii evaluat la fiecare frame:
//
//   Animal mort?
//   └── DA → ramane mort
//   └── NU → Foc foarte aproape (< criticalRadius)?
//             ├── DA → FUGA ACTIVA
//             └── NU → Foc detectat (< detectionRadius)?
//                       ├── DA → EVITARE preventiva
//                       └── NU → Sunt obosit?
//                                 ├── DA → ODIHNA
//                                 └── NU → RATACIRE aleatoare
// ================================================================

[RequireComponent(typeof(NavMeshAgent))]
public class AnimalAI : MonoBehaviour
{
    [Header("Tip animal")]
    public AnimalType animalType = AnimalType.Deer;

    [Header("Referinte")]
    public FireSimulator fireSimulator;
    public TerrainGenerator terrainGen;

    // Starea curenta si statisticile animalului
    private AnimalStats _stats;
    private AnimalState _state = AnimalState.Wandering;
    private NavMeshAgent _agent;

    // Stamina si odihna
    private float _stamina;
    private float _restTimer;

    // Wandering
    private float _wanderTimer;
    private Vector3 _wanderTarget;

    // Zig-zag pentru fuga
    private float _zigzagTimer;
    private float _zigzagAngle;

    // Label vizual deasupra animalului
    private TextMesh _label;

    // ================================================================
    // INITIALIZARE
    // ================================================================
    void Start()
    {
        _stats = AnimalStats.Get(animalType);
        _agent = GetComponent<NavMeshAgent>();
        _stamina = _stats.staminaMax;

        _agent.speed = _stats.moveSpeed;
        _agent.acceleration = 20f;
        _agent.stoppingDistance = 0.5f;

        // Cream label vizual deasupra animalului
        CreateLabel();

        // Prima destinatie aleatoare
        SetWanderDestination();
    }

    // ================================================================
    // UPDATE — evalueaza Decision Tree la fiecare frame
    // ================================================================
    void Update()
    {
        if (_state == AnimalState.Dead) return;

        // ── DECISION TREE ─────────────────────────────────────────
        // Nodul 1: verifica proximitatea focului
        float distToFire = GetDistanceToNearestFire();

        if (distToFire < _stats.criticalRadius)
        {
            // FOC FOARTE APROAPE — fuga activa
            EnterState(AnimalState.Fleeing);
        }
        else if (distToFire < _stats.detectionRadius)
        {
            // FOC DETECTAT dar nu critic — evitare preventiva
            EnterState(AnimalState.Avoiding);
        }
        else if (_stamina <= 0f)
        {
            // NU e foc dar sunt obosit — odihna
            EnterState(AnimalState.Resting);
        }
        else
        {
            // Totul e ok — ratacire normala
            EnterState(AnimalState.Wandering);
        }

        // ── EXECUTAM COMPORTAMENTUL STARII CURENTE ────────────────
        switch (_state)
        {
            case AnimalState.Fleeing: ExecuteFlee(distToFire); break;
            case AnimalState.Avoiding: ExecuteAvoid(distToFire); break;
            case AnimalState.Resting: ExecuteRest(); break;
            case AnimalState.Wandering: ExecuteWander(); break;
        }

        // ── VERIFICAM DACA ANIMALUL E PRINS DE FOC ────────────────
        CheckIfBurned();

        // Actualizam label-ul
        UpdateLabel();
    }

    // ================================================================
    // TRANZITII INTRE STARI
    // ================================================================
    void EnterState(AnimalState newState)
    {
        if (_state == newState) return;

        // Logica la intrarea intr-o stare noua
        switch (newState)
        {
            case AnimalState.Fleeing:
                _agent.speed = _stats.fleeSpeed;
                break;
            case AnimalState.Avoiding:
                _agent.speed = _stats.moveSpeed * 1.3f;
                break;
            case AnimalState.Resting:
                _agent.speed = 0f;
                _agent.ResetPath();
                _restTimer = _stats.restTime;
                break;
            case AnimalState.Wandering:
                _agent.speed = _stats.moveSpeed;
                break;
        }

        _state = newState;
    }

    // ================================================================
    // COMPORTAMENT: FUGA ACTIVA
    // Animal fuge cat mai departe de foc, cu zig-zag bazat pe tip
    // ================================================================
    void ExecuteFlee(float distToFire)
    {
        // Consuma stamina la fuga
        _stamina -= Time.deltaTime * 2f;
        _stamina = Mathf.Max(0f, _stamina);

        // Directia de baza = departe de foc
        Vector3 firePos = GetNearestFirePosition();
        Vector3 awayDir = (transform.position - firePos).normalized;

        // Adaugam zig-zag bazat pe statisticile animalului
        _zigzagTimer += Time.deltaTime * 3f;
        float zigzag = Mathf.Sin(_zigzagTimer) * _stats.zigzagIntensity;
        Vector3 sideDir = Vector3.Cross(awayDir, Vector3.up);
        Vector3 fleeDir = (awayDir + sideDir * zigzag).normalized;

        // Destinatia = pozitia curenta + directia de fuga * distanta mare
        Vector3 target = transform.position + fleeDir * 20f;
        target = ClampToTerrain(target);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 10f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    // ================================================================
    // COMPORTAMENT: EVITARE PREVENTIVA
    // Animal se indeparteaza incet de foc inainte sa fie periculos
    // ================================================================
    void ExecuteAvoid(float distToFire)
    {
        Vector3 firePos = GetNearestFirePosition();
        Vector3 awayDir = (transform.position - firePos).normalized;
        Vector3 target = transform.position + awayDir * 10f;
        target = ClampToTerrain(target);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 8f, NavMesh.AllAreas))
        {
            if (!_agent.hasPath || Vector3.Distance(_agent.destination, hit.position) > 3f)
                _agent.SetDestination(hit.position);
        }
    }

    // ================================================================
    // COMPORTAMENT: ODIHNA
    // Animal sta pe loc si isi reface stamina
    // ================================================================
    void ExecuteRest()
    {
        _restTimer -= Time.deltaTime;
        _stamina += Time.deltaTime * (_stats.staminaMax / _stats.restTime);
        _stamina = Mathf.Min(_stamina, _stats.staminaMax);

        if (_restTimer <= 0f)
        {
            // Terminat odihna — revenim la ratacire
            _stamina = _stats.staminaMax;
            EnterState(AnimalState.Wandering);
        }
    }

    // ================================================================
    // COMPORTAMENT: RATACIRE
    // Animal se misca aleator pe teren cand nu e pericol
    // ================================================================
    void ExecuteWander()
    {
        _stamina += Time.deltaTime * 0.5f;
        _stamina = Mathf.Min(_stamina, _stats.staminaMax);

        _wanderTimer -= Time.deltaTime;

        // La fiecare 3-6 secunde alegem o noua destinatie aleatoare
        if (_wanderTimer <= 0f || !_agent.hasPath ||
            Vector3.Distance(transform.position, _agent.destination) < 1f)
        {
            SetWanderDestination();
            _wanderTimer = Random.Range(3f, 6f);
        }
    }

    // ================================================================
    // VERIFICAM DACA ANIMALUL E PRINS DE FOC
    // Daca celula pe care sta animalul arde, moare
    // ================================================================
    void CheckIfBurned()
    {
        if (fireSimulator == null || fireSimulator.States == null) return;

        // Convertim pozitia animalului in coordonate grid
        Vector2Int gridPos = terrainGen.WorldToGrid(transform.position);
        int res = fireSimulator.States.GetLength(0);

        gridPos.x = Mathf.Clamp(gridPos.x, 0, res - 1);
        gridPos.y = Mathf.Clamp(gridPos.y, 0, res - 1);

        CellState cell = fireSimulator.States[gridPos.y, gridPos.x];

        if (cell == CellState.Burning || cell == CellState.Burned)
        {
            Die();
        }
    }

    // ================================================================
    // MOARTE
    // ================================================================
    void Die()
    {
        _state = AnimalState.Dead;
        _agent.ResetPath();
        _agent.enabled = false;

        // Culoarea devine rosie ca sa se vada ca a murit
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0.5f, 0f, 0f);

        // Rotim animalul pe o parte (culcat)
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 90f);

        Debug.Log("[AnimalAI] " + _stats.displayName + " a murit!");
    }

    // ================================================================
    // UTILITARE — gasim focul cel mai aproape
    // ================================================================
    float GetDistanceToNearestFire()
    {
        if (fireSimulator == null || fireSimulator.States == null)
            return float.MaxValue;

        int res = fireSimulator.States.GetLength(0);
        float minDist = float.MaxValue;

        // Cautam in zona din jurul animalului (nu tot gridul — prea lent)
        Vector2Int myGrid = terrainGen.WorldToGrid(transform.position);
        int searchRadius = Mathf.RoundToInt(_stats.detectionRadius /
                            (terrainGen.terrainWidth / (float)(res - 1)));

        int x0 = Mathf.Max(0, myGrid.x - searchRadius);
        int x1 = Mathf.Min(res - 1, myGrid.x + searchRadius);
        int z0 = Mathf.Max(0, myGrid.y - searchRadius);
        int z1 = Mathf.Min(res - 1, myGrid.y + searchRadius);

        for (int z = z0; z <= z1; z++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (fireSimulator.States[z, x] == CellState.Burning)
                {
                    // Convertim celula in pozitie world
                    float wx = (float)x / (res - 1) * terrainGen.terrainWidth
                             + terrainGen.transform.position.x;
                    float wz = (float)z / (res - 1) * terrainGen.terrainLength
                             + terrainGen.transform.position.z;

                    float dist = Vector3.Distance(transform.position,
                                                  new Vector3(wx, transform.position.y, wz));
                    if (dist < minDist) minDist = dist;
                }
            }
        }

        return minDist;
    }

    Vector3 GetNearestFirePosition()
    {
        if (fireSimulator == null || fireSimulator.States == null)
            return transform.position;

        int res = fireSimulator.States.GetLength(0);
        float minDist = float.MaxValue;
        Vector3 nearest = transform.position;

        Vector2Int myGrid = terrainGen.WorldToGrid(transform.position);
        int searchRadius = Mathf.RoundToInt(_stats.detectionRadius /
                              (terrainGen.terrainWidth / (float)(res - 1)));

        int x0 = Mathf.Max(0, myGrid.x - searchRadius);
        int x1 = Mathf.Min(res - 1, myGrid.x + searchRadius);
        int z0 = Mathf.Max(0, myGrid.y - searchRadius);
        int z1 = Mathf.Min(res - 1, myGrid.y + searchRadius);

        for (int z = z0; z <= z1; z++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (fireSimulator.States[z, x] == CellState.Burning)
                {
                    float wx = (float)x / (res - 1) * terrainGen.terrainWidth
                             + terrainGen.transform.position.x;
                    float wz = (float)z / (res - 1) * terrainGen.terrainLength
                             + terrainGen.transform.position.z;

                    Vector3 pos = new Vector3(wx, transform.position.y, wz);
                    float dist = Vector3.Distance(transform.position, pos);
                    if (dist < minDist) { minDist = dist; nearest = pos; }
                }
            }
        }

        return nearest;
    }

    void SetWanderDestination()
    {
        // Alegem o pozitie aleatoare pe NavMesh in raza de 20 unitati
        Vector3 randomDir = Random.insideUnitSphere * 20f;
        randomDir += transform.position;
        randomDir.y = transform.position.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, 20f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    Vector3 ClampToTerrain(Vector3 pos)
    {
        // Tine pozitia in limitele terenului
        float ox = terrainGen.transform.position.x;
        float oz = terrainGen.transform.position.z;
        pos.x = Mathf.Clamp(pos.x, ox + 5f, ox + terrainGen.terrainWidth - 5f);
        pos.z = Mathf.Clamp(pos.z, oz + 5f, oz + terrainGen.terrainLength - 5f);
        return pos;
    }

    // ================================================================
    // LABEL VIZUAL — arata starea si tipul deasupra animalului
    // ================================================================
    void CreateLabel()
    {
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.parent = transform;
        labelGO.transform.localPosition = new Vector3(0, 2.5f, 0);

        _label = labelGO.AddComponent<TextMesh>();
        _label.fontSize = 24;
        _label.alignment = TextAlignment.Center;
        _label.anchor = TextAnchor.LowerCenter;
        _label.color = Color.white;

        // Adaugam MeshRenderer ca sa se vada
        labelGO.GetComponent<MeshRenderer>().sortingOrder = 10;
    }

    void UpdateLabel()
    {
        if (_label == null) return;

        // Label-ul priveste mereu spre camera
        if (Camera.main != null)
            _label.transform.rotation = Camera.main.transform.rotation;

        string stateIcon = _state switch
        {
            AnimalState.Fleeing => "🔴 FUGA",
            AnimalState.Avoiding => "🟡 EVITA",
            AnimalState.Resting => "💤 ODIHNA",
            AnimalState.Wandering => "🟢 RATACIRE",
            AnimalState.Dead => "💀 MORT",
            _ => ""
        };

        _label.text = _stats.displayName + "\n" + stateIcon;
        _label.color = _state switch
        {
            AnimalState.Fleeing => Color.red,
            AnimalState.Avoiding => Color.yellow,
            AnimalState.Resting => Color.cyan,
            AnimalState.Dead => new Color(0.5f, 0f, 0f),
            _ => Color.green
        };
    }

    // Getter pentru stare — folosit de AnimalSpawner pentru statistici
    public AnimalState CurrentState => _state;
    public AnimalType Type => animalType;
    public string DisplayName => _stats.displayName;
}