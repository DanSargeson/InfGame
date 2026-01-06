using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfGame
{
    public sealed class UiButton
    {
        public Rectangle Bounds;
        public string Text;

        public UiButton(Rectangle bounds, string text) {
            Bounds = bounds;
            Text = text;
        }

        public bool HitTest(Point p) => Bounds.Contains(p);

        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel) {
            // simple flat button (no styling rabbit hole)
            sb.Draw(pixel, Bounds, Color.White * 0.15f);

            var size = font.MeasureString(Text);
            var pos = new Vector2(
                Bounds.Center.X - size.X / 2f,
                Bounds.Center.Y - size.Y / 2f
            );
            sb.DrawString(font, Text, pos, Color.White);
        }
    }
}
