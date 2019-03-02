namespace Marbles.Core.Model.Components.SpecialMarbles
{
    public class LineClearerSpecialMarbleDetails : SpecialMarbleDetails
    {
        public LineClearerType ClearerType;
    }

    public enum LineClearerType
    {
        VerticalClearer,
        HorizontalClearer,
        HorizontalAndVerticalClearer,
    }
}