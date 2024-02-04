using System.Collections.Generic;
using UnityEngine.UIElements;

public class SwitchButton : BaseBoolField
{
    public new class UxmlFactory : UxmlFactory<SwitchButton, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        readonly UxmlBoolAttributeDescription ValueAttribute = new() { name = "value" };

        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield return new UxmlChildElementDescription(typeof(VisualElement)); }
        }

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            SwitchButton button = (SwitchButton)ve;
            button.Pressed = ValueAttribute.GetValueFromBag(bag, cc);
        }
    }

    public static new readonly string ussClassName = "switch-button";

    public bool Pressed
    {
        get => base.value;
        set => base.value = value;
    }

    public SwitchButton() : base("")
    {
        AddToClassList(ussClassName);
    }
}