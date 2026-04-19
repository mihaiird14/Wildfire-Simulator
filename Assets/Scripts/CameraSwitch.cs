using UnityEngine;
public class CameraSwitch : MonoBehaviour
{
    [Header("Camere")]
    public Camera mapCamera;      // camera 2d
    public Camera groundCamera;   // camera 3d

    [Header("Miscare la sol")]
    public float moveSpeed = 10f;
    public float sprintSpeed = 25f;
    public float rotateSpeed = 2f;

    // Modul curent
    private bool _isGroundMode = false;

    // Unghiurile camerei la sol
    private float _yaw = 0f;
    private float _pitch = 10f;

    void Start()
    {
        // Pornim in Modul 1 - harta
        SetMode(false);
    }

    void Update()
    {
        // Tab comuta intre moduri
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _isGroundMode = !_isGroundMode;
            SetMode(_isGroundMode);
        }

        // Miscare doar in Modul 2
        if (_isGroundMode)
        {
            HandleGroundMovement();
            HandleGroundRotation();
            KeepAboveTerrain();
        }
    }
    //schimba camera
    void SetMode(bool groundMode)
    {
        mapCamera.gameObject.SetActive(!groundMode);
        groundCamera.gameObject.SetActive(groundMode);

        if (groundMode)
            Debug.Log("[CameraSwitch] Modul 2 - La sol | WASD=miscare, Click dreapta=rotire, Tab=inapoi la harta");
        else
            Debug.Log("[CameraSwitch] Modul 1 - Harta | Tab=treci la sol");
    }

    //wasd
    void HandleGroundMovement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += groundCamera.transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= groundCamera.transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= groundCamera.transform.right;
        if (Input.GetKey(KeyCode.D)) move += groundCamera.transform.right;
        move.y = 0f;
        move.Normalize();

        groundCamera.transform.position += move * speed * Time.deltaTime;
    }
    void HandleGroundRotation()
    {
        if (Input.GetMouseButton(1))
        {
            _yaw += Input.GetAxis("Mouse X") * rotateSpeed;
            _pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
            _pitch = Mathf.Clamp(_pitch, -30f, 60f);

            groundCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }

    void KeepAboveTerrain()
    {
        if (Terrain.activeTerrain == null) return;

        Vector3 pos = groundCamera.transform.position;
        float terrainY = Terrain.activeTerrain.SampleHeight(pos)
                         + Terrain.activeTerrain.transform.position.y;
        if (pos.y < terrainY + 2f)
        {
            pos.y = terrainY + 2f;
            groundCamera.transform.position = pos;
        }
    }
}