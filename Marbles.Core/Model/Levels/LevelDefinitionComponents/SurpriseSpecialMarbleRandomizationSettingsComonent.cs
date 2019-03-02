namespace Marbles.Core.Model.Levels.LevelDefinitionComponents
{
    public class SurpriseSpecialMarbleRandomizationSettingsComonent : SpecialMarbleRandomizationSettingComponent
    {
        public override SpecialMarbleType SpecialMarbleType
        {
            get
            {
                return SpecialMarbleType.SurpriseMarble;
            }
        }

        public SpecialMarbleType SurpriseType = SpecialMarbleType.None;
    }
}