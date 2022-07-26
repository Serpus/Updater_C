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
using System.ComponentModel;
using Newtonsoft.Json;
using Updater.CustomElements;
using System.Threading;
using Notifications.Wpf;
using System.Diagnostics;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class PrepareBuildsWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        private BackgroundWorker worker = new BackgroundWorker();

        bool loading;

        public PrepareBuildsWindow()
        {
            InitializeComponent();
            this.Closing += cancelClosing;

            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;

            Version.Content = $"v. {Data.GetVersion()}";
        }

        private void cancelClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (Data.IsCloseProgram)
            {
                e.Cancel = false;
            }
            this.Visibility = Visibility.Hidden;
        }

        public void PrepareBuilds()
        {
            Log.Info("Ветка: " + Data.branchName);
            Log.Info("---Обработка билд-планов---");

            foreach (Project project in Data.projects)
            {
                if (project.planKey == null)
                {
                    Log.Error("Не удаётся найти данные для проекта " + project.name);
                    continue;
                }

                Log.Info(project.name);
                string url = $"https://ci-sel.dks.lanit.ru/rest/api/latest/plan/{project.planKey.key}/branch";
                string result = Requests.getRequest(url);
                
                if (result == null)
                {
                    Log.Info($"result of {project.planKey.key} is null");
                    continue;
                }

                BranchList branchList = JsonConvert.DeserializeObject<BranchList>(result);
                foreach (Branch branch in branchList.branches.branch)
                {
                    if (branch.shortName.Equals(Data.branchName))
                    {
                        project.branch = branch;
                    }
                }
                Log.Info("-");
            }
        }

        public void settingBranchInList()
        {
            branchName.Content = Data.branchName;
            Log.Info("--- Отобранные билд-планы ---");
            foreach (Project project in Data.projects)
            {
                if (project.branch == null || !project.branch.shortName.Equals(Data.branchName))
                {
                    continue;
                }
                Log.Info(project.branch.name);

                ContextMenu cm = new ContextMenu();
                ResultMenuItem menuItem = new ResultMenuItem()
                {
                    Header = "Открыть в браузере",
                    ResultUrl = $"https://ci-sel.dks.lanit.ru/browse/{project.branch.key}",
                };
                menuItem.Click += OpenCurrentBuild;
                cm.Items.Add(menuItem);

                if (project.planKey.key.Contains("EIS"))
                {
                    ProjectCheckBox checkBox1 = new ProjectCheckBox();
                    checkBox1.Project = project;
                    checkBox1.Content = project.branch.name;
                    checkBox1.ContextMenu = cm;
                    fcsBuilds.Children.Add(checkBox1);
                    continue;
                }

                if (project.planKey.key.Contains("LKP"))
                {
                    ProjectCheckBox checkBox1 = new ProjectCheckBox();
                    checkBox1.Project = project;
                    checkBox1.Content = project.branch.name;
                    checkBox1.ContextMenu = cm;
                    lkpBuilds.Children.Add(checkBox1);
                    continue;
                }

                if (project.planKey.key.Contains("EPZ"))
                {
                    ProjectCheckBox checkBox1 = new ProjectCheckBox();
                    checkBox1.Project = project;
                    checkBox1.Content = project.branch.name;
                    checkBox1.ContextMenu = cm;
                    epzBuilds.Children.Add(checkBox1);
                    continue;
                }

                ProjectCheckBox checkBox = new ProjectCheckBox();
                checkBox.Project = project;
                checkBox.Content = project.branch.name;
                checkBox.ContextMenu = cm;
                otherBuilds.Children.Add(checkBox);
            }

            setEnableDisableCheckBoxes();
            Data.IsPrepareBuildsDone = true;
            Log.Info("--- *** ---");
        }

        private void setEnableDisableCheckBoxes()
        {
            fcsGroupCheckBox.IsEnabled = fcsBuilds.Children.Count > 0;

            lkpGroupCheckBox.IsEnabled = lkpBuilds.Children.Count > 0;

            epzGroupCheckBox.IsEnabled = epzBuilds.Children.Count > 0;

            otherGroupCheckBox.IsEnabled = otherBuilds.Children.Count > 0;
        }

        private void CancelPrepareBuildsWindow(object sender, RoutedEventArgs e)
        {
            //fcsBuilds.Children.Clear();
            //lkpBuilds.Children.Clear();
            //epzBuilds.Children.Clear();
            //otherBuilds.Children.Clear();
            Log.Info("Закрываем окно с отобранными билд-планами");
            this.Visibility = Visibility.Hidden;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            StackPanel somePanel = null;

            if (sender is CheckBox checkBox)
            {
                if (checkBox.Name.Contains("fcs"))
                {
                    somePanel = fcsBuilds;
                }
                if (checkBox.Name.Contains("lkp"))
                {
                    somePanel = lkpBuilds;
                }
                if (checkBox.Name.Contains("epz"))
                {
                    somePanel = epzBuilds;
                }
                if (checkBox.Name.Contains("other"))
                {
                    somePanel = otherBuilds;
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

        public void setCheckedBoxesInData()
        {
            List<ProjectCheckBox> boxes = new List<ProjectCheckBox>();

            foreach (ProjectCheckBox box in fcsBuilds.Children)
            {
                if (box.IsChecked == true)
                {
                    boxes.Add(box);
                }
            }

            foreach (ProjectCheckBox box in lkpBuilds.Children)
            {
                if (box.IsChecked == true)
                {
                    boxes.Add(box);
                }
            }

            foreach (ProjectCheckBox box in epzBuilds.Children)
            {
                if (box.IsChecked == true)
                {
                    boxes.Add(box);
                }
            }

            foreach (ProjectCheckBox box in otherBuilds.Children)
            {
                if (box.IsChecked == true)
                {
                    boxes.Add(box);
                }
            }

            Data.checkedBoxes = boxes;
        }
        private bool SuccessNotification = false;
        private bool FailedNotification = false;
        private void StartBuilds(object sender, RoutedEventArgs e)
        {
            setCheckedBoxesInData();
            if (Data.checkedBoxes.Count == 0)
            {
                Log.Info("Не выбрали ни одного билда для запуска");
                MessageBox.Show("Выберите хотя бы один билд-план для запуска сборки");
                return;
            }
            Log.Info("---Старт билдов---");

            SuccessNotification = SuccessBuildNotif.IsChecked.Value;
            FailedNotification = FailedBuildNotif.IsChecked.Value;

            worker.RunWorkerAsync();
        }

        public void CleanBranchesCheckBoxes()
        {
            fcsBuilds.Children.Clear();
            lkpBuilds.Children.Clear();
            epzBuilds.Children.Clear();
            otherBuilds.Children.Clear();
        }





        private void startLoading()
        {
            LoadingGrid.Visibility = Visibility.Visible;
            loading = true;
        }

        private void stopLoading()
        {
            LoadingGrid.Visibility = Visibility.Hidden;
            loading = false;
        }

        private async void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<Project> startedBuilds = new List<Project>(); 

            worker.ReportProgress(1);

            foreach (ProjectCheckBox checkBox in Data.checkedBoxes)
            {
                string startBuildUrl = $"https://ci-sel.dks.lanit.ru/rest/api/latest/queue/{checkBox.Project.branch.key}";
                Log.Info("request: " + startBuildUrl);
                string result = await Requests.postRequestAsync(startBuildUrl);
                // Пример ответа:
                // {"planKey":"EIS-EISBTKWF42","buildNumber":17,"buildResultKey":"EIS-EISBTKWF42-17","triggerReason":"Manual build",
                // "link":{"href":"https://ci-sel.dks.lanit.ru/rest/api/latest/result/EIS-EISBTKWF42-17","rel":"self"}}
                StartingBuildResult buildResult = JsonConvert.DeserializeObject<StartingBuildResult>(result);
                checkBox.Project.startingBuildResult = buildResult;
                if (buildResult.buildResultkey == null)
                {
                    Log.Error($"{checkBox.Project.branch.key}({checkBox.Project.name}): Build resilt key is null");
                    continue;
                }
                startedBuilds.Add(checkBox.Project);
                if (SuccessNotification || FailedNotification)
                {
                    CreateBuildNotification(buildResult.buildResultkey, checkBox.Project.name);
                }
                Log.Info("---");
            }

            Data.startedBuilds = startedBuilds;
            Data.IsBuildsStarted = true;
        }

        private void CreateBuildNotification(string key, string projectName)
        {
            Log.Info("Create build notification for " + key);
            CheckStatusWorker getStatusWorker = new CheckStatusWorker();
            getStatusWorker.DoWork += GetStatusWorker_DoWork;
            getStatusWorker.RunWorkerCompleted += GetStatusWorker_RunWorkerCompleted;
            getStatusWorker.Key = key;
            getStatusWorker.Link = "https://ci-sel.dks.lanit.ru/browse/";
            getStatusWorker.RequestLink = "https://ci-sel.dks.lanit.ru/rest/api/latest/result/";
            getStatusWorker.StatusType = "Статус билда " + projectName;
            getStatusWorker.SuccessMessage = "Билд успешный";
            getStatusWorker.FailedMessage = "Билд упал";
            getStatusWorker.RunWorkerAsync();
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!loading)
            {
                startLoading();
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        private void PrepareBuildsGeneralSettings(object sender, MouseEventArgs e)
        {
            if (Data.IsBuildsStarted)
            {
                stopLoading();
                this.Close();
            }
        }

        private void OpenCurrentBuild(object sender, RoutedEventArgs e)
        {
            if (sender is ResultMenuItem Result)
            {
                Process.Start(Result.ResultUrl);
            }
        }

        private void GetStatusWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CheckStatusWorker worker = sender as CheckStatusWorker;
            var notificationManager = new NotificationManager();

            NotificationType notifType = NotificationType.Error;
            string message = worker.FailedMessage;
            string title = worker.StatusType;

            if ((worker.Status.Equals("Successful") || worker.Status.Equals("SUCCESS")) & SuccessNotification)
            {
                notifType = NotificationType.Success;
                message = worker.SuccessMessage;

                Log.Info($"Показ уведомления: {title} - {notifType}. worker.Status: {worker.Status}. success notif: {SuccessNotification}");

                notificationManager.Show(new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = notifType,
                },
                expirationTime: TimeSpan.FromSeconds(30),
                onClick: () =>
                {
                    Process.Start(worker.Link + worker.Key);
                });
            }

            if (!(worker.Status.Equals("Successful") || worker.Status.Equals("SUCCESS")) & FailedNotification)
            {
                notifType = NotificationType.Error;
                message = worker.FailedMessage;

                Log.Info($"Показ уведомления: {title} - {notifType}. worker.Status: {worker.Status}. failed notif: {FailedNotification}");

                notificationManager.Show(new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = notifType,
                },
                expirationTime: TimeSpan.FromSeconds(30),
                onClick: () =>
                {
                    Process.Start(worker.Link + worker.Key);
                });
            }
        }

        private void GetStatusWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            CheckStatusWorker worker = sender as CheckStatusWorker;
            string status;
            do
            {
                Thread.Sleep(5000);
                string result = Requests.getSilenceRequest(worker.RequestLink + worker.Key);
                BuildStatus buildStatus = JsonConvert.DeserializeObject<BuildStatus>(result);
                status = buildStatus.state;
            } while (status.Equals("Unknown") || status.Equals("UNKNOWN"));
            worker.Status = status;
        }
    }
}
