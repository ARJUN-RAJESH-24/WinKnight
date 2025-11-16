using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Media;
using System.IO;
using System.Windows.Media;

namespace WinKnightUI
{
    public partial class StartupSplashScreen : Window
    {
        private DispatcherTimer _loadingTimer;
        private DispatcherTimer _progressTimer;
        private DispatcherTimer _closeTimer;
        private int _dotCount = 0;
        private int _progressValue = 0;
        private SoundPlayer _soundPlayer;

        public StartupSplashScreen()
        {
            InitializeComponent();
            PlayStartupSound();
            StartAnimations();
            InitializeApp();
        }

        private void PlayStartupSound()
        {
            try
            {
                // Try to load sound from embedded resources or file system
                string[] possibleSoundPaths = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "startup.wav"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.wav"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Assets", "startup.wav"),
                    "pack://application:,,,/Assets/startup.wav"
                };

                foreach (string soundPath in possibleSoundPaths)
                {
                    try
                    {
                        if (File.Exists(soundPath) || soundPath.StartsWith("pack://"))
                        {
                            _soundPlayer = new SoundPlayer(soundPath);
                            _soundPlayer.Play();
                            break;
                        }
                    }
                    catch
                    {
                        // Continue to next path if this one fails
                        continue;
                    }
                }

                // If no sound file found, use system beep as fallback
                if (_soundPlayer == null)
                {
                    SystemSounds.Beep.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sound error: {ex.Message}");
                // Fallback to system beep
                SystemSounds.Beep.Play();
            }
        }

        private void StartAnimations()
        {
            // Start shield animation
            var shieldAnimation = (Storyboard)FindResource("ShieldAnimation");
            shieldAnimation.Begin();

            // Start glow animation
            var glowAnimation = (Storyboard)FindResource("GlowAnimation");
            glowAnimation.Begin();

            // Start text glow animation
            var textGlowAnimation = (Storyboard)FindResource("TextGlowAnimation");
            textGlowAnimation.Begin();

            // Start loading dots animation
            _loadingTimer = new DispatcherTimer();
            _loadingTimer.Interval = TimeSpan.FromMilliseconds(500);
            _loadingTimer.Tick += LoadingTimer_Tick;
            _loadingTimer.Start();

            // Start progress bar animation
            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(50);
            _progressTimer.Tick += ProgressTimer_Tick;
            _progressTimer.Start();
        }

        private void LoadingTimer_Tick(object sender, EventArgs e)
        {
            _dotCount = (_dotCount + 1) % 4;
            LoadingDots.Text = new string('.', _dotCount);
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (_progressValue < 100)
            {
                _progressValue += 2; // Faster progress for 2.5 second total
                ProgressFill.Width = (ActualWidth - 80) * (_progressValue / 100.0);
            }
            else
            {
                _progressTimer.Stop();
            }
        }

        private void InitializeApp()
        {
            // Simulate initialization process - show for 2.5 seconds total
            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromMilliseconds(2500);
            _closeTimer.Tick += (s, e) =>
            {
                _closeTimer.Stop();
                _loadingTimer.Stop();
                _progressTimer.Stop();
                ShowMainWindow();
            };
            _closeTimer.Start();
        }

        private void ShowMainWindow()
        {
            // Stop sound if it's still playing
            _soundPlayer?.Stop();
            
            var mainWindow = new MainWindow();
            mainWindow.Show();
            
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clean up timers
            _loadingTimer?.Stop();
            _progressTimer?.Stop();
            _closeTimer?.Stop();
            
            // Dispose sound player
            _soundPlayer?.Dispose();
            
            base.OnClosed(e);
        }
    }
}