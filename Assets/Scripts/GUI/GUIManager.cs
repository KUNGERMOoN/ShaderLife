using MiniBinding;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class GUIManager : MonoBehaviour
{
    [Header("References")]
    public UIDocument Document;
    public SimulationRunner Simulation;
    public EventSystem EventSystem;

    VisualElement root;

    public bool Focused
    {
        get => root.focusController.focusedElement != null;
    }

    private void OnEnable()
    {
        root = Document.rootVisualElement;

        MiniBinder.Bind(Q<SwitchButton>("play"), Simulation.UpdateInRealtime);
        MiniBinder.Bind(Q<SwitchButton>("pause"), Simulation.UpdateInRealtime,
            x => !x, x => !x);

        MiniBinder.Bind(Q<Button>("next"), Simulation.UpdateBoard);
        MiniBinder.Bind(Q<Button>("clear"), Simulation.ClearBoard);

        MiniBinder.Bind(Q<UnsignedIntegerField>("simulationSpeed"), Simulation.BoardUpdateRate);

        MiniBinder.Bind(Q<IntegerField>("seed"), Simulation.Seed);
        MiniBinder.Bind(Q<Slider>("chance"), Simulation.Chance);
        MiniBinder.Bind(Q<Button>("randomize"), Simulation.Randomise);
    }

    private void OnDisable()
    {
        MiniBinder.UnbindUI();
    }

    T Q<T>(string name) where T : VisualElement
        => root.Q<T>(name);
}
