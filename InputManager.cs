using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using System;

namespace InfGame
{
    public class InputManager
    {
        public event Action<Point> OnTap;
        public event Action<float> OnVerticalScroll;

        public void Update() {
            // 1. Gesture Support (Scrolling ONLY)
            // We removed GestureType.Tap to prevent double-clicking
            while (TouchPanel.IsGestureAvailable) {
                var g = TouchPanel.ReadGesture();

                if (g.GestureType == GestureType.VerticalDrag) {
                    OnVerticalScroll?.Invoke(g.Delta.Y);
                }
            }

            // 2. Raw Input (Fast Tapping)
            // This handles BOTH Gameplay Taps and UI Clicks instantly
            var touchState = TouchPanel.GetState();
            foreach (var touch in touchState) {
                if (touch.State == TouchLocationState.Pressed) {
                    OnTap?.Invoke(new Point((int)touch.Position.X, (int)touch.Position.Y));
                }
            }
        }
    }
}