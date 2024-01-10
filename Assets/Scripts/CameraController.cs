using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Camera Camera;
    public SimulationRunner Simulation;
    public GUIManager GUIManager;

    Vector2 velocity;
    Vector2 moveInput;
    float zoomInput;
    float zoom = 0.9f;

    bool focusingBack;

    private void Update()
    {
        float input = Input.mouseScrollDelta.y;
        if (Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.E))
            input = 1;
        else if (Input.GetKeyDown(KeyCode.PageDown) || Input.GetKeyDown(KeyCode.Q))
            input = -1;

        if (focusingBack) input = 0;

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

        transform.position += (Vector3)velocity * Time.fixedDeltaTime;

        float bounds = GameManager.LoadedSettings.CameraPositionLimit;
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -bounds, bounds),
            Mathf.Clamp(transform.position.y, -bounds, bounds),
            transform.position.z);
    }

    Vector2Int lastCellPos;
    private void LateUpdate()
    {
        Vector2Int cellPos = CalculateSelectedCell();

        if (focusingBack)
        {
            lastCellPos = cellPos;
        }

        if (GUIManager.Focused == false)
        {
            int mouseState = 0;

            if (Input.GetKey(KeyCode.Mouse0))
                mouseState = 1;
            else if (Input.GetKey(KeyCode.Mouse1))
                mouseState = -1;

            if (mouseState != 0)
            {
                DrawLine(lastCellPos, cellPos, mouseState > 0);
            }
        }

        lastCellPos = cellPos;
        focusingBack = false;
    }

    private void OnApplicationFocus(bool focus)
        => focusingBack = focus;

    Vector2Int CalculateSelectedCell()
    {
        var screenPos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height) * 2 - Vector2.one;
        var worldPos = (Vector2)Camera.transform.position + Camera.orthographicSize * new Vector2(screenPos.x * Camera.aspect, screenPos.y);

        var boardPos = worldPos + Vector2.one / 2;
        return (boardPos * Simulation.BoardSize).FloorToInt();
    }

    //Shamelessly stolen straight from the wikipedia: https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm#All_cases
    public void DrawLine(Vector2Int start, Vector2Int end, bool value)
    {
        //TODO: Rewrite this in a more organised fashion
        Vector2Int d = new(Mathf.Abs(end.x - start.x), -Mathf.Abs(end.y - start.y));
        Vector2Int s = new(start.x < end.x ? 1 : -1, start.y < end.y ? 1 : -1);
        int error = d.x + d.y;
        int boardSize = Simulation.BoardSize;

        while (true)
        {
            if (start.x < boardSize && start.x >= 0 && start.y < boardSize && start.y >= 0)
                Simulation.SetPixel(start, value);
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
