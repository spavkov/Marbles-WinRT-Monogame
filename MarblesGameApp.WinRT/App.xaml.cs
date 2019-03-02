using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Callisto.Controls;
using Marbles.UI;
using Marbles.UI.SettingsViewModels;
using Marbles.UI.SettingsViews;
using Roboblob.Core.WinRT.Threading;
using Roboblob.DependencyInjection;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Xaml;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace MarblesGameApp.WinRT
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private GamePage _gamePage;
        private static bool _isIocInitialized = false;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
            Resuming += OnResuming;
            UnhandledException += OnUnhandled;
            
            TaskScheduler.UnobservedTaskException += OnThreadException;
        }

        private void OnResuming(object sender, object e)
        {
            _gamePage.Game.Resume();
        }

        private void OnThreadException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            Debug.WriteLine("-----");
            Debug.WriteLine("Unhandled Exception occured in thread + " + e.Exception.Message);
            Debug.WriteLine(e.Exception.StackTrace);
            Debug.WriteLine("-----");

            if (_gamePage != null)
            {
                _gamePage.ShowExceptionPopup(e.Exception);
            }
        }

        private void OnUnhandled(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Debug.WriteLine("-----");
            Debug.WriteLine("Unhandled Exception occured + " + e.Exception.Message);
            Debug.WriteLine(e.Exception.StackTrace);
            Debug.WriteLine("-----");

            if (_gamePage != null)
            {
                _gamePage.ShowExceptionPopup(e.Exception);
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Settings
            SettingsPane.GetForCurrentView().CommandsRequested += Settings_CommandsRequested;

            new UiThreadDispatcher().Initialize();

            Window.Current.VisibilityChanged += OnVisibilityChanged;

            if (!_isIocInitialized)
            {
                MarblesUIBootstrapper.Init();
                _isIocInitialized = true;
            }

            _gamePage = Window.Current.Content as GamePage;                

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (_gamePage == null)
            {
                // Create a main GamePage

                bool loadState = (args.PreviousExecutionState == ApplicationExecutionState.Terminated);
                _gamePage = new GamePage(args.Arguments, args.SplashScreen, loadState);

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the GamePage in the current Window
                Window.Current.Content = _gamePage;
            }

            // Ensure the current window is active
            Window.Current.Activate();
            _gamePage.Game.GoToMainMenuScreen();
        }

        private void OnVisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            if (!e.Visible)
            {
                if (_gamePage.Game.IsGameplayCurrentlyRunning)
                    _gamePage.Game.PauseGameplay();
            }
        }

        private void Settings_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            // About
            var about = new SettingsCommand("About", "About", (x) =>
            {
                _gamePage.Game.PauseGameplay();
                var settings = new SettingsFlyout();
                settings.FlyoutWidth = SettingsFlyout.SettingsFlyoutWidth.Narrow;
                settings.HeaderText = "About";

                var aboutView = new SettingsAboutView();
                aboutView.DataContext = new SettingsAboutViewModel();
                settings.Content = aboutView;
                settings.IsOpen = true;
            });
            args.Request.ApplicationCommands.Add(about);

            var privacy = new SettingsCommand("Privacy", "Privacy Policy", async (x) =>
            {
                _gamePage.Game.PauseGameplay();
                await Windows.System.Launcher.LaunchUriAsync(new Uri("http://roboblob.com/games/marblesprivacypolicy.html"));
            });
            args.Request.ApplicationCommands.Add(privacy);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            _gamePage.Game.Suspend();
            
			
            deferral.Complete();
        }
    }
}
