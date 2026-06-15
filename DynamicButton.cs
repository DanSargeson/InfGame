using Java.Lang.Annotation;
using Microsoft.Xna.Framework;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace InfGame
{
    public class DynamicButton : UiButton
    {
        private Func<string> _textFunc;
        private Func<bool> _activeFunc;

        public DynamicButton(Rectangle bounds, Func<string> textFunc, Func<bool> activeFunc, Action onClick)
            : base(bounds, "", onClick) {
            _textFunc = textFunc;
            _activeFunc = activeFunc;
        }

        public override void Update(double dt, GameState state) {
            base.Update(dt, state);
            // The button automatically updates its own text and state!
            if (_textFunc != null) Text = _textFunc();
            if (_activeFunc != null) IsActive = _activeFunc();
        }
    }
}