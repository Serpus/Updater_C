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
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public PrepareDeployWindow()
        {
            InitializeComponent();
            this.Closing += cancelClosing;
        }

        public void SetBuilds()
        {
            foreach (Project project in Data.startedBuilds)
            {
                ProjectCheckBox checkBox = new ProjectCheckBox();
                checkBox.Project = project;
                checkBox.Content = project.branch.name;

                if (project.buildStatus.state.Equals("Successful"))
                {
                    SuccessBuilds.Children.Add(checkBox);
                }
                else if (project.buildStatus.state.Equals("Failed"))
                {
                    checkBox.IsEnabled = false;
                    ProcessBuilds.Children.Add(checkBox);
                }

                if (project.buildStatus.state.Equals("In Progress") | project.buildStatus.state.Equals("Unknown"))
                {
                    checkBox.IsEnabled = false;
                    ProcessBuilds.Children.Add(checkBox);
                }
            }
        }

        private void cancelClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (Data.IsCloseProgram)
            {
                e.Cancel = false;
            }
            SuccessBuilds.Children.Clear();
            ProcessBuilds.Children.Clear();
            Log.Info("Закрываем окно подготовки деплоев");
            this.Visibility = Visibility.Hidden;
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
            SuccessBuilds.Children.Clear();
            ProcessBuilds.Children.Clear();
            Log.Info("Закрываем окно подготовки деплоев");
            this.Visibility = Visibility.Hidden;
        }

        private void StartDeploy(object sender, RoutedEventArgs e)
        {

        }
    }
}
