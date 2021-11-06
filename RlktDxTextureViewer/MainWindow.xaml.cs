using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RlktDxTextureViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Utils utils = new Utils();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
                return;
        }

        private void loadBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "DirectX Mesh (.X)|*.X";
            if (ofd.ShowDialog() == true)
            {
                utils.LoadXFile(ofd.FileName);
                
                texName.Text = utils.GetTextureName();
                texName.IsEnabled = true;

                currentFilePanel.Visibility = Visibility.Visible;
                currentFile.Content = utils.GetFileName();

                saveBtn.IsEnabled = true;
            }
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = utils.GetFileName();
            if(sfd.ShowDialog() == true)
            {
                utils.SetTextureName(texName.Text);
                utils.SaveXFile(sfd.FileName);
            }
        }
    }
}
