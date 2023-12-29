using UnityEngine;

public class GUIManager : MonoBehaviour
{
    /*[Header("References")]
    public UIDocument Document;
    public SimulationRunner SimulationRunner;

    VisualElement root;

    #region UIElements
    SwitchButton playButton;
    SwitchButton pauseButton;
    Button nextButton;
    Button clearButton;

    UnsignedIntegerField simulationSpeedField;

    IntegerField seedField;
    SwitchButton randomSeedButton;
    Slider chanceSlider;
    Button randomizeButton;
    #endregion UIElements

    private void OnEnable()
    {
        root = Document.rootVisualElement;

        playButton = root.Q<SwitchButton>(name: "play");
        playButton.RegisterCallback<ClickEvent>(SwitchPlayState);
        pauseButton = root.Q<SwitchButton>(name: "pause");
        pauseButton.RegisterCallback<ClickEvent>(SwitchPlayState);
        nextButton = root.Q<Button>(name: "next");
        nextButton.RegisterCallback<ClickEvent>(NextButton);
        clearButton = root.Q<Button>(name: "clear");
        clearButton.RegisterCallback<ClickEvent>(ClearButton);

        RefreshControlButtons();

        simulationSpeedField = root.Q<UnsignedIntegerField>(name: "simulationSpeed");
        simulationSpeedField.RegisterCallback<ChangeEvent<uint>>(OnSimulationSpeedChanged);

        seedField = root.Q<IntegerField>("seed");
        seedField.RegisterCallback<ChangeEvent<int>>(OnSeedChanged);
        randomSeedButton = root.Q<SwitchButton>("randomSeed");
        randomSeedButton.RegisterCallback<ClickEvent>(RandomSeedButton);
        chanceSlider = root.Q<Slider>("chance");
        chanceSlider.RegisterCallback<ChangeEvent<float>>(OnChanceSliderChanged);
        randomizeButton = root.Q<Button>(name: "randomize");
        randomizeButton.RegisterCallback<ClickEvent>(Randomize);
    }

    private void OnDisable()
    {
        playButton.UnregisterCallback<ClickEvent>(SwitchPlayState);
        pauseButton.UnregisterCallback<ClickEvent>(SwitchPlayState);
        nextButton.UnregisterCallback<ClickEvent>(NextButton);
        clearButton.UnregisterCallback<ClickEvent>(ClearButton);

        simulationSpeedField.UnregisterCallback<ChangeEvent<uint>>(OnSimulationSpeedChanged);

        seedField.UnregisterCallback<ChangeEvent<int>>(OnSeedChanged);
        randomSeedButton.UnregisterCallback<ClickEvent>(RandomSeedButton);
        chanceSlider.UnregisterCallback<ChangeEvent<float>>(OnChanceSliderChanged);
        randomizeButton.UnregisterCallback<ClickEvent>(Randomize);
    }

    void SwitchPlayState(ClickEvent ctx)
    {
        Simulation.UpdateInRealtime = !Simulation.UpdateInRealtime;
        RefreshControlButtons();
    }

    void NextButton(ClickEvent ctx) => Simulation.UpdateBoard();

    //TODO: This creates a completely new simulation and thus does a lot of unnecessary work.
    //We should instead just run a compute kernel that would clear the board
    void ClearButton(ClickEvent ctx) => Simulation.CreateSimulation();

    void RefreshControlButtons()
    {
        //TODO: also use an alternative texture for the button icons when they are pressed down
        //Maybe we should move the button that's currently pressed down downwards a bit?
        bool play = Simulation.UpdateInRealtime;

        playButton.Pressed = play == true;
        pauseButton.Pressed = play == false;
    }

    void OnSeedChanged(ChangeEvent<int> ctx) => Simulation.Seed = ctx.newValue;

    void RandomSeedButton(ClickEvent ctx) => Simulation.RandomSeed = !Simulation.RandomSeed;

    void OnChanceSliderChanged(ChangeEvent<float> ctx) => Simulation.Chance = ctx.newValue;

    //TODO: Use int internally, also if the value is 0 simply simulate every frame
    void OnSimulationSpeedChanged(ChangeEvent<uint> ctx) => Simulation.BoardUpdateRate = ctx.newValue;

    void Randomize(ClickEvent ctx) => Simulation.Randomise();*/
}
