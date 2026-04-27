namespace CortriumBLE
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Subscribe to unhandled exceptions in non-UI threads
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Subscribe to unobserved task exceptions
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            MainPage = new AppShell();
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved(); // Prevents the app from terminating
            ShowErrorMessage(e.Exception);
        }

        private async void ShowErrorMessage(Exception ex)
        {
            // Log the exception details
            System.Diagnostics.Debug.WriteLine($"Unhandled exception: {ex}");

            // Show an error message to the user
            if (MainPage != null)
            {
                await MainPage.DisplayAlert("Error", "An unexpected error occurred. Please try again.", "OK");
            }
        }
    }
}
