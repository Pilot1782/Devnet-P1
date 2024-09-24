namespace DevNet_P11;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        //MainPage = new AppShell();
        App.Current.MainPage = new MainPage();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        Window window = base.CreateWindow(activationState);

        window.Destroying += (s, e) =>
        {
            DevNet_P11.MainPage.Scraper.Shutdown();
        };

        return window;
    }
}