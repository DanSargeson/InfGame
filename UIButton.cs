using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfGame
{
    public sealed class UiButton
    {
        // "Logical" bounds (where it sits in the long list, ignoring scroll)
        public Rectangle Bounds;
        public string Text;

        public Action OnClick;
        public bool IsActive = false; // Can we afford it?

        private float _flashTimer = 0f; // For the visual "pop"

        public UiButton(Rectangle bounds, string text, Action onClick) {
            Bounds = bounds;
            Text = text;
            OnClick = onClick;
        }

        public bool HitTest(Point p) => IsActive && Bounds.Contains(p);

        public void TriggerFlash() {
            _flashTimer = 0.15f; // Flash for 150ms
        }

        public void Update(double dt) {
            if (_flashTimer > 0) _flashTimer -= (float)dt;
        }

        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel, int scrollOffset = 0) {
            // Calculate screen position based on scroll
            var screenRect = new Rectangle(Bounds.X, Bounds.Y - scrollOffset, Bounds.Width, Bounds.Height);

            // 1. Determine Color
            Color color;
            Color textColor = Color.White;

            if (_flashTimer > 0) {
                // Flash Bright White (Visual Cue)
                color = Color.White * 0.8f;
                textColor = Color.Black;
            }
            else if (IsActive) {
                // Normal (Affordable)
                color = Color.White * 0.2f;
            }
            else {
                // Disabled (Too expensive) - Dim Red
                color = Color.Red * 0.1f;
                textColor = Color.Gray;
            }

            // 2. Draw Background
            sb.Draw(pixel, screenRect, color);

            // 3. Draw Text (Centered)
            if (!string.IsNullOrEmpty(Text)) {
                var size = font.MeasureString(Text);
                var pos = new Vector2(
                    screenRect.Center.X - size.X / 2f,
                    screenRect.Center.Y - size.Y / 2f
                );
                sb.DrawString(font, Text, pos, textColor);
            }
        }
    }
}