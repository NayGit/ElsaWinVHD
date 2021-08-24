using ElsaWinVHD.ViewModel;
using System.Text;
using System.Windows;

namespace ElsaWinVHD
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

#if !NET40
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif

            var w = new MainWindow() 
            { 
                DataContext = new MainViewModel()
            };
            //this.MainWindow = w;
            w.Show();

        }
    }
}
