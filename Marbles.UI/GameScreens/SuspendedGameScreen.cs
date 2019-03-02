using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roboblob.XNA.WinRT.GameStateManagement;

namespace Marbles.UI.GameScreens
{
    public class SuspendedGameScreen : GameScreen
    {
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;

        public SuspendedGameScreen(Game game) : base(game)
        {
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);
        }

        protected override void LoadContent()
        {
            _font = Game.Content.Load<SpriteFont>(@"SpriteFonts\debugfont");
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (_font != null)
            {
                _spriteBatch.Begin();
                _spriteBatch.DrawString(_font, "Paused", new Vector2(100,100), Color.Red);
                _spriteBatch.End();
            }
            base.Draw(gameTime);
        }
    }
}