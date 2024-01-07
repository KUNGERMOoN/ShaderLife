using MiniBinding;
using UnityEngine;
using UnityEngine.UIElements;

public class GUIManager : MonoBehaviour
{
    [Header("References")]
    public UIDocument Document;
    public SimulationRunner SimulationRunner;

    VisualElement root;

    private void OnEnable()
    {
        root = Document.rootVisualElement;

        MiniBinder.Bind(Q<SwitchButton>("play"), SimulationRunner.UpdateInRealtime);
        MiniBinder.Bind(Q<SwitchButton>("pause"), SimulationRunner.UpdateInRealtime,
            x => !x, x => !x);

        MiniBinder.Bind(Q<Button>("next"), NextButton);
        MiniBinder.Bind(Q<Button>("clear"), ClearButton);

        MiniBinder.Bind(Q<UnsignedIntegerField>("simulationSpeed"), SimulationRunner.BoardUpdateRate);

        MiniBinder.Bind(Q<IntegerField>("seed"), SimulationRunner.Seed);
        MiniBinder.Bind(Q<SwitchButton>("randomSeed"), SimulationRunner.RandomSeed);
        MiniBinder.Bind(Q<Slider>("chance"), SimulationRunner.Chance);
        MiniBinder.Bind(Q<Button>("randomize"), Randomize);
    }

    T Q<T>(string name) where T : VisualElement
        => root.Q<T>(name);

    private void OnDisable()
    {
        MiniBinder.UnbindUI();
    }

    void NextButton() => SimulationRunner.Simulation.UpdateBoard();

    void ClearButton() => SimulationRunner.Simulation.Clear();

    void Randomize() => SimulationRunner.Randomise();
}
