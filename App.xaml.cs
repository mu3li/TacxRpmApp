namespace TacxRpmApp;

public partial class App : Application
{
    public App(MainPage mainPage)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(mainPage, false);
        MainPage = new NavigationPage(mainPage);
    }
}
