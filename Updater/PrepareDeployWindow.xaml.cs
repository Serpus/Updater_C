using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private async void StartDeploy(object sender, RoutedEventArgs e)
        {
            startLoading();

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
                        Log.Info("Стенд: " + standBuild.Name);
                        if (standBuild.Name.Contains(stand.Name))
                        {
                            Deploy(standBuild, p);
                            Data.startedDeploys.Add(p.Project);
                        }
                    }
                }
            }

            stopLoading();
            Cancel(sender, e);
        }

        private async void Deploy(Stand standBuild, ProjectCheckBox p)
        {
            /* request bodyJson example
             * String json = "{'planResultKey':'EIS-EISRDIKWF40-14'," +
                                "'name':'release-11.0.0-14'," +
                                "'nextVersionName':'release-11.0.0-15'}";*/
            CreateVersionBody body = new CreateVersionBody()
            {
                planResultKey = p.Project.startingBuildResult.buildResultkey,
                name = p.Project.branch.shortName + "-" + p.Project.startingBuildResult.buildNumber + "-" + standBuild.Name,
                nextVersionName = p.Project.branch.shortName + "-" + (p.Project.startingBuildResult.buildNumber + 1) + "-" + standBuild.Name
            };
            String response = await Requests.postRequestAsync("https://ci-sel.dks.lanit.ru/rest/api/latest/deploy/project/" + standBuild.DeploymentProjectId + "/version", body);
            /*
             * response example: 
             * {"id":82619038,
             * "name":"hotfix-12.0.4-9-ЕИС-7",
             * "creationDate":1643745388854,
             * "creatorUserName":"Kazankin",
             * "items" и так дале...
             */
            CreateVersionBodyResponse createVersionBodyResponse = JsonConvert.DeserializeObject<CreateVersionBodyResponse>(response);

            String deployUrl = $"https://ci-sel.dks.lanit.ru/rest/api/latest/queue/deployment?environmentId={standBuild.id}&versionId={createVersionBodyResponse.id}";
            Log.Info("deployUrl: " + deployUrl);
            response = await Requests.postRequestAsync(deployUrl);
            /*
             * Response example:
             * {"deploymentResultId":85399076,
             * "link":{
             *     "href":"https://ci-sel.dks.lanit.ru/rest/api/latest/deploy/result/85399076",
             *     "rel":
             *     "self" 
             *     }
             * }
             */
            StartingDeployResult startingDeployResult = JsonConvert.DeserializeObject<StartingDeployResult>(response);

            p.Project.startingDeployResult = startingDeployResult;
        }
    }
}
