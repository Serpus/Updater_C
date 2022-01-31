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
        private bool loading;

        public PrepareDeployWindow()
        {
            InitializeComponent();
            this.Closing += cancelClosing;
        }

        public void startLoading()
        {
            LoadingGrid.Visibility = Visibility.Visible;
            loading = true;
        }

        public void stopLoading()
        {
            LoadingGrid.Visibility = Visibility.Hidden;
            loading = false;
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
                Log.Info("aonfmgaoikndf");
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
            foreach (Stand stand in Data.selectedStands)
            {
                foreach (ProjectCheckBox p in SuccessBuilds.Children)
                {
                    if (p.IsChecked == false)
                    {
                        continue;
                    }

                    foreach (Stand standBuild in p.Project.stands)
                    {
                        if (standBuild.Name.Contains(stand.Name))
                        {
                            /* String json = "{'planResultKey':'EIS-EISRDIKWF40-14'," +
                                "'name':'release-11.0.0-14'," +
                                "'nextVersionName':'release-11.0.0-15'}";*/
                            String json = "{\"planResultKey\":\"" + p.Project.startingBuildResult.buildResultkey + "\"," +
                                        "\"name\":\"" + p.Project.branch.shortName + "-" + p.Project.startingBuildResult.buildNumber + "-" + stand.Name + "\"," +
                                        "\"nextVersionName\":\"" + p.Project.branch.shortName + "-" + (p.Project.startingBuildResult.buildNumber + 1) + "-" + stand.Name + "\"}";
                            Log.Info("JSON: " + json);
                            Log.Info($"https://ci-sel.dks.lanit.ru/rest/api/latest/queue/deployment?environmentId={standBuild.id}&versionId=79175346");
                        }
                    }
                }
            }
        }
    }
}
