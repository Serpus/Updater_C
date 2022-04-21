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

namespace Updater.MO
{
    public partial class PrepareNoBuildsDeployWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private BackgroundWorker preapareBuildsWorker = new BackgroundWorker();
        private BackgroundWorker startDeployWorker = new BackgroundWorker();

        private bool loading = false;
        public PrepareNoBuildsDeployWindow()
        {
            InitializeComponent();

            startDeployWorker.WorkerReportsProgress = true;
            startDeployWorker.WorkerSupportsCancellation = true;
            startDeployWorker.DoWork += startDeploy_DoWork;
            startDeployWorker.ProgressChanged += startDeploy_ProgressChanged;
            startDeployWorker.RunWorkerCompleted += startDeploy_RunWorkerCompleted;

            preapareBuildsWorker.WorkerSupportsCancellation = true;
            preapareBuildsWorker.WorkerReportsProgress = true;
            preapareBuildsWorker.DoWork += PreapareBuildsWorker_DoWork;
            preapareBuildsWorker.ProgressChanged += PreapareBuildsWorker_ProgressChanged;
            preapareBuildsWorker.RunWorkerCompleted += PreapareBuildsWorker_RunWorkerCompleted;

            preapareBuildsWorker.RunWorkerAsync();
        }

        public void SetBuilds()
        {
            Data.startedBuilds.Sort(CompareProjects.CompareBranch);
            foreach (Project project in Data.startedBuilds)
            {
                ProjectCheckBox checkBox = new ProjectCheckBox();
                checkBox.Project = project;
                checkBox.Content = project.branch.name;

                if (project.buildStatus == null)
                {
                    TextBlock textBlock = new TextBlock()
                    {
                        Text = project.branch.name,
                    };
                    checkBox.IsEnabled = false;
                    NoBuilds.Children.Add(textBlock);
                    continue;
                }

                if (project.buildStatus.state.Equals("Successful"))
                {
                    SuccessBuilds.Children.Add(checkBox);
                }
                //else if (project.buildStatus.state.Equals("Failed"))
                //{
                //    checkBox.IsEnabled = false;
                //    ProcessBuilds.Children.Add(checkBox);
                //}

                //if (project.buildStatus.state.Equals("In Progress") | project.buildStatus.state.Equals("Unknown"))
                //{
                //    checkBox.IsEnabled = false;
                //    ProcessBuilds.Children.Add(checkBox);
                //}
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
                //if (checkBox.Name.Contains("Process"))
                //{
                //    somePanel = ProcessBuilds;
                //}

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
            Data.startedBuilds = null;
            this.Close();
        }

        private void StartDeploys(object sender, RoutedEventArgs e)
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
            Cancel(sender, e);
        }

        private async Task<StartingDeployResult> Deploy(Stand standBuild, Project p)
        {
            Log.Info("Билд: " + p.branch.name + ". Стенд: " + standBuild.Name);
            /* request bodyJson example
             * String json = "{'planResultKey':'EIS-EISRDIKWF40-14'," +
                                "'name':'release-11.0.0-14'," +
                                "'nextVersionName':'release-11.0.0-15'}";*/
            CreateVersionBody body = new CreateVersionBody()
            {
                planResultKey = p.startingBuildResult.buildResultkey,
                name = p.branch.shortName + "-" + p.startingBuildResult.buildNumber + "-" + standBuild.Name,
                nextVersionName = p.branch.shortName + "-" + (p.startingBuildResult.buildNumber + 1) + "-" + standBuild.Name
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

            return startingDeployResult;
        }




        private string message = "";

        private void PreapareBuildsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Data.startedBuilds = new List<Project>();
            preapareBuildsWorker.ReportProgress(1);
            Log.Info("Отбираем из бамбу проекты с веткой " + Data.branchName);
            foreach(Project project in Data.projects)
            {
                message = "Отбираем из бамбу проекты с веткой " + Data.branchName;
                if (project.planKey.key.Contains("EPZ-"))
                {
                    continue;
                }

                string url = $"https://ci-sel.dks.lanit.ru/rest/api/latest/plan/{project.planKey.key}/branch";
                string result = Requests.getRequest(url);

                BranchList branchList = JsonConvert.DeserializeObject<BranchList>(result);
                foreach (Branch branch in branchList.branches.branch)
                {
                    if (branch.shortName.Equals(Data.branchName))
                    {
                        project.branch = branch;
                        Data.startedBuilds.Add(project);
                        break;
                    }
                }
            }

            foreach (Project project in Data.startedBuilds)
            {
                message = "Получаем последние билды";
                if (project.branch == null)
                {
                    continue;
                }
                
                string url = $"https://ci-sel.dks.lanit.ru/rest/api/latest/result/{project.branch.key}-latest";
                string result = Requests.getRequest(url);

                if (result == null)
                {
                    Log.Info("У билд-плана: " + project.name + " отсутствуют сборки - https://ci-sel.dks.lanit.ru/browse/" + project.branch.key);
                    continue;
                }

                BuildStatus buildStatus = JsonConvert.DeserializeObject<BuildStatus>(result);
                Log.Info("Билд-план: " + project.name + " / Ветка: " + project.branch.shortName + " / Последний билд: " + buildStatus.buildResultKey + " - " + buildStatus.state);
                project.buildStatus = buildStatus;
            }
        }
        private void PreapareBuildsWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            startLoading(message);
        }
        private void PreapareBuildsWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetBuilds();
            stopLoading();
            Log.Info("--- *** ---");
        }

        public void startDeploy_DoWork(object sender, DoWorkEventArgs e)
        {
            Data.startedDeploys = new List<StartedDeploy>();
            startDeployWorker.ReportProgress(1);
            Log.Info("--- Начало деплоев ---");
            foreach (PreparedDeploy deploy in Data.preparedDeploy)
            {
                StartingDeployResult depRes = Deploy(deploy.StandEnvironment, deploy.Project).Result;
                StartedDeploy startedDeploy = new StartedDeploy()
                {
                    DeployResult = depRes,
                    Project = deploy.Project,
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
            Log.Info("--- *** ---");
        }

        public void startLoading()
        {
            LoadingGrid.Visibility = Visibility.Visible;
            loading = true;
        }
        public void startLoading(string message)
        {
            Message.Content = message;
            if (!loading) {
                LoadingGrid.Visibility = Visibility.Visible;
                loading = true;
            }
        }
        public void stopLoading()
        {
            LoadingGrid.Visibility = Visibility.Hidden;
            loading = false;
        }
    }
}
