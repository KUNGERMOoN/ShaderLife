using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Camera Camera;
    public GameOfLife GameOfLife;

    Vector2 velocity;
    Vector2 moveInput;
    float zoomInput;
    float zoom;

    private void Start()
    {
        zoom = 0.9f;
        velocity = Vector2.zero;
    }

    private void Update()
    {
        float input = Input.mouseScrollDelta.y;
        if (Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.E))
            input = 1;
        else if (Input.GetKeyDown(KeyCode.PageDown) || Input.GetKeyDown(KeyCode.Q))
            input = -1;

        zoomInput += input * GameManager.LoadedSettings.CameraZoomMultiplier;

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
        float deltaS = (Time.fixedDeltaTime * GameManager.LoadedSettings.CameraZoomMultiplier) / GameManager.LoadedSettings.CameraZoomTime;
        float zoomDelta = Mathf.Clamp(zoomInput, -deltaS, deltaS);
        zoomInput -= zoomDelta;

        zoom += zoomDelta;

        zoom = Mathf.Max(zoom, 0);

        //TODO: zoom to to mouse position rather than to the center of the screen

        Camera.orthographicSize = 1 / Mathf.Pow(2, zoom);

        float speed = Camera.orthographicSize * 2 * GameManager.LoadedSettings.CameraMaxSpeed;

        Vector2 velocityDelta = Vector2.ClampMagnitude(moveInput * speed - velocity, GameManager.LoadedSettings.CameraAcceleration);
        velocity += velocityDelta;

        transform.position += new Vector3(velocity.x, velocity.y, 0) * Time.fixedDeltaTime;

        float bounds = GameManager.LoadedSettings.CameraPositionLimit;
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -bounds, bounds),
            Mathf.Clamp(transform.position.y, -bounds, bounds),
            transform.position.z);
    }

    Vector2Int lastCellPos;
    private void LateUpdate()
    {
        var screenPos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height) * 2 - Vector2.one;
        var worldPos = (Vector2)Camera.transform.position + Camera.orthographicSize * new Vector2(screenPos.x * Camera.aspect, screenPos.y);

        var boardPos = worldPos + Vector2.one / 2;

        int boardsize = GameOfLife.Simulation.Size;
        Vector2Int cellPos = (boardPos * boardsize).FloorToInt();

        Vector2Int cellPosClamped = lastCellPos.Clamp(0, boardsize - 1);
        Vector2Int lastCellPosClamped = cellPos.Clamp(0, boardsize - 1);

        if (true)
        {
            bool pressed;
            bool value;

            if (Input.GetKey(KeyCode.Mouse0))
            {
                pressed = true;
                value = true;
            }
            else if (Input.GetKey(KeyCode.Mouse1))
            {
                pressed = true;
                value = false;
            }
            else
            {
                pressed = false;
                value = false;
            }

            if (pressed)
            {
                DrawLine(cellPos, lastCellPos, value);
            }
        }

        lastCellPos = cellPos;
    }

    [Button]
    //Shamelessly stolen straight from the wikipedia: https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm#All_cases
    public void DrawLine(Vector2Int start, Vector2Int end, bool value)
    {
        Vector2Int d = new(Mathf.Abs(end.x - start.x), -Mathf.Abs(end.y - start.y));
        Vector2Int s = new(start.x < end.x ? 1 : -1, start.y < end.y ? 1 : -1);
        int error = d.x + d.y;
        int boardSize = GameOfLife.Simulation.Size;

        while (true)
        {
            if (start.x < boardSize && start.x >= 0 && start.y < boardSize && start.y >= 0)
                GameOfLife.SetPixel(start, value);
            if (start.x == end.x && start.y == end.y) break;
            int e2 = 2 * error;
            if (e2 >= d.y)
            {
                if (start.x == end.x) break;
                error += d.y;
                start.x += s.x;
            }
            if (e2 <= d.x)
            {
                if (start.y == end.y) break;
                error += d.x;
                start.y += s.y;
            }
        }
    }
}
