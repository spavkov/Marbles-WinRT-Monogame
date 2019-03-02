using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Model.Components
{
    public class NewSpecialMarblePostEffectComponent : Component
    {
        public List<BoardCell<GameEntity>> TargetCells = new List<BoardCell<GameEntity>>();
        public List<GameEntity> TargetEntities = new List<GameEntity>();
        public double RemainingDurationinSeconds = 2f;
        public PostEffectType EffectType;

    }

    public enum PostEffectType
    {
        Electric,
        Burn,
    }
}