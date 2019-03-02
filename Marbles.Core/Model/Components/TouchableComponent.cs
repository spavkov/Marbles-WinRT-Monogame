using System;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Model.Components
{
    public class TouchableComponent : Component
    {
        public bool IsTouched;

        public bool IsTouchable;

        public BoundingSphere Bounds = new BoundingSphere();

        private Vector3 _touchVector = new Vector3(0,0,0);

        public DateTime LastTouchDateTime;

        public bool ContainsPoint(Vector2 point)
        {
            if (!IsTouchable)
            {
                return false;
            }

            _touchVector.X = point.X;
            _touchVector.Y = point.Y;

            return Bounds.Contains(_touchVector) == ContainmentType.Contains;
        }
    }
}