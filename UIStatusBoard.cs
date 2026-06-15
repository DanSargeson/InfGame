using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfGame
{
    public class UIStatusBoard : UIElement
    {
        private GameState _state;

        public UIStatusBoard(Rectangle bounds, GameState state) {
            Bounds = bounds;
            _state = state;
        }

        public override void Update(double dt, GameState state) { }
        public override bool HandleTap(Point p) => false;

        public override void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel, int scrollOffset = 0) {
            var currentCps = _state.SoulsPerSecond * _state.TimeScale;

            // Draw Stats
            sb.DrawString(font, $"Souls: {NumberFormat.Compact(_state.Souls)}", new Vector2(Bounds.X, Bounds.Y), Color.White);
            sb.DrawString(font, $"Per Sec: {NumberFormat.Compact(currentCps, 2)}", new Vector2(Bounds.X, Bounds.Y + 40), Color.White);
            sb.DrawString(font, $"Rebirth Pts: {NumberFormat.Compact(_state.RebirthPoints)}", new Vector2(Bounds.X, Bounds.Y + 80), Color.Gold);
            sb.DrawString(font, $"Multiplier: {NumberFormat.Compact(_state.prestigeMult, 2)}x", new Vector2(Bounds.X, Bounds.Y + 120), Color.Green);

            // Corruption Colors
            Color colour = Color.White;
            if (_state.Corruption > 0.5) colour = Color.Yellow;
            if (_state.Corruption > 0.65) colour = Color.Orange;
            if (_state.Corruption > 0.80) colour = Color.OrangeRed;
            if (_state.Corruption > 0.90) colour = Color.Red;

            var speedPct = (_state.TimeScale * 100).ToString("F1");
            var corruptionPct = (_state.Corruption * 100).ToString("F1");
            var bonusPct = ((_state.CorruptionBonus - 1.0) * 100).ToString("F0");

            sb.DrawString(font, $"Integrity: {speedPct}% (Corruption: {corruptionPct}%)", new Vector2(Bounds.X, Bounds.Y + 160), colour);
            sb.DrawString(font, $"Rebirth Bonus: +{bonusPct}%", new Vector2(Bounds.X, Bounds.Y + 200), Color.Plum);
        }
    }
}