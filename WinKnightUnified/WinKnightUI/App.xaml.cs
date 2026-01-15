using System;
using System.Windows;
using System.Media;
using System.Threading.Tasks;
using System.IO;

namespace WinKnightUI
{
    public partial class App : Application
    {
        private StartupSplashScreen? _splashScreen;
        private SoundPlayer? _soundPlayer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create and show splash screen
            _splashScreen = new StartupSplashScreen();
            _splashScreen.Show();

            // Play startup sound
            PlayStartupSound();

            // Initialize main window in background
            InitializeMainWindow();
        }

        private void PlayStartupSound()
        {
            try
            {
                string audioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "startup.wav");
                
                if (File.Exists(audioPath))
                {
                    _soundPlayer = new SoundPlayer(audioPath);
                    _soundPlayer.Play();
                    ActivityLogger.Log("Startup sound played successfully");
                }
                else
                {
                    ActivityLogger.Log($"Audio file not found: {audioPath}");
                    // Try alternative path
                    audioPath = "Assets/startup.wav";
                    if (File.Exists(audioPath))
                    {
                        _soundPlayer = new SoundPlayer(audioPath);
                        _soundPlayer.Play();
                    }
                }
            }
            catch (Exception ex)
            {
                ActivityLogger.Log($"Sound play failed: {ex.Message}");
            }
        }

        private async void InitializeMainWindow()
        {
            // Simulate some loading time
            await Task.Delay(3000);

            // Create and show main window
            var mainWindow = new MainWindow();
            mainWindow.Loaded += (s, e) =>
            {
                _splashScreen?.Close();
                _soundPlayer?.Stop();
                _soundPlayer?.Dispose();
            };

            mainWindow.Show();
        }
    }
}