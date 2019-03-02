namespace Marbles.Core.Model.Levels.LevelDefinitionComponents
{
    public class GameOverSpecialMarbleRandomizationSettingsComponent : SpecialMarbleRandomizationSettingComponent
    {
        public override SpecialMarbleType SpecialMarbleType
        {
            get
            {
                return SpecialMarbleType.GameOverMarble;
            }
        }

        public float TimeUntilEndInSeconds = 10;
    }
}