﻿namespace DevNet_P11;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        Window window = base.CreateWindow(activationState);

        window.Destroying += (s, e) =>
        {
            DevNet_P11.MainPage.scraper.Shutdown();
        };

        return window;
    }
}