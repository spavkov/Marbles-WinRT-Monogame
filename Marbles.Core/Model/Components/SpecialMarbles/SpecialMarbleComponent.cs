using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Model.Components.SpecialMarbles
{
    public class SpecialMarbleComponent : Component
    {
        public SpecialMarbleType SpecialMarbleType { get; set; }

        public SpecialMarbleDetails Details { get; set; }
    }

    public abstract class SpecialMarbleDetails
    {
        
    }
}