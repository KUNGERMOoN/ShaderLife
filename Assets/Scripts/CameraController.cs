using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Camera Camera;
    public SimulationRunner SimulationManager;

    Vector2 velocity;
    Vector2 moveInput;
    float zoomInput;
    float zoom;

    enum MouseState { None, Lmb, Rmb }

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
    MouseState lastMouseState;
    private void LateUpdate()
    {
        var screenPos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height) * 2 - Vector2.one;
        var worldPos = (Vector2)Camera.transform.position + Camera.orthographicSize * new Vector2(screenPos.x * Camera.aspect, screenPos.y);

        var boardPos = worldPos + Vector2.one / 2;
        Vector2Int cellPos = (boardPos * SimulationManager.Simulation.Size).FloorToInt();
        MouseState mouseState;

        if (Input.GetKey(KeyCode.Mouse0))
            mouseState = MouseState.Lmb;
        else if (Input.GetKey(KeyCode.Mouse1))
            mouseState = MouseState.Rmb;
        else
            mouseState = MouseState.None;

        //TODO: maybe someday add line drawing
        /*if (Input.GetKey(KeyCode.LeftShift))
        {
            if (mouseState != lastMouseState)
            {
                Debug.Log($"{nameof(lastMouseState)}: {lastMouseState}, {nameof(mouseState)}: {mouseState}");
                if (mouseState == MouseState.None)
                {
                    DrawLine(lastCellPos, cellPos, lastMouseState == MouseState.Lmb);
                }
                lastMouseState = mouseState;
            }
            if (mouseState == MouseState.None)
            {
                lastCellPos = cellPos;
            }
        }
        else
        {*/
        if (mouseState != MouseState.None)
        {
            DrawLine(lastCellPos, cellPos, mouseState == MouseState.Lmb);
        }

        lastCellPos = cellPos;
        lastMouseState = mouseState;
        /*}*/
    }

    //Shamelessly stolen straight from the wikipedia: https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm#All_cases
    public void DrawLine(Vector2Int start, Vector2Int end, bool value)
    {
        Vector2Int d = new(Mathf.Abs(end.x - start.x), -Mathf.Abs(end.y - start.y));
        Vector2Int s = new(start.x < end.x ? 1 : -1, start.y < end.y ? 1 : -1);
        int error = d.x + d.y;
        int boardSize = SimulationManager.Simulation.Size;

        while (true)
        {
            if (start.x < boardSize && start.x >= 0 && start.y < boardSize && start.y >= 0)
                SimulationManager.Simulation.SetPixel(start, value);
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
