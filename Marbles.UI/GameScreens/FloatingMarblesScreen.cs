using System;
using System.Diagnostics;
using System.Text;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Roboblob.XNA.WinRT.Camera;
using Roboblob.XNA.WinRT.Content;
using Roboblob.XNA.WinRT.GameStateManagement;
using Roboblob.XNA.WinRT.GfxEffects.AdvancedParticleSystems;
using Roboblob.XNA.WinRT.GfxEffects.AdvancedParticleSystems.ParticleEmitters;
using Roboblob.XNA.WinRT.GfxEffects.AdvancedParticleSystems.ParticleModifiers.Gravity;
using Roboblob.XNA.WinRT.Input;
using Roboblob.XNA.WinRT.MenuSystem;
using Roboblob.XNA.WinRT.ResolutionIndependence;

namespace Marbles.UI.GameScreens
{
    public abstract class FloatingMarblesScreen : RollingBackgroundTileScreen
    {
        private readonly Game _game;
        private ResolutionIndependentRenderer _resolution;

        private Rectangle _fullScreenRectangle;

        private ParticleSystem _marblesParticleSystem;
        private Rectangle _rectangleForNewParticles;
        private ITextureSheetLoader _textureSheetLoader;
        private TextureSheet _gameArtTextureSheet;
        private Texture2D _menuBgTileTexture;

        private RectangleConstantEmitter _emitter;

        protected FloatingMarblesScreen(Game game) : base(game)
        {
            _game = game;
            _resolution = Game.Services.GetService(typeof(ResolutionIndependentRenderer)) as ResolutionIndependentRenderer;
            CalucalteScreenElementSizes();

            _textureSheetLoader = game.Services.GetService(typeof(ITextureSheetLoader)) as ITextureSheetLoader;
        }

        private void CalucalteScreenElementSizes()
        {
            _fullScreenRectangle = new Rectangle(0, 0, _resolution.ScreenWidth, _resolution.ScreenHeight);
            _rectangleForNewParticles = new Rectangle(0, _fullScreenRectangle.Height + 100, _fullScreenRectangle.Width, 5);

            if (_emitter != null)
            {
                _emitter.Rectangle = _rectangleForNewParticles;
            }
        }

        protected override void LoadContent()
        {
            _gameArtTextureSheet = _textureSheetLoader.Load(@"SpriteSheets\GameArt");

            CalucalteScreenElementSizes();

            _marblesParticleSystem = new ParticleSystem(_game, null, 100);
            _marblesParticleSystem.BlendState = BlendState.AlphaBlend;

            _emitter = new RectangleConstantEmitter(_rectangleForNewParticles, 2);
            _emitter.ParticleSubtextureNames.Add("blue");
            _emitter.ParticleSubtextureNames.Add("green");
            _emitter.ParticleSubtextureNames.Add("red");
            _emitter.ParticleSubtextureNames.Add("orange");
            _emitter.ParticleSubtextureNames.Add("yellow");
            _emitter.ParticleSubtextureNames.Add("silver");
            _emitter.ParticleMaxAlpha = 255;
            _emitter.ParticleMinAlpha = 255;

            _marblesParticleSystem.Emitters.Add(_emitter);
            _marblesParticleSystem.Modifiers.Add(new MainMenuCombinedModifier(-100));
            _marblesParticleSystem.TextureSheet = _gameArtTextureSheet;

            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            _marblesParticleSystem.Update(gameTime);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            _marblesParticleSystem.Draw(gameTime);

            _resolution.SetupVirtualScreenViewport();


        }

        public override void OnScreenSizeChanged()
        {
            CalucalteScreenElementSizes();
            base.OnScreenSizeChanged();
        }

        private class MinYParticleModifier : IParticleModifier
        {
            private readonly float _minY;

            public MinYParticleModifier(float minY)
            {
                _minY = minY;
            }

            public void Update(GameTime gameTime, Particle particle, float ageInPercents)
            {
                if (particle.Position.Y <= _minY)
                {
                    particle.IsAlive = false;
                    particle.RemainingLifeTimeInSeconds = 0;
                    particle.StartingLifetimeInSeconds = 0;
                }
            }
        }

        private class MainMenuCombinedModifier : IParticleModifier
        {
            private IParticleModifier _m1, _m2;
            private Vector2 _gravityForBurners = new Vector2(0, -9.2f);

            public MainMenuCombinedModifier(float minY)
            {
                _m1 = new DirectionalPullModifier { Gravity = _gravityForBurners };
                _m2 = new MinYParticleModifier(minY);
            }

            public void Update(GameTime gameTime, Particle particle, float ageInPercents)
            {
                _m1.Update(gameTime, particle, ageInPercents);
                _m2.Update(gameTime, particle, ageInPercents);
            }
        }

    }
}