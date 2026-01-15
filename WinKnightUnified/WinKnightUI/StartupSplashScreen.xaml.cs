using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace WinKnightUI
{
    public partial class StartupSplashScreen : Window
    {
        private DispatcherTimer? _loadingTimer;
        private int _loadingStep = 0;
        private readonly string[] _loadingSteps = {
            "Initializing System Protection...",
            "Loading Security Modules...",
            "Starting Monitoring Services...",
            "Preparing User Interface...",
            "Almost Ready..."
        };

        public StartupSplashScreen()
        {
            InitializeComponent();
            StartLoadingAnimation();
        }

        private void StartLoadingAnimation()
        {
            // Start the loading text animation
            _loadingTimer = new DispatcherTimer();
            _loadingTimer.Interval = TimeSpan.FromMilliseconds(600);
            _loadingTimer.Tick += LoadingTimer_Tick;
            _loadingTimer.Start();
        }

        private void LoadingTimer_Tick(object? sender, EventArgs e)
        {
            // Cycle through loading dots
            string baseText = "Loading modules";
            int dotCount = (_loadingStep % 4) + 1;
            LoadingText.Text = baseText + new string('.', dotCount);

            // Update status text every few cycles
            if (_loadingStep % 8 == 0 && _loadingStep / 8 < _loadingSteps.Length)
            {
                StatusText.Text = _loadingSteps[_loadingStep / 8];
            }

            _loadingStep++;

            // Stop after going through all steps a couple times
            if (_loadingStep >= _loadingSteps.Length * 8)
            {
                _loadingTimer?.Stop();
                LoadingText.Text = "Ready!";
                StatusText.Text = "Launching WinKnight...";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _loadingTimer?.Stop();
            base.OnClosed(e);
        }
    }
}