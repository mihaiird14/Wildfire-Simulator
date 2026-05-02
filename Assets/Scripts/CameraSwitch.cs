using UnityEngine;

public class CameraSwitch : MonoBehaviour
{
    [Header("Camere")]
    public Camera mapCamera;
    public Camera groundCamera;

    [Header("Miscare la sol")]
    public float moveSpeed = 10f;
    public float sprintSpeed = 25f;
    public float rotateSpeed = 2f;

    private bool _isGroundMode = false;
    private float _yaw = 0f;
    private float _pitch = 10f;

    void Start()
    {
        SetMode(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _isGroundMode = !_isGroundMode;
            SetMode(_isGroundMode);
        }

        if (_isGroundMode)
        {
            HandleGroundMovement();
            HandleGroundRotation();
            KeepAboveTerrain();
        }
    }

    void SetMode(bool groundMode)
    {
        mapCamera.gameObject.SetActive(!groundMode);
        groundCamera.gameObject.SetActive(groundMode);

        if (groundMode)
            Debug.Log("[Camera] Modul 2 - La sol | WASD=miscare, E=sus, Q=jos, Click dreapta=rotire, Tab=harta");
        else
            Debug.Log("[Camera] Modul 1 - Harta | Tab=treci la sol");
    }

    void HandleGroundMovement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += groundCamera.transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= groundCamera.transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= groundCamera.transform.right;
        if (Input.GetKey(KeyCode.D)) move += groundCamera.transform.right;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;    // urca
        if (Input.GetKey(KeyCode.Q)) move += Vector3.down;  // coboara

        // Miscarea orizontala nu afecteaza Y (doar E/Q fac asta)
        Vector3 horizontal = new Vector3(move.x, 0f, move.z).normalized * speed * Time.deltaTime;
        Vector3 vertical = new Vector3(0f, move.y, 0f) * speed * Time.deltaTime;

        groundCamera.transform.position += horizontal + vertical;
    }

    void HandleGroundRotation()
    {
        if (Input.GetMouseButton(1))
        {
            _yaw += Input.GetAxis("Mouse X") * rotateSpeed;
            _pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
            _pitch = Mathf.Clamp(_pitch, -30f, 80f);
            groundCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }

    void KeepAboveTerrain()
    {
        if (Terrain.activeTerrain == null) return;

        Vector3 pos = groundCamera.transform.position;
        float terrainY = Terrain.activeTerrain.SampleHeight(pos)
                       + Terrain.activeTerrain.transform.position.y;

        if (pos.y < terrainY + 1.5f)
        {
            pos.y = terrainY + 1.5f;
            groundCamera.transform.position = pos;
        }
    }
}