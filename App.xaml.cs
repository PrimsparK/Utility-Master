using System.Windows;
using UtilityMaster.Services;

namespace UtilityMaster;

public partial class App : System.Windows.Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        using var db = DatabaseService.CreateContext();
        DatabaseService.InitializeDefaults(db);
        
        var mainWin = new MainWindow();
        mainWin.Show();
    }
}