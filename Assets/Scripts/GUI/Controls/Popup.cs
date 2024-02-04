using System;
using UnityEngine.UIElements;

public class Popup : VisualElement
{
    public new class UxmlFactory : UxmlFactory<Popup, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        readonly UxmlStringAttributeDescription TitleAttribute = new()
        {
            name = "title",
            defaultValue = defaultTitle
        };

        readonly UxmlBoolAttributeDescription CloseableAttribute = new()
        {
            name = "closeable",
            defaultValue = true
        };

        readonly UxmlBoolAttributeDescription DraggableAttribute = new()
        {
            name = "draggable",
            defaultValue = true
        };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            Popup popup = (Popup)ve;
            popup.Title = TitleAttribute.GetValueFromBag(bag, cc);
            popup.Closeable = CloseableAttribute.GetValueFromBag(bag, cc);
            popup.Draggable = DraggableAttribute.GetValueFromBag(bag, cc);
        }
    }

    private readonly VisualElement container;
    private readonly VisualElement header;
    private readonly Label headerLabel;
    private readonly Button headerClose;
    private readonly VisualElement content;

    public override VisualElement contentContainer => content;

    public static readonly string ussClassName = "popup";
    public static readonly string containerUssClassName = ussClassName + "-container";
    public static readonly string headerUssClassName = containerUssClassName + "__header";
    public static readonly string headerLabelUssClassName = headerUssClassName + "-label";
    public static readonly string headerCloseUssClassName = headerUssClassName + "-close";
    public static readonly string contentUssClassName = containerUssClassName + "__content";
    public static readonly string closeableUssClassName = ussClassName + "__closeable";
    public static readonly string draggableUssClassName = ussClassName + "__draggable";

    public string Title
    {
        get => headerLabel.text;
        set => headerLabel.text = value;
    }
    const string defaultTitle = "New popup";

    bool closeable;
    public bool Closeable
    {
        get => closeable;
        set
        {
            closeable = value;
            headerClose.style.display = closeable ? DisplayStyle.Flex : DisplayStyle.None;
            if (value)
                AddToClassList(closeableUssClassName);
            else
                RemoveFromClassList(closeableUssClassName);
        }
    }

    public event Action Opened, Closed;

    private readonly Draggable dragManipulator;
    public bool Draggable
    {
        get => dragManipulator.enabled;
        set
        {
            dragManipulator.enabled = value;
            if (value)
                AddToClassList(draggableUssClassName);
            else
                RemoveFromClassList(draggableUssClassName);
        }
    }

    public bool IsOpened { get; private set; }

    public void Open()
    {
        style.display = DisplayStyle.Flex;
        IsOpened = true;
        Opened?.Invoke();
    }

    public void Close()
    {
        style.display = DisplayStyle.None;
        IsOpened = false;
        Closed?.Invoke();
    }

    public Popup() : this(defaultTitle) { }

    public Popup(string title, bool closeable = true, bool draggable = true)
    {
        RegisterCallback<ClickEvent>(ctx =>
        {
            if (Closeable && focusController.focusedElement == this)
                Close();
        });

        focusable = true;
        AddToClassList(ussClassName);
        style.position = Position.Absolute;

        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;

        container = new VisualElement { name = nameof(container) };
        container.AddToClassList(containerUssClassName);
        var auto = new StyleLength(StyleKeyword.Auto);
        container.style.marginLeft = auto;
        container.style.marginRight = auto;
        container.style.marginTop = auto;
        container.style.marginBottom = auto;
        container.focusable = true;
        hierarchy.Add(container);

        header = new() { name = nameof(header) };
        header.AddToClassList(headerUssClassName);
        header.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
        header.style.width = Length.Percent(100);
        header.focusable = true;
        container.hierarchy.Add(header);

        headerLabel = new(title) { name = nameof(headerLabel) };
        dragManipulator = new Draggable(container, this);
        headerLabel.AddManipulator(dragManipulator);
        headerLabel.AddToClassList(headerLabelUssClassName);
        headerLabel.style.flexGrow = 1;
        headerLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
        header.Add(headerLabel);

        headerClose = new() { name = nameof(headerClose) };
        headerClose.RegisterCallback<ClickEvent>(ctx => Close());
        headerClose.AddToClassList(headerCloseUssClassName);
        headerClose.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        headerClose.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        headerClose.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        headerClose.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);

        header.Add(headerClose);

        content = new VisualElement
        {
            name = "content-container"
        };
        content.AddToClassList(contentUssClassName);
        container.hierarchy.Add(content);

        Closeable = closeable;
        Draggable = draggable;
    }
}
