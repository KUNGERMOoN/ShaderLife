using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Camera Camera;
    public GameOfLife Simulation;

    [Space]
    public float Speed;
    public float Acceleration;
    [Tooltip("The change \"s\" of camera's zoom, if v = s / t")]
    public float ZoomMultiplier;
    [Tooltip("The time \"t\" of camera's zoom, if v = s / t")]
    public float ZoomTime;

    Vector2 Velocity;
    Vector2 moveInput;
    float zoomInput;
    float zoom;

    private void Start()
    {
        zoom = 1;
        Velocity = Vector2.zero;
    }

    private void Update()
    {
        zoomInput += Input.mouseScrollDelta.y * ZoomMultiplier;

        moveInput = new();
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveInput.x = 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveInput.x = -1;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveInput.y = 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveInput.y = -1;
    }

    void FixedUpdate()
    {
        //v = s / t
        //v = Δs / Δt
        //s / t = Δs / Δt
        //Δs = (Δt * s) / t
        float deltaS = (Time.fixedDeltaTime * ZoomMultiplier) / ZoomTime;
        float zoomDelta = Mathf.Clamp(zoomInput, -deltaS, deltaS);
        zoom += zoomDelta;
        zoomInput -= zoomDelta;

        Camera.orthographicSize = 1 / Mathf.Pow(2, zoom);

        float speed = Camera.orthographicSize * 2 * Speed;

        Vector2 velocityDelta = Vector2.ClampMagnitude(moveInput * speed - Velocity, Acceleration);
        Velocity += velocityDelta;

        transform.position += new Vector3(Velocity.x, Velocity.y, 0) * Time.fixedDeltaTime;
    }
}
