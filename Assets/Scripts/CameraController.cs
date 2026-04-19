using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Distanta fata de teren")]
    public float _distance = 250f;
    public float minDistance = 5f;
    public float maxDistance = 500f;

    [Header("Unghi de vedere")]
    public float _pitch = 55f;
    public float _yaw = 45f;

    [Header("Viteze")]
    public float orbitSpeed = 150f;
    public float zoomSpeed = 50f;
    public float panSpeed = 20f;
    public float moveSpeed = 50f;

    private Vector3 _target;

    void Awake()
    {
        if (Terrain.activeTerrain != null)
        {
            TerrainData td = Terrain.activeTerrain.terrainData;
            float cx = td.size.x / 2f;
            float cz = td.size.z / 2f;
            float cy = Terrain.activeTerrain.SampleHeight(new Vector3(cx, 0, cz));
            _target = new Vector3(cx, cy, cz);
        }
        else
        {
            _target = new Vector3(100f, 0f, 100f);
        }

        UpdateCameraPosition();
    }

    void Start()
    {

        UpdateCameraPosition();
    }

    void Update()
    {
        HandleOrbit();
        HandleZoom();
        HandlePan();
        HandleWASD();
        SnapTargetToTerrain();
        UpdateCameraPosition();
    }

    void HandleOrbit()
    {
        if (Input.GetMouseButton(0))
        {
            _yaw += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
            _pitch -= Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch, 5f, 80f);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            _distance -= scroll * zoomSpeed;
            _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
        }
    }

    void HandlePan()
    {
        if (Input.GetMouseButton(1))
        {
            float pan = panSpeed * Time.deltaTime;
            Vector3 right = transform.right;
            Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
            right.y = 0f;
            forward.y = 0f;
            _target -= right * Input.GetAxis("Mouse X") * pan * _distance;
            _target -= forward * Input.GetAxis("Mouse Y") * pan * _distance;
        }
    }

    void HandleWASD()
    {
        Vector3 right = transform.right;
        Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
        right.y = 0f;
        forward.y = 0f;
        float speed = moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.W)) _target += forward * speed;
        if (Input.GetKey(KeyCode.S)) _target -= forward * speed;
        if (Input.GetKey(KeyCode.A)) _target -= right * speed;
        if (Input.GetKey(KeyCode.D)) _target += right * speed;
    }

    void SnapTargetToTerrain()
    {
        if (Terrain.activeTerrain == null) return;
        float terrainY = Terrain.activeTerrain.SampleHeight(_target)
                       + Terrain.activeTerrain.transform.position.y;
        _target.y = terrainY;
    }

    void UpdateCameraPosition()
    {
        float pitchRad = _pitch * Mathf.Deg2Rad;
        float yawRad = _yaw * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(
            Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
            Mathf.Sin(pitchRad),
            Mathf.Cos(pitchRad) * Mathf.Cos(yawRad)
        );
        transform.position = _target + dir * _distance;
        transform.LookAt(_target);
    }
}