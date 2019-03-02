using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Model.Components
{
    public class VerticalBounceComponent : Component
    {
        public Vector2 CurrentPosition = new Vector2();
        public Vector2 DestinationPosition = new Vector2();

        public float InitialVelocity = 100f;
        public float BounceCoeficient = 0.8f;

        public float CurrentVelocity = 0;

        public bool Finished;

        public bool Started;

        public float Height;
    }
}