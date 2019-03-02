namespace Marbles.Core.Model.Levels.LevelDefinitionComponents
{
    public abstract class LineClearerSpecialMarbleRandomizationSettingsComponent : SpecialMarbleRandomizationSettingComponent
    {
        public override SpecialMarbleType SpecialMarbleType
        {
            get
            {
                return SpecialMarbleType.LineClearerMarble;
            }
        }
    }

    public class VerticalLineClearerSpecialMarbleRandomizationSettingsComponent : LineClearerSpecialMarbleRandomizationSettingsComponent
    {

    }

    public class HorizontalLineClearerSpecialMarbleRandomizationSettingsComponent : LineClearerSpecialMarbleRandomizationSettingsComponent
    {

    }

    public class HorizontalAndVerticalLineClearerSpecialMarbleRandomizationSettingsComponent : LineClearerSpecialMarbleRandomizationSettingsComponent
    {

    }
}