using System;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Model.Components
{
    public class TimedBombComponent : Component
    {
        public TimedBombComponent(int bombTimerInSeconds)
        {
            InitialTimerInSeconds = bombTimerInSeconds;
            SecondsUntilExplosion = bombTimerInSeconds;
            IsRunning = false;
            IsExploded = false;

        }

        public bool IsRunning { get; set; }

        public int InitialTimerInSeconds { get; set; }

        public double SecondsUntilExplosion { get; set; }

        public override void Start()
        {
            IsExploded = false;
            IsRunning = true;
        }

        public bool IsExploded { get; set; }

        public bool IsDone
        {
            get { return SecondsUntilExplosion <= 0; }
        }
    }
}