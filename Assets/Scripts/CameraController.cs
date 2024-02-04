using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Camera Camera;
    public SimulationRunner Simulation;
    public GUIManager GUIManager;

    Vector2 velocity;
    Vector2 moveInput;

    float targetZoom;
    float zoom = 0.9f;

    Vector2 zoomPosition;

    bool focusingBack;

    private void Start()
    {
        targetZoom = zoom;
    }

    private void Update()
    {
        if (focusingBack || GUIManager.Focused) return;

        //Zoom
        float input = Input.mouseScrollDelta.y;
        if (input != 0)
            zoomPosition = MouseToScreenPos();
        if (Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.E))
            input = 1;
        else if (Input.GetKeyDown(KeyCode.PageDown) || Input.GetKeyDown(KeyCode.Q))
            input = -1;

        targetZoom += input * GameManager.LoadedSettings.CameraZoomMultiplier;
        targetZoom = Mathf.Max(targetZoom, 0);

        //Movement
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

    private void FixedUpdate()
    {
        if (GUIManager.Focused) return;

        //Zoom
        float zoomStep;
        if (GameManager.LoadedSettings.CameraZoomSmoothness != 0f)
            zoomStep = 1 / Mathf.Clamp(GameManager.LoadedSettings.CameraZoomSmoothness, 0f, 10f);
        else zoomStep = float.PositiveInfinity;

        var lastOrthographicSize = Camera.orthographicSize;
        zoom = Mathf.Lerp(zoom, targetZoom, zoomStep);
        Camera.orthographicSize = 1 / Mathf.Pow(2, zoom);

        //We offset the camera to make sure you zoom "into" the mouse position instead of the center of the screen
        //To see why we calculate it like that, see Docs/ZoomIntoPosition.txt
        Vector2 zoomOffset = zoomPosition * new Vector2(Camera.aspect, 1) *
            (lastOrthographicSize - Camera.orthographicSize);

        transform.position += (Vector3)zoomOffset;

        //Movement
        float speed = Camera.orthographicSize * 2 * Mathf.Clamp01(GameManager.LoadedSettings.CameraMaxSpeed);

        var acceleration = Mathf.Max(0.03f, GameManager.LoadedSettings.CameraAcceleration);
        Vector2 velocityDelta = Vector2.ClampMagnitude(moveInput * speed - velocity,
            acceleration);
        velocity += velocityDelta;

        transform.position += (Vector3)velocity * Time.fixedDeltaTime;

        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -1, 1),
            Mathf.Clamp(transform.position.y, -1, 1),
            transform.position.z);
    }

    Vector2Int lastCellPos;
    private void LateUpdate()
    {
        var boardPos = ScreenToWorldPos(MouseToScreenPos()) + Vector2.one / 2;
        Vector2Int cellPos = (boardPos * Simulation.BoardSize).FloorToInt();

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

    Vector2 MouseToScreenPos()
        => new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height)
            * 2 - Vector2.one;

    Vector2 ScreenToWorldPos(Vector2 screenPos)
        => (Vector2)Camera.transform.position + ScreenWorldSize * screenPos;

    Vector2 ScreenWorldSize => Camera.orthographicSize * new Vector2(Camera.aspect, 1);

    //Based on: https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm#All_cases
    public void DrawLine(Vector2Int start, Vector2Int end, bool value)
    {
        Vector2Int Δ = new(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));
        Vector2Int direction = new(start.x < end.x ? 1 : -1, start.y < end.y ? 1 : -1);
        Vector2Int current = start;
        int error = Δ.x - Δ.y; //Diffrence between Δ.x and Δ.y
        int boardSize = Simulation.BoardSize;

        while (true)
        {
            if (current.x < boardSize && current.x >= 0 && current.y < boardSize && current.y >= 0)
            {
                Simulation.SetPixel(current, value);
            }

            if (current == end) break;

            if (2 * error >= -Δ.y)
            {
                if (current.x == end.x) break;
                current.x += direction.x;
                error += -Δ.y;
            }
            if (2 * error <= Δ.x)
            {
                if (current.y == end.y) break;
                current.y += direction.y;
                error += Δ.x;
            }
        }
    }
}
