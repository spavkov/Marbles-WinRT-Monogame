using System.Collections.Generic;

namespace Marbles.Core.Model.Levels.LevelDefinitionComponents
{
    public class InitialRandomizationSpecialMarbleSettingsComponent : LevelDefinitionComponent
    {
        public List<SpecialMarbleRandomizationSettingComponent> Settings = new List<SpecialMarbleRandomizationSettingComponent>();  
    }

    public class PostInitialRandomizationSpecialMarbleSettingsComponent : LevelDefinitionComponent
    {
        public List<SpecialMarbleRandomizationSettingComponent> Settings = new List<SpecialMarbleRandomizationSettingComponent>();
    }
}