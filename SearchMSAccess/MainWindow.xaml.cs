using System.Windows;
using System.Windows.Forms;

namespace SearchMSAccess
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private SearchModel model
        {
            get
            {
                return (SearchModel)DataContext;
            }
        }

        private void btnFillFilename_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "MS Access files | *.accdb";
            dialog.Multiselect = false;
            if (dialog.ShowDialog().Value)
            {
                model.FilePath = dialog.FileName;
            }
        }

        private void btnFillFoldername_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                model.FolderPath = dialog.SelectedPath;
            }
        }
    }
}
