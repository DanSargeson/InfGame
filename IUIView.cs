using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace InfGame
{
    public interface IUIView
    {
        void Layout(Rectangle bounds, Stack<UiButton> buttonPool);
        void Update(double dt);
        void UpdateData(double dt);

        // Removed scrollY. The view manages its own layout coordinates now.
        void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel);

        // --- NEW: Input Handling ---
        bool HandleTap(Point p);
        void HandleScroll(float deltaY);

        void Cleanup(Stack<UiButton> buttonPool);
    }
}