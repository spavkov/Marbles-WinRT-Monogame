namespace Marbles.Core.Model
{
    public class MarblesGameSettings
    {
        private bool _soundsEffectsEnabled = true;
        public bool SoundsEffectsEnabled
        {
            get { return _soundsEffectsEnabled; }
            set { _soundsEffectsEnabled = value; }
        }
    }
}