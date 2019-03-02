using System;
using Marbles.Core.Model;
using Microsoft.Xna.Framework;

namespace Marbles.Core.Helpers
{
    public class MarbleColorsHelper
    {
        public Color MarbleColorToColor(MarbleColor marbleColor)
        {
            switch (marbleColor)
            {
                case MarbleColor.Yellow:
                    return Color.Yellow;

                case MarbleColor.Blue:
                    return Color.CadetBlue;

                case MarbleColor.Green:
                    return Color.FromNonPremultiplied(99, 248, 0, 255);

                case MarbleColor.Orange:
                    return Color.Orange;

                case MarbleColor.Purple:
                    return Color.FromNonPremultiplied(243,0,221, 255);

                case MarbleColor.Red:
                    return Color.Red;

                case MarbleColor.Brown:
                    return Color.Brown;

                case MarbleColor.Silver:
                    return Color.Silver;

                case MarbleColor.Gray:
                    return Color.Gray;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}