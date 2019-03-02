using System;
using Marbles.Core.Model;
using Microsoft.Xna.Framework;
using Roboblob.XNA.WinRT.Content;

namespace Marbles.Core.Helpers
{
    public class MarbleTexturesHelper
    {
        private Rectangle _yellowMarbleRect;
        private Rectangle _blueMarbleRect;

        private Rectangle _greenMarbleRect;
        private Rectangle _orangeMarbleRect;
        private Rectangle _purpleMarbleRect;
        private Rectangle _redMarbleRect;
        private Rectangle _grayMarbleRect;
        private Rectangle _brownMarbleRect;
        private Rectangle _silverMarbleRect;

        private Vector2 _marbleTextureOriginVector;

        public MarbleTexturesHelper(TextureSheet gameArtSheet)
        {
            Initialize( gameArtSheet);
        }

        private void Initialize(TextureSheet gameArtSheet)
        {
            _yellowMarbleRect = gameArtSheet.SubTextures["yellow"].Rect;
            _blueMarbleRect = gameArtSheet.SubTextures["blue"].Rect;
            _greenMarbleRect = gameArtSheet.SubTextures["green"].Rect;
            _orangeMarbleRect = gameArtSheet.SubTextures["orange"].Rect;
            _purpleMarbleRect = gameArtSheet.SubTextures["purple"].Rect;
            _redMarbleRect = gameArtSheet.SubTextures["red"].Rect;
            _grayMarbleRect = gameArtSheet.SubTextures["gray"].Rect;
            _brownMarbleRect = gameArtSheet.SubTextures["brown"].Rect;
            _silverMarbleRect = gameArtSheet.SubTextures["silver"].Rect;

            _marbleTextureOriginVector = new Vector2(GameConstants.MarbleTextureWidth / 2, GameConstants.MarbleTextureHeight / 2); // all marbles should be same size for this to work
        }

        public Vector2 GetMarbleTextureOriginVector()
        {
            return _marbleTextureOriginVector;
        }

        public void GetMarbleRectable(MarbleColor color, out Rectangle rectangle)
        {
            switch (color)
            {
                case MarbleColor.Gray:
                    rectangle = _grayMarbleRect;
                    break;
                case MarbleColor.Blue:
                    rectangle = _blueMarbleRect;
                    break;
                case MarbleColor.Brown:
                    rectangle = _brownMarbleRect;
                    break;
                case MarbleColor.Silver:
                    rectangle = _silverMarbleRect;
                    break;
                case MarbleColor.Green:
                    rectangle = _greenMarbleRect;
                    break;
                case MarbleColor.Orange:
                    rectangle = _orangeMarbleRect;
                    break;
                case MarbleColor.Purple:
                    rectangle = _purpleMarbleRect;
                    break;
                case MarbleColor.Red:
                    rectangle = _redMarbleRect;
                    break;
                case MarbleColor.Yellow:
                    rectangle = _yellowMarbleRect;
                    break;
                default:
                    throw new Exception("Invalid rectangle color " + color);
            }
        }
    }
}