namespace Marbles.Core.Model.Levels.LevelDefinitionComponents
{
    public class TimeIncreaseSpecialMarbleRandomizationSettingsComponent : SpecialMarbleRandomizationSettingComponent
    {
        public override SpecialMarbleType SpecialMarbleType
        {
            get
            {
                return SpecialMarbleType.TimeExtensionMarble;
            }
        }

        public float SecondsToAdd = 10;
    }
}