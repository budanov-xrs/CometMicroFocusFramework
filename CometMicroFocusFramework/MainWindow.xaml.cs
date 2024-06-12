using System.Threading;
using System.Windows;

namespace CometMicroFocusFramework
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private CometMicroFocus _xray;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _xray = new CometMicroFocus();
            
            new Thread(Update).Start();
        }

        private void Update()
        {
            ActualMa.Content = _xray.ActualMa;
            XRayStatus.Content = _xray.IsHv;
        }
    }
}