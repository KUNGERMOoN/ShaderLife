using Sirenix.OdinInspector;
using System;
using UnityEngine;

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

        Vector2Int cellPos = (boardPos * GameOfLife.Simulation.Size).FloorToInt();

        if (Mathf.Clamp01(boardPos.x) == boardPos.x && Mathf.Clamp01(boardPos.y) == boardPos.y)
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
                var posDelta = cellPos - lastCellPos;

                Vector2Int iteratorPos = lastCellPos;

                do
                {
                    //How many pixels do we need to go upwards (or downwards) in this column
                    int yDelta = Mathf.FloorToInt(posDelta.y * (iteratorPos.x - lastCellPos.x) / posDelta.x);

                    int y = 0;
                    do
                    {
                        iteratorPos.y += Math.Sign(y);
                        GameOfLife.SetPixel(iteratorPos, value);

                        y += Math.Sign(y);
                    }
                    while (y != yDelta);

                    iteratorPos.x += Math.Sign(posDelta.x); //Move to the next column
                }
                while (iteratorPos.x != cellPos.x);

                /*for (int x = lastCellPos.x; x != cellPos.x; x += Math.Sign(posDelta.x))
                {
                    int y = lastCellPos.y + ;
                    for (int i = 0; i < Mathf.Abs(y - lastY); i++)
                    {
                        GameOfLife.SetPixel(new(x, y + i * Math.Sign(y - lastY)), value);
                    }
                }*/
            }
        }

        lastCellPos = cellPos;
    }

    [Button]
    public void DrawLine(Vector2Int start, Vector2Int end, bool value)
    {
        var posDelta = end - start;

        if (posDelta.x >= 0)
        {
            if (posDelta.y >= 0)
            {
                if (posDelta.x >= posDelta.y)
                {
                    Bresenham(new(posDelta.x, posDelta.y),
                        delta => GameOfLife.SetPixel(start + new Vector2Int(delta.x, delta.y), value));
                }
                else
                {
                    Bresenham(new(posDelta.y, posDelta.x),
                        delta => GameOfLife.SetPixel(start + new Vector2Int(delta.y, delta.x), value));
                }
            }
            else
            {
                if (posDelta.x >= -posDelta.y)
                {
                    Bresenham(new(-posDelta.y, posDelta.x),
                        delta => GameOfLife.SetPixel(start + new Vector2Int(delta.y, -delta.x), value));
                }
                else
                {
                    Bresenham(new(-posDelta.x, posDelta.y),
                        delta => GameOfLife.SetPixel(start + new Vector2Int(delta.x, -delta.y), value));
                }
            }
        }
        else
        {
            if (posDelta.y >= 0)
            {
                if (posDelta.y >= -posDelta.x)
                {
                    Bresenham(new(posDelta.y, -posDelta.x),
                        delta => GameOfLife.SetPixel(start + new Vector2Int(-delta.y, delta.x), value));
                }
                else
                {
                    Bresenham(new(posDelta.x, -posDelta.y),
                        delta => GameOfLife.SetPixel(start + new Vector2Int(-delta.x, delta.y), value));
                }
            }
            else
            {
                if (-posDelta.x >= -posDelta.y)
                {
                    Bresenham(new(-posDelta.x, -posDelta.y),
                        delta => GameOfLife.SetPixel(start + new Vector2Int(-delta.x, -delta.y), value));
                }
                else
                {
                    Bresenham(new(-posDelta.y, -posDelta.x),
                        delta => GameOfLife.SetPixel(start + new Vector2Int(-delta.y, -delta.x), value));
                }
            }
        }

        #region Unused
        /*if (posDelta.x > 0)
        {
            if (posDelta.y > 0)
            {
                Bresenham(new(posDelta.x, posDelta.y));
            }
            else
            {
                Bresenham(new(-posDelta.y, posDelta.x));
            }
        }
        else
        {
            if (posDelta.y > 0)
            {

            }
            else
            {
                Bresenham(new(-posDelta.x, -posDelta.y));
            }
        }*/

        /*
        var posDeltaSign = new Vector2Int(Math.Sign(posDelta.x), Math.Sign(posDelta.y));
        Vector2Int iteratorPos = lastCellPos;
        do
        {
            Vector2Int iteratorLocalPos = iteratorPos - lastCellPos;

            //How many pixels do we need to go upwards (or downwards) in this column
            int yDelta;
            if (posDelta.x != 0)
                yDelta = Mathf.FloorToInt(posDelta.y * iteratorLocalPos.x / posDelta.x) - iteratorLocalPos.y;
            else yDelta = posDelta.y;

            int y = 0;
            do
            {
                iteratorPos.y += Math.Sign(yDelta);
                GameOfLife.SetPixel(iteratorPos, value);

                y += Math.Sign(yDelta);
            }
            while (y != yDelta);

            iteratorPos.x += Math.Sign(posDelta.x); //Move to the next column
        }
        while (iteratorPos.x != cellPos.x + posDeltaSign.x);
        //while ((cellPos.x - iteratorPos.x) * Math.Sign(posDelta.x) >= 0);
        //^ Until we overshoot our target x position
        */
        #endregion Unused
    }

    //Shamelessly stolen from: https://classic.csunplugged.org/documents/activities/community-activities/line-drawing/line-drawing.pdf
    void Bresenham(Vector2Int posDelta, Action<Vector2Int> drawer)
    {
        int a = 2 * posDelta.y;
        int b = a - 2 * posDelta.x;
        int p = a - posDelta.x;

        drawer(new(0, 0));

        Vector2Int iterator = new(1, 0);
        while (iterator.x <= posDelta.x && iterator.y <= posDelta.y)
        {
            if (p < 0)
            {
                drawer(iterator);
                p += a;
            }
            else
            {
                iterator.y++;
                drawer(iterator);
                p += b;
            }

            iterator.x++;
        }
    }
}
