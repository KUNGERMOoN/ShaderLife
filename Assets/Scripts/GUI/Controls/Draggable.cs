using UnityEngine;
using UnityEngine.UIElements;

namespace GameOfLife.GUI
{
    //Heavily based on: docs.unity3d.com/Manual/UIE-manipulators.html
    //TODO: Get rid of the stuff that's not actually needed here
    public class Draggable : PointerManipulator
    {
        public bool enabled = true;

        public readonly VisualElement dragElement;
        public readonly VisualElement dragSpace;
        private Vector3 m_Start;
        protected bool m_Active;
        private int m_PointerId;

        public Draggable(VisualElement dragElement, VisualElement dragSpace)
        {
            this.dragElement = dragElement;
            this.dragSpace = dragSpace;
            m_PointerId = -1;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            m_Active = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        protected void OnPointerDown(PointerDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (CanStartManipulation(e) && enabled)
            {
                m_Start = e.localPosition;
                m_PointerId = e.pointerId;

                m_Active = true;
                target.CapturePointer(m_PointerId);
                e.StopPropagation();
            }
        }

        protected void OnPointerMove(PointerMoveEvent e)
        {
            if (!m_Active || !target.HasPointerCapture(m_PointerId))
                return;

            Vector2 delta = e.localPosition - m_Start;

            var spaceHeight = dragSpace.resolvedStyle.height - dragElement.resolvedStyle.height;
            var spaceWidth = dragSpace.resolvedStyle.width - dragElement.resolvedStyle.width;
            dragElement.style.top = Mathf.Clamp(dragElement.style.top.value.value + delta.y,
                -spaceHeight / 2, spaceHeight / 2);
            dragElement.style.left = Mathf.Clamp(dragElement.style.left.value.value + delta.x,
                -spaceWidth / 2, spaceWidth / 2);

            e.StopPropagation();
        }

        protected void OnPointerUp(PointerUpEvent e)
        {
            if (!m_Active || !target.HasPointerCapture(m_PointerId) || !CanStopManipulation(e))
                return;

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }
    }
}