using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfGame
{
    public class UiButton : UIElement
    {

        public string Text;
        private float _scale = 1.0f;
        public Action OnClick;
        public bool IsActive = true; // Can we afford it?
        private float _flashTimer = 0f; // For the visual "pop"
        public object Tag; // Optional user data

        public UiButton(Rectangle bounds, string text, Action onClick) {
            Bounds = bounds;
            Text = text;
            OnClick = onClick;
        }

        public bool HitTest(Point p) => IsActive && Bounds.Contains(p);

        public void TriggerFlash() {
            _flashTimer = 0.15f; // Flash for 150ms
            _scale = 0.95f;
        }

        public override  void Update(double dt, GameState state) {
            if (_flashTimer > 0) _flashTimer -= (float)dt;

            if (_scale < 1.0f) {
                _scale += (float)dt * 2.0f; // Speed of recovery
                if (_scale > 1.0f) _scale = 1.0f;
            }
        }

        public override bool HandleTap(Point p) {
            if (IsActive && Bounds.Contains(p)) {
                _flashTimer = 0.15f;
                _scale = 0.95f;
                OnClick?.Invoke();
                return true; // Input was consumed
            }
            return false;
        }

        public void Configure(Rectangle bounds, string text, Action onClick) {
            Bounds = bounds;
            Text = text;
            OnClick = onClick;

            // Reset State
            IsActive = true;
            Tag = null; // Important: Clear old data references!
            _flashTimer = 0f;
        }

        public override void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel, int scrollOffset = 0) {
            // Calculate center for scaling
            var screenRect = new Rectangle(Bounds.X, Bounds.Y - scrollOffset, Bounds.Width, Bounds.Height);
            Vector2 center = new Vector2(screenRect.X + screenRect.Width / 2f, screenRect.Y + screenRect.Height / 2f);

            // Draw Background (Scaled)
            // We use a destination rectangle calculated from the scale
            int w = (int)(Bounds.Width * _scale);
            int h = (int)(Bounds.Height * _scale);
            var drawRect = new Rectangle((int)(center.X - w / 2), (int)(center.Y - h / 2), w, h);

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