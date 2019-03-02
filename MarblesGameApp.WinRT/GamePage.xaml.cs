using System;
using Marbles.Core.Helpers;
using Marbles.Core.Repositories;
using Marbles.UI;
using Roboblob.Core.WinRT.Threading;
using Roboblob.MVVM.WinRT.ViewModels;
using SharpDX.Direct2D1;
using Windows.ApplicationModel.Activation;
using Windows.Data.Html;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MonoGame.Framework;


namespace MarblesGameApp.WinRT
{
    /// <summary>
    /// The root page used to display the game.
    /// </summary>
    public sealed partial class GamePage : IExceptionPopupHelper
    {
        internal Rect splashImageRect; // Rect to store splash screen image coordinates.
        internal bool dismissed = false; // Variable to track splash screen dismissal status.
        internal Frame rootFrame;

        private SplashScreen splash; // Variable to hold the splash screen object.

        readonly MarblesGame _game;
        private CoreWindow _window;
        private UiThreadDispatcher _uiThreadDispatcher;
        private Exception _lastException;
        private GameSettingsRepository _settingsRepository;

        public GamePage(string launchArguments, SplashScreen splashscreen, bool loadState)
        {
            this.InitializeComponent();

           _window = CoreWindow.GetForCurrentThread();
           _window.SizeChanged += _window_SizeChanged;

            try
            {
                // Create the game.
                _game = XamlGame<MarblesGame>.Create(launchArguments, Window.Current.CoreWindow, this);
                _uiThreadDispatcher = _game.Services.GetService(typeof (UiThreadDispatcher)) as UiThreadDispatcher;
                _game.ExceptionsPopupHelper = this;               
            }
            catch (Exception e)
            {
                this.ShowExceptionPopup(e);
            }
        }



        public MarblesGame Game
        {
            get { return _game; }
        }

        public bool IsOpen
        {
            get { return exceptionPopupControl.IsOpen; }
        }

        public void ShowExceptionPopup(Exception e)
        {
            _lastException = e;
            _game.PauseGameplay();
            _uiThreadDispatcher.InvokeOnUiThread(() =>
                                                     {
                                                         if (exceptionPopupControl != null && !exceptionPopupControl.IsOpen)
                                                         {
                                                             exceptionPopupControl.DataContext = new ExceptionViewModel(e);
                                                             RootPopupBorder.Width = Window.Current.Bounds.Width / 2 + 100;
                                                             RootPopupBorder.Height = Window.Current.Bounds.Height / 1.5;
                                                             exceptionPopupControl.HorizontalOffset = (Window.Current.Bounds.Width - RootPopupBorder.Width)/2;
                                                             exceptionPopupControl.VerticalOffset = (Window.Current.Bounds.Height - RootPopupBorder.Height) / 2;
                                                             detailsGrid.Visibility = Visibility.Collapsed;
                                                             toggleDetailsLink.Content = "Show Error Details";
                                                             exceptionPopupControl.IsOpen = true;
                                                         }
                                                     });
        }

        private void _window_SizeChanged(CoreWindow sender, WindowSizeChangedEventArgs args)
        {
            _game.ScreenSizeChanged(args.Size.Width, args.Size.Height);
        }

        private void OnExceptionOkClick(object sender, RoutedEventArgs e)
        {
            if (exceptionPopupControl != null && exceptionPopupControl.IsOpen)
            {
                exceptionPopupControl.IsOpen = false;
            }
        }

        private async void OnExceptionSendClick(object sender, RoutedEventArgs e)
        {
            var uri =
                Uri.EscapeUriString("mailto:"+ GameConstants.ExceptionsEmail + "?subject=Unhandled exception in Marbles game" + "&body=" +
                                    _lastException.Message + Environment.NewLine + _lastException.StackTrace +
                                    Environment.NewLine + _lastException.Source);
            var mail = new Uri(uri);
            await Windows.System.Launcher.LaunchUriAsync(mail);  
        }

        private void ToggleDetailsClicked(object sender, RoutedEventArgs e)
        {
            detailsGrid.Visibility = detailsGrid.Visibility == Visibility.Visible
                                         ? Visibility.Collapsed
                                         : Visibility.Visible;

            toggleDetailsLink.Content = detailsGrid.Visibility == Visibility.Visible
                                            ? "Hide Details"
                                            : "Show Error Details";
        }
    }

    public class ExceptionViewModel : ViewModelBase
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }

        public ExceptionViewModel(Exception exception)
        {
            if (exception == null)
            {
                return;
            }

            Message = exception.Message;
            StackTrace = exception.StackTrace;

        }
    }
}
