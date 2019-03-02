using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Model.Components
{
    public class ScreenDataComponent : Component
    {
        public Vector2 Position = new Vector2(0,0);

        public float RotationAngle = 0;
    }
}