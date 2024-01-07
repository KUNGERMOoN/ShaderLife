using System;
using UnityEngine.UIElements;

public class ButtonBinder : IDisposable
{
    public readonly Button Button;
    readonly Action Clicked;

    public ButtonBinder(Button button, Action clicked)
    {
        Button = button;
        Button.RegisterCallback<ClickEvent>(OnClicked);
        Clicked = clicked;
    }

    void OnClicked(ClickEvent @event) => Clicked();

    public void Dispose()
    {
        Button.UnregisterCallback<ClickEvent>(OnClicked);
    }
}
