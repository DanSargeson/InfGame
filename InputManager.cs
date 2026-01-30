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
            // 1. Gesture Support (Scrolling & UI Taps)
            while (TouchPanel.IsGestureAvailable) {
                var g = TouchPanel.ReadGesture();

                if (g.GestureType == GestureType.Tap) {
                    OnTap?.Invoke(new Point((int)g.Position.X, (int)g.Position.Y));
                }
                else if (g.GestureType == GestureType.VerticalDrag) {
                    OnVerticalScroll?.Invoke(g.Delta.Y);
                }
            }

            // 2. Raw Input (Fast Gameplay Tapping)
            // Note: This duplicates the Gesture Tap slightly, but is faster.
            // You might want to separate "UI Tap" (Gesture) vs "Game Tap" (Raw)
            // For now, let's fire OnTap for this too, and let UI decide.
            var touchState = TouchPanel.GetState();
            foreach (var touch in touchState) {
                if (touch.State == TouchLocationState.Pressed) {
                    OnTap?.Invoke(new Point((int)touch.Position.X, (int)touch.Position.Y));
                }
            }
        }
    }
}