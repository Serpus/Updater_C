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
using System.Threading;
using Updater.CustomElements;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для PrepareDeployWindow.xaml
    /// </summary>
    public partial class PrepareDeployWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        private bool loading = false;
        private BackgroundWorker startDeployWorker = new BackgroundWorker();
        private BackgroundWorker RefreshBuildStatusWorker = new BackgroundWorker();

        public PrepareDeployWindow()
        {
            InitializeComponent();

            startDeployWorker.WorkerReportsProgress = true;
            startDeployWorker.WorkerSupportsCancellation = true;
            startDeployWorker.DoWork += startDeploy_DoWork;
            startDeployWorker.ProgressChanged += startDeploy_ProgressChanged;
            startDeployWorker.RunWorkerCompleted += startDeploy_RunWorkerCompleted;

            RefreshBuildStatusWorker.WorkerReportsProgress = true;
            RefreshBuildStatusWorker.WorkerSupportsCancellation = true;
            RefreshBuildStatusWorker.DoWork += RefreshBuildStatusWorker_DoWork;
            RefreshBuildStatusWorker.ProgressChanged += RefreshBuildStatusWorker_ProgressChanged;
            RefreshBuildStatusWorker.RunWorkerCompleted += RefreshBuildStatusWorker_RunWorkerCompleted;

            RefreshBuildStatusWorker.RunWorkerAsync();

            Version.Content = $"v. {Data.GetVersion()}";
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

                if (project.branch.name.Contains("EPZ -"))
                {
                    continue;
                }

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

            String standsList = "";
            foreach (Stand stand in Data.selectedStands)
            {
                standsList += stand.Name + ", ";
            }
            standsList = standsList.Substring(0, standsList.Length - 2);
            standNames.Content = standsList;
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
            Log.Info("Закрываем окно подготовки деплоев (False)");
            this.DialogResult = false;
            this.Close();
        }

        private void CloseWindow()
        {
            SuccessBuilds.Children.Clear();
            ProcessBuilds.Children.Clear();
            Log.Info("Закрываем окно подготовки деплоев (True)");
            this.DialogResult = true;
            this.Close();
        }

        private async void StartDeploy(object sender, RoutedEventArgs e)
        {
            Data.preparedDeploy = new List<PreparedDeploy>();

            foreach (Stand stand in Data.selectedStands)
            {
                foreach (ProjectCheckBox projectCb in SuccessBuilds.Children)
                {
                    if (projectCb.IsChecked == false)
                    {
                        continue;
                    }

                    foreach (Stand standBuild in projectCb.Project.stands)
                    {
                        if (standBuild.Name.Contains(stand.Name))
                        {
                            PreparedDeploy deploy = new PreparedDeploy()
                            {
                                StandEnvironment = standBuild,
                                Project = projectCb.Project
                            };
                            Data.preparedDeploy.Add(deploy);
                        }
                    }
                }
            }

            startDeployWorker.RunWorkerAsync();
        }

        private async Task<StartingDeployResult> Deploy(Stand standBuild, Project p)
        {
            Log.Info("Билд: " + p.branch.name + ", Стенд: " + standBuild.Name);
            /* request bodyJson example
             * String json = "{'planResultKey':'EIS-EISRDIKWF40-14'," +
                                "'name':'release-11.0.0-14'," +
                                "'nextVersionName':'release-11.0.0-15'}";*/
            string standShortName = standBuild.Name.Replace("ЕИС-", "");
            CreateVersionBody body = new CreateVersionBody()
            {
                planResultKey = p.startingBuildResult.buildResultkey,
                name = p.branch.shortName + "-" + p.startingBuildResult.buildNumber + "-" + standShortName,
                nextVersionName = p.branch.shortName + "-" + (p.startingBuildResult.buildNumber + 1) + "-" + standShortName
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

            Log.Info("---");
            return startingDeployResult;
        }





        public void startDeploy_DoWork(object sender, DoWorkEventArgs e)
        {
            Data.startedDeploys = new List<StartedDeploy>();
            startDeployWorker.ReportProgress(50);
            Log.Info("--- Начало деплоев ---");
            foreach (PreparedDeploy deploy in Data.preparedDeploy)
            {
                StartingDeployResult depRes = Deploy(deploy.StandEnvironment, deploy.Project).Result;
                StartedDeploy startedDeploy = new StartedDeploy()
                {
                    DeployResult = depRes,
                    Project = deploy.Project,
                    Stand = deploy.StandEnvironment
                };
                Data.startedDeploys.Add(startedDeploy);
            }
        }

        public void startDeploy_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!loading)
            {
                startLoading();
            }
        }

        public void startDeploy_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stopLoading();
            CloseWindow();
            Log.Info("--- *** ---");
        }

        private void RefreshBuildStatusWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Log.Info("Обновляем статусы билдов");
            RefreshBuildStatusWorker.ReportProgress(50);
            foreach (Project project in Data.startedBuilds)
            {
                if (project.branch.name.Contains("EPZ -"))
                {
                    continue;
                }

                string buildResultkey = project.startingBuildResult.buildResultkey;
                string result = Requests.getRequest("https://ci-sel.dks.lanit.ru/rest/api/latest/result/" + buildResultkey);
                BuildStatus buildStatus = JsonConvert.DeserializeObject<BuildStatus>(result);
                project.buildStatus = buildStatus;
            }
        }

        private void RefreshBuildStatusWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!loading)
            {
                startLoading();
            }
        }

        private void RefreshBuildStatusWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetBuilds();
            stopLoading();
            Log.Info("--- *** ---");
        }
    }
}
