using UnityEngine;

public class GameOfLifeComponent : MonoBehaviour
{
    GameOfLife simulation;

    [ExecuteAlways]
    private void Awake()
    {
        if (simulation != null) simulation.Dispose();
        simulation = new GameOfLife();
    }
}
