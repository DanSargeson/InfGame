using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfGame
{
    public class FloatingText
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public string Text;
        public Color Color;

        public float Life;     // Current life
        public float MaxLife;  // Total duration (e.g. 1.0s)

        public bool IsActive => Life > 0;

        public FloatingText(Vector2 pos, string text, Color color) {
            Position = pos;
            Text = text;
            Color = color;
            Velocity = new Vector2(0, -100); // Moves Up
            Life = 1.0f;
            MaxLife = 1.0f;
        }

        public void Update(double dt) {
            Life -= (float)dt;
            Position += Velocity * (float)dt;
        }

        public void Draw(SpriteBatch sb, SpriteFont font) {
            if (!IsActive) return;

            // Fade out alpha
            float alpha = Life / MaxLife;
            sb.DrawString(font, Text, Position, Color * alpha);
        }
    }
}