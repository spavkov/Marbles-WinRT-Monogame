using System;
using System.Collections.Generic;
using Marbles.Core.Systems;
using Windows.UI.Xaml.Documents;

namespace Marbles.Core
{
    public class GamePriorities

    {
        public static List<Type> Systems = new List<Type>()
                                               {
                                                   typeof (SequenceVisualizationRenderingSystem),
                                                   typeof (MarbleBoardRendererSystem),                                                  
                                                   
                                                   typeof (TouchSequencesSystem),
                                                   typeof (MarbleBoardTouchedSequencesReplacerSystem),
                                                   typeof (BoardRandomizationSystem),
                                                   typeof (SpecialMarblesCullminationSystem),
                                                   typeof (CurrentGameInformationTrackingSystem),
                                                   typeof (MarbleGameLevelControllerSystem),
                                                   typeof (LevelScoringSystem),

                                                   typeof (NewMarblesBounceInitializationSystem),
                                                   typeof (MarbleVerticalBouncerSys),
                                                   typeof (SpecialMarblesClearingAndAddingTrackerSystem),                                                  
                                                   typeof (SpecialMarblesClearingPostEffectsSystem),   

                                                   typeof (ScoreChangesVisualizerSystem),
                                                   typeof (MarbleSpecialEffectsRenderingSystem),
                                                   typeof (GuiRendererSystem),
                                                   typeof (MarbleSoundsSystem),

                                               };


    }
}