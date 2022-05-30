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
using Updater.CustomElements;
using Updater.ProjectParser.BuildParser;

namespace Updater.MO
{
    /// <summary>
    /// Логика взаимодействия для PrepareEpzBdWindow.xaml
    /// </summary>
    public partial class PrepareOpWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private bool loading = false;
        private BuildResultsInBranch BuildStatus;

        private BackgroundWorker preapareBuildsWorker = new BackgroundWorker();
        private BackgroundWorker startDeployWorker = new BackgroundWorker();
        internal Project SelectedProject;
        public PrepareOpWindow()
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

            Version.Content = $"v. {Data.GetVersion()}";
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
            DataOp.startedBuilds = null;
            this.DialogResult = false;
            this.Close();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            DataOp.startedBuilds = null;
            this.DialogResult = true;
            this.Close();
        }

        public void SetBuilds()
        {
            if (SelectedProject.branch == null)
            {
                MessageBox.Show("У билд-плана: " + SelectedProject.name + " отсутствует ветка " + DataOp.branchName, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DataOp.startedBuilds = null;
                this.Close();
                return;
            }
            if (BuildStatus == null)
            {
                MessageBox.Show("У билд-плана: " + SelectedProject.name + " отсутствуют сборки - https://ci-sel.dks.lanit.ru/browse/" + SelectedProject.branch.key, "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (BuildStatus buildStatus in BuildStatus.Results.Result)
            {
                ContextMenu cm = new ContextMenu();
                ResultMenuItem menuItem = new ResultMenuItem()
                {
                    Header = "Открыть в браузере",
                    ResultUrl = $"https://ci-sel.dks.lanit.ru/browse/{buildStatus.buildResultKey}",
                };
                menuItem.Click += OpenCurrentBuild;
                cm.Items.Add(menuItem);

                ProjectCheckBox checkBox = new ProjectCheckBox
                {
                    Project = SelectedProject,
                    Content = SelectedProject.name + " #" + buildStatus.buildNumber + " - " + buildStatus.state,
                    ContextMenu = cm
                };
                checkBox.Click += CheckBox_Click1;

                if (buildStatus.state.Equals("Successful"))
                {
                    SuccessBuilds.Children.Add(checkBox);
                }
            }

            String standsList = "";
            foreach (Stand stand in DataOp.selectedStands)
            {
                standsList += stand.Name + ", ";
            }
            standsList = standsList.Substring(0, standsList.Length - 2);
            standNames.Content = standsList;
        }

        private void CheckBox_Click1(object sender, RoutedEventArgs e)
        {
            if (sender is ProjectCheckBox checkBox)
            {
                if (checkBox.IsChecked.Value)
                {
                    foreach (ProjectCheckBox pcb in SuccessBuilds.Children)
                    {
                        if (!pcb.IsChecked.Value)
                        {
                            pcb.IsEnabled = false;
                        }
                    }
                } else
                {
                    checkBox.IsChecked = false;
                    foreach (ProjectCheckBox pcb in SuccessBuilds.Children)
                    {
                        pcb.IsEnabled = true;
                    }
                }
                
            }
        }

        private void StartDeploys(object sender, RoutedEventArgs e)
        {
            DataOp.preparedDeploy = new List<PreparedDeploy>();

            foreach (Stand stand in DataOp.selectedStands)
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
                            DataOp.preparedDeploy.Add(deploy);
                        }
                    }
                }
            }
            startDeployWorker.RunWorkerAsync();
            Close(sender, e);
        }

        private async Task<StartingDeployResult> Deploy(Stand standBuild, Project p)
        {
            Log.Info("ЕПЗ: Билд: " + p.branch.name + ", Стенд: " + standBuild.Name);
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
            Log.Info("ЕПЗ: deployUrl: " + deployUrl);
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
            DataOp.startedBuilds = new List<Project>();
            preapareBuildsWorker.ReportProgress(1);
            Log.Info("ЕПЗ: Отбираем из бамбу проекты с веткой " + DataOp.branchName);
            message = "Отбираем из бамбу проекты с веткой " + DataOp.branchName;

            string url = $"https://ci-sel.dks.lanit.ru/rest/api/latest/plan/{SelectedProject.planKey.key}/branch";
            string result = Requests.getRequest(url);

            BranchList branchList = JsonConvert.DeserializeObject<BranchList>(result);
            foreach (Branch branch in branchList.branches.branch)
            {
                if (branch.shortName.Equals(DataOp.branchName))
                {
                    SelectedProject.branch = branch;
                    break;
                }
            }

            if (SelectedProject.branch == null)
            {
                return;
            }

            url = $"https://ci-sel.dks.lanit.ru/rest/api/latest/result/{SelectedProject.branch.key}";
            result = Requests.getRequest(url);

            if (result == null)
            {
                return;
            }

            BuildStatus = JsonConvert.DeserializeObject<BuildResultsInBranch>(result);
        }
        private void PreapareBuildsWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            startLoading(message);
        }
        private void PreapareBuildsWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetBuilds();
            stopLoading();
            Log.Info("ЕПЗ: --- *** ---");
        }

        public void startDeploy_DoWork(object sender, DoWorkEventArgs e)
        {
            DataOp.startedDeploys = new List<StartedDeploy>();
            startDeployWorker.ReportProgress(1);
            Log.Info("ЕПЗ: --- Начало деплоев ---");
            foreach (PreparedDeploy deploy in DataOp.preparedDeploy)
            {
                StartingDeployResult depRes = Deploy(deploy.StandEnvironment, deploy.Project).Result;
                StartedDeploy startedDeploy = new StartedDeploy()
                {
                    DeployResult = depRes,
                    Project = deploy.Project,
                    Stand = deploy.StandEnvironment
                };
                DataOp.startedDeploys.Add(startedDeploy);
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
            Log.Info("ЕПЗ: --- *** ---");
        }

        public void startLoading()
        {
            LoadingGrid.Visibility = Visibility.Visible;
            loading = true;
        }
        public void startLoading(string message)
        {
            Message.Content = message;
            if (!loading)
            {
                LoadingGrid.Visibility = Visibility.Visible;
                loading = true;
            }
        }
        public void stopLoading()
        {
            LoadingGrid.Visibility = Visibility.Hidden;
            loading = false;
        }
        private void OpenCurrentBuild(object sender, RoutedEventArgs e)
        {
            if (sender is ResultMenuItem)
            {
                ResultMenuItem Result = (ResultMenuItem)sender;
                System.Diagnostics.Process.Start(Result.ResultUrl);
            }
        }
    }
}
