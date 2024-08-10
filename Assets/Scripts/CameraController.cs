using GameOfLife.GUI;
using UnityEngine;

namespace GameOfLife
{
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

        Vector2 zoomMousePosition;

        bool wasWindowFocused = true;

        private void Start()
        {
            targetZoom = zoom;
        }

        private void Update()
        {
            //Prevent all movement if a popup is opened
            if (Popup.OpenedPopups.Count > 0) return;

            //Zoom
            //Prevent sudden zoom jumps after opening other windows and using scroll wheel
            if (wasWindowFocused)
            {
                float input = Input.mouseScrollDelta.y;
                if (input != 0)
                    zoomMousePosition = MouseToScreenPos();

                if (Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.E))
                {
                    input = 1;
                    zoomMousePosition = Vector2.zero;
                }
                else if (Input.GetKeyDown(KeyCode.PageDown) || Input.GetKeyDown(KeyCode.Q))
                {
                    input = -1;
                    zoomMousePosition = Vector2.zero;
                }

                targetZoom += input * GameManager.Settings.CameraZoomMultiplier;
                targetZoom = Mathf.Max(targetZoom, 0);
            }

            //Movement
            //Avoid typing characters into text boxes and moving the camera at the same time
            if (GUIManager.Focused == false)
            {
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
        }

        private void FixedUpdate()
        {
            //Zoom
            float zoomStep;
            if (GameManager.Settings.CameraZoomSmoothness != 0f)
                zoomStep = 1 / Mathf.Clamp(GameManager.Settings.CameraZoomSmoothness, 0f, 10f);
            else zoomStep = float.PositiveInfinity;

            var lastOrthographicSize = Camera.orthographicSize;
            zoom = Mathf.Lerp(zoom, targetZoom, zoomStep);
            Camera.orthographicSize = 1 / Mathf.Pow(2, zoom);

            //We offset the camera to make sure you "zoom into" the mouse position instead of the center of the screen
            //The math behind all of this is explained in Docs/ZoomIntoPosition.txt
            Vector2 zoomOffset = zoomMousePosition * new Vector2(Camera.aspect, 1) *
                (lastOrthographicSize - Camera.orthographicSize);

            transform.position += (Vector3)zoomOffset;

            //Movement
            float speed = Camera.orthographicSize * 2 * Mathf.Clamp01(GameManager.Settings.CameraMaxSpeed);

            var acceleration = Camera.orthographicSize * 2 * Mathf.Max(0.03f, GameManager.Settings.CameraAcceleration);
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
        bool wasDrawFocused = false;
        private void LateUpdate()
        {
            //Drawing
            var boardPos = ScreenToWorldPos(MouseToScreenPos()) + Vector2.one / 2;
            Vector2Int cellPos = Vector2Int.FloorToInt(boardPos * Simulation.BoardSize);

            bool isDrawFocused =
                Popup.OpenedPopups.Count == 0 &&
                !GUIManager.Focused;

            //To avoid accidentally drawing long stripes of cells when switching windows
            if (wasWindowFocused == false)
            {
                lastCellPos = cellPos;
            }

            //Make sure both this and a previous frame were elgible to draw on the board
            if (isDrawFocused && wasDrawFocused)
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
            wasDrawFocused = isDrawFocused;

            //At the end of the frame, reset the wasWindowFocused value
            wasWindowFocused = true;
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus)
                wasWindowFocused = false;
        }

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
}