using Marbles.Core.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roboblob.XNA.WinRT.GameStateManagement;
using Roboblob.XNA.WinRT.ResolutionIndependence;

namespace Marbles.UI.GameScreens
{
    public class RollingBackgroundTileScreen : GameScreen
    {
        private readonly Game _game;
        private ResolutionIndependentRenderer _resolution;
        protected SpriteBatch SpriteBatch;
        private Rectangle _fullScreenRectangle;
        private Texture2D _menuBgTileTexture;
        private double _elapsed;

        public RollingBackgroundTileScreen(Game game) : base(game)
        {
            _game = game;
            SpriteBatch = new SpriteBatch(_game.GraphicsDevice);
            _resolution = Game.Services.GetService(typeof(ResolutionIndependentRenderer)) as ResolutionIndependentRenderer;
            CalucalteScreenElementSizes();
        }

        public override void OnScreenSizeChanged()
        {
            CalucalteScreenElementSizes();
            base.OnScreenSizeChanged();
        }

        protected override void LoadContent()
        {
            _menuBgTileTexture = _game.Content.Load<Texture2D>(GameConstants.RollingBackgroundTileName);
            base.LoadContent();
        }

        private void CalucalteScreenElementSizes()
        {
            _fullScreenRectangle = new Rectangle(_lastFullScreenRectangleX, 0, _resolution.ScreenWidth, _resolution.ScreenHeight);
        }

        public bool PauseRollingBackground;
        private static int _lastFullScreenRectangleX;

        public override void Update(GameTime gameTime)
        {
            if (!PauseRollingBackground)
            {
                _elapsed += gameTime.ElapsedGameTime.TotalSeconds;
                if (_elapsed >= 0.04)
                {
                    _elapsed = 0;
                    _fullScreenRectangle.X = _fullScreenRectangle.X + 1;
                    _lastFullScreenRectangleX = _fullScreenRectangle.X;
                    if (_fullScreenRectangle.X > _fullScreenRectangle.X + _menuBgTileTexture.Width)
                    {
                        _fullScreenRectangle.X = 0;
                    }
                }
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            _resolution.SetupFullViewport();

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone);

            SpriteBatch.Draw(_menuBgTileTexture, Vector2.Zero, _fullScreenRectangle, Color.White);

            SpriteBatch.End();

            //base.Draw(gameTime);
        }
    }
}