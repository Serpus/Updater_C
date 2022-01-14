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
using System.Windows.Shapes;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для PrepareDeployWindow.xaml
    /// </summary>
    public partial class PrepareDeployWindow : Window
    {
        public PrepareDeployWindow()
        {
            InitializeComponent();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            StackPanel somePanel = null;

            if (sender is CheckBox checkBox)
            {
                if (checkBox.Name.Contains("Success"))
                {
                    somePanel = SuccessBuilds;
                }
                if (checkBox.Name.Contains("Process"))
                {
                    somePanel = ProcessBuilds;
                }

                if (checkBox.IsChecked == true)
                {
                    foreach (CheckBox box in somePanel.Children)
                    {
                        box.IsChecked = true;
                    }
                }
                else
                {
                    foreach (CheckBox box in somePanel.Children)
                    {
                        box.IsChecked = false;
                    }
                }
            }
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {

        }

        private void StartDeploy(object sender, RoutedEventArgs e)
        {

        }
    }
}
