using System;

namespace Marbles.Core.Helpers
{
    public interface IExceptionPopupHelper
    {
        void ShowExceptionPopup(Exception e);
        bool IsOpen { get; }
    }
}