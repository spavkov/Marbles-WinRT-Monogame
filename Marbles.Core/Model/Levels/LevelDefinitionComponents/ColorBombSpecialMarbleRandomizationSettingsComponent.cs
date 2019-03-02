namespace Marbles.Core.Model.Levels.LevelDefinitionComponents
{
    public class ColorBombSpecialMarbleRandomizationSettingsComponent : SpecialMarbleRandomizationSettingComponent
    {
        public override SpecialMarbleType SpecialMarbleType
        {
            get
            {
                return SpecialMarbleType.ColorBombMarble;
            }
        }
    }
}