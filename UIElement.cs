using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfGame
{
    public abstract class UIElement
    {
        public virtual Rectangle Bounds { get; set; }
        public bool IsVisible { get; set; } = true;

        // Pass down the state so elements can read their own data if needed
        public abstract void Update(double dt, GameState state);
        public abstract void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel, int scrollOffset = 0);
        public abstract bool HandleTap(Point p);
    }
}