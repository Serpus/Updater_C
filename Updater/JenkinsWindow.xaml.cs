using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
using Newtonsoft.Json;
using Notifications.Wpf;
using System.Diagnostics;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для JenkinsWindow.xaml
    /// </summary>
    public partial class JenkinsWindow : Window
    {
        public bool loading = false;
        private Job branchListLocal = new Job();

        private BackgroundWorker getJobsWorker = new BackgroundWorker();
        private BackgroundWorker getBranchesWorker = new BackgroundWorker();
        private BackgroundWorker startDeployWorker = new BackgroundWorker();
        private BackgroundWorker refreshStatusWorker = new BackgroundWorker();

        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        public JenkinsWindow()
        {
            InitializeComponent();

            getJobsWorker.WorkerReportsProgress = true;
            getJobsWorker.WorkerSupportsCancellation = true;
            getJobsWorker.DoWork += getJobs_DoWork;
            getJobsWorker.ProgressChanged += getJobs_ProgressChanged;
            getJobsWorker.RunWorkerCompleted += getJobs_RunWorkerCompleted;

            getBranchesWorker.WorkerReportsProgress = true;
            getBranchesWorker.WorkerSupportsCancellation = true;
            getBranchesWorker.DoWork += getBranches_DoWork;
            getBranchesWorker.ProgressChanged += getBranches_ProgressChanged;
            getBranchesWorker.RunWorkerCompleted += getBranches_RunWorkerCompleted;

            startDeployWorker.WorkerReportsProgress = true;
            startDeployWorker.WorkerSupportsCancellation = true;
            startDeployWorker.DoWork += startDeploy_DoWork;
            startDeployWorker.ProgressChanged += startDeploy_ProgressChanged;
            startDeployWorker.RunWorkerCompleted += startDeploy_RunWorkerCompleted;

            refreshStatusWorker.WorkerReportsProgress = true;
            refreshStatusWorker.WorkerSupportsCancellation = true;
            refreshStatusWorker.DoWork += refreshStatus_DoWork;
            refreshStatusWorker.ProgressChanged += refreshStatus_ProgressChanged;
            refreshStatusWorker.RunWorkerCompleted += refreshStatus_RunWorkerCompleted;

            Version.Content = $"v. {Data.GetVersion()}";
        }

        private void JenkinsWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            if (Data.IsCloseProgram)
            {
                e.Cancel = false;
            }
            this.Visibility = Visibility.Hidden;
            Log.Info("Закрываем окно Дженкинса");
        }

        /**
         * Получаем все джобы для выбранного реестра
         */
        private void GetJobs(object sender, RoutedEventArgs e)
        {
            if (BranchName.Text.Equals(""))
            {
                MessageBox.Show("Введите название ветки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (sender is ProjectButton projectButton)
            {
                projectButton = (ProjectButton)sender;
                DataJenkins.ProjectName = projectButton.ProjectName;
                projectButton.IsEnabled = false;
            } else
            {
                Log.Info("sender in GetJobs method is not ProjectButton");
                MessageBox.Show("sender in GetJobs method is not ProjectButton");
                return;
            }
            getJobsWorker.RunWorkerAsync();
        }

        /**
         * Создание чекбоксов в части окна (с реестрами)
         */
        public void CreateProjectCheckBoxes()
        {
            getBranchesWorker.RunWorkerAsync();
        }

        /**
         * Отметить все чекбоксы
         */
        private void CheckAll(object sender, RoutedEventArgs e)
        {
            foreach(var element in jobsRegisterStackPanel.Children)
            {
                if (element is JobCheckBox checkBox)
                {
                    if (checkBox.IsEnabled)
                    {
                        checkBox.IsChecked = true;
                    }
                }
            }
            Log.Info("Отмечены все чекбоксы");
        }

        /**
         * Снять все чекбоксы
         */
        private void UncheckAll(object sender, RoutedEventArgs e)
        {
            foreach (var element in jobsRegisterStackPanel.Children)
            {
                if (element is JobCheckBox checkBox) {
                    checkBox.IsChecked = false;
                }
            }
            Log.Info("Все чекбоксы сняты");
        }

        /**
         * Сброс выбранного проекта
         */
        private void ResetProject(object sender, RoutedEventArgs e)
        {
            Log.Info("Сброс выбранных проекток");
            DataJenkins.Registers = null;
            SelectedBranchName.Text = "";
            jobsRegisterStackPanel.Children.Clear();
            BuildStatusTabs.Items.Clear();
            BranchHint.Visibility = Visibility.Hidden;
            ConfirmButton.IsEnabled = false;
            BuildStatusGrid.IsEnabled = false;
            BranchName.IsEnabled = true;
            ProjectStackPanel.IsEnabled = true;
            RegisterList.IsEnabled = true;
            CheckAllButton.IsEnabled = true;
            UncheckAllButton.IsEnabled= true;

            foreach (var obj in ProjectStackPanel.Children)
            {
                if (obj is Button)
                {
                    Button button = (Button)obj;
                    button.IsEnabled = true;
                }

                if (obj is StackPanel)
                {
                    StackPanel stackPanel = (StackPanel)obj;
                    foreach (var item in stackPanel.Children)
                    {
                        if (item is CheckBox)
                        {
                            CheckBox checkBox = (CheckBox)item;
                            checkBox.IsChecked = false;
                        }
                    }
                }
                
            }
        }

        /**
         * Начинаем деплой
         */
        private bool SuccessNotificationValue = false;
        private bool FailedNotificationValue = false;
        private void Confirm(object sender, RoutedEventArgs e)
        {
            // Сетим значения чекбоксов с уведомлениями в переменную
            SuccessNotificationValue = SuccessBuildNotif.IsChecked.Value;
            FailedNotificationValue = FailedBuildNotif.IsChecked.Value;

            // Проверяем, что отмечен хотя бы один чекбокс с реестром
            int i = 0;
            foreach (var element in jobsRegisterStackPanel.Children)
            {
                if (element is CheckBox checkBox)
                {
                    i++;
                    if (checkBox.IsChecked.Value)
                    {
                        break;
                    }
                    if (i == jobsRegisterStackPanel.Children.Count)
                    {
                        MessageBox.Show("Необходимо отметить хотя бы один чекбокс");
                        return;
                    }
                }
            }

            // Проверяем, что выбран хотя бы один стенд
            i = 0;
            foreach (var element in StandCheckBoxes.Children)
            {
                if (element is CheckBox checkBox)
                {
                    i++;
                    if (checkBox.IsChecked.Value)
                    {
                        break;
                    }
                    if (i == StandCheckBoxes.Children.Count)
                    {
                        MessageBox.Show("Необходимо выбрать хотя бы один стенд");
                        return;
                    }
                }
            }

            // Запускаем сборки
            ConfirmMo confirm = new ConfirmMo();
            confirm.ShowDialog();
            if (confirm.DialogResult.Value)
            {
                Log.Info("--- Начинаем деплоить ---");
                DataJenkins.DeployEnvironments = new List<DeployEnvironment>();
                foreach (CheckBox checkBox in StandCheckBoxes.Children)
                {
                    if (checkBox.IsChecked.Value)
                    {
                        foreach (var element in jobsRegisterStackPanel.Children)
                        {
                            if (element is JobCheckBox regCB)
                            {
                                if (regCB.IsChecked.Value)
                                {
                                    DeployEnvironment de = new DeployEnvironment()
                                    {
                                        RegisterName = regCB.Content.ToString(),
                                        Project = regCB.JobProject,
                                        Branch = HttpUtility.UrlEncode(BranchName.Text),
                                        Stand = checkBox.Content.ToString(),
                                        SKIP_DB = SKIP_DB.IsChecked.Value.ToString().ToLower()
                                    };
                                    de.Branch = HttpUtility.UrlEncode(de.Branch).Replace("%252f", "%252F");
                                    DataJenkins.DeployEnvironments.Add(de);
                                }
                            }
                            
                        }
                    }
                }


                // Код для теста. Эмулирует отмеченные чекбоксы. Далее по коду нужно отключить запрос начала деплоя
                //DataJenkins.DeployEnvironments.Add(new DeployEnvironment()
                //{
                //    RegisterName = "psk",
                //    Project = "priv",
                //    Branch = "develop",
                //    Stand = "eis3.lanit.ru",
                //});
                //DataJenkins.DeployEnvironments.Add(new DeployEnvironment()
                //{
                //    RegisterName = "audit",
                //    Project = "priv",
                //    Branch = "develop",
                //    Stand = "eis3.lanit.ru",
                //});
                //DataJenkins.DeployEnvironments.Add(new DeployEnvironment()
                //{
                //    RegisterName = "rec",
                //    Project = "priv",
                //    Branch = "develop",
                //    Stand = "eis3.lanit.ru",
                //});

                RegisterList.IsEnabled = false;
                ConfirmButton.IsEnabled = false;
                BuildStatusGrid.IsEnabled = true;
                CheckAllButton.IsEnabled = false;
                UncheckAllButton.IsEnabled = false;
                startDeployWorker.RunWorkerAsync(); // Отключаем для дебага
            }
        }

        private void RefreshStatus(object sender, RoutedEventArgs e)
        {
            Log.Info("Обновляем статусы запущенных билдов");
            try
            {
                refreshStatusWorker.RunWorkerAsync();
            } catch (Exception ex)
            {
                Log.Info(ex.Message);
            }
        }

        private void setBuildResultInUi()
        {
            if (DataJenkins.BuildResults == null)
            {
                MessageBox.Show("Результаты билдов отсутствуют");
                return;
            }

            HashSet<string> StandSet = new HashSet<string>();
            List<BuildStatusLabel> labelList = new List<BuildStatusLabel>();

            BuildStatusTabs.Items.Clear();

            foreach (BuildResult result in DataJenkins.BuildResults)
            {
                ContextMenu cm = new ContextMenu();
                ResultMenuItem menuItem = new ResultMenuItem() 
                {
                    Header = "Открыть в браузере", 
                    ResultUrl = result.Url
                };
                menuItem.Click += OpenCurrentBuild;
                cm.Items.Add(menuItem);
                BuildStatusLabel label = new BuildStatusLabel()
                {
                    Content = result.FullDisplayName + " - " + result.Result,
                    BuildResult = result,
                    ContextMenu = cm,
                };

                if (label.BuildResult.Result == null)
                {
                    label.Content = result.FullDisplayName + " - В процессе";
                } else 
                {
                    if (label.BuildResult.Result.Equals("SUCCESS"))
                    {
                        label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#12BF0F");
                    }
                    else if (label.BuildResult.Result.Equals("FAILURE"))
                    {
                        label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#DF0E0E");
                    }
                }
                

                StandSet.Add(label.BuildResult.getStand());
                labelList.Add(label);
            }

            foreach (string stand in StandSet)
            {
                ListBox listBox = new ListBox();
                foreach (BuildStatusLabel label in labelList)
                {
                    if (label.BuildResult.getStand().Equals(stand))
                    {
                        listBox.Items.Add(label);
                    }
                }
                BuildStatusTabs.Items.Add(new TabItem { Header = stand, Content = listBox, IsSelected = true });
            }
        }

        private void OpenCurrentBuild(object sender, RoutedEventArgs e)
        {
            if (sender is ResultMenuItem)
            {
                ResultMenuItem Result = (ResultMenuItem)sender;
                System.Diagnostics.Process.Start(Result.ResultUrl);
            }
        }

        /**
         * Открываем билды в браузере
         */
        private void OpenBuildsInBrowser(object sender, RoutedEventArgs e)
        {
            foreach (TabItem item in BuildStatusTabs.Items)
            {
                if (!item.IsSelected)
                {
                    continue;
                }
                if (item.Content is ListBox)
                {
                    ListBox listBox = (ListBox)item.Content;
                    foreach (BuildStatusLabel label in listBox.Items)
                    {
                        Log.Info("Открываем все билды Дженкинса для стенда " + item.Header.ToString());
                        Process.Start(label.BuildResult.Url);
                    }
                }
            }
        }

        private void SetBranchName(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem)
            {
                MenuItem item = (MenuItem)sender;
                BranchName.Text = item.Header.ToString();
            }
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

        // Получение джобов

        public void getJobs_DoWork(object sender, DoWorkEventArgs e)
        {
            DataJenkins.Registers = new List<Register>();
            getJobsWorker.ReportProgress(1);
            var response = Requests.getRequest($"https://ci-sel.dks.lanit.ru/jenkins/job/{DataJenkins.ProjectName}/api/json?pretty=true");
            if (response == null)
            {
                MessageBox.Show("Не получилось получить пайплайны для проекта " + DataJenkins.ProjectName, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error("Не получилось получить пайплайны для проекта " + DataJenkins.ProjectName);
                return;
            }
            Jobs jobsList = JsonConvert.DeserializeObject<Jobs>(response);
            if (jobsList != null)
            {
                foreach (Job job in jobsList.jobs)
                {
                    Register register = new Register()
                    {
                        name = job.name,
                        url = job.url,
                        project = DataJenkins.ProjectName,
                        BranchList = new Jobs()
                    };
                    DataJenkins.Registers.Add(register);
                }
            } else
            {
                MessageBox.Show("Не получилось получить пайплайны для проекта " + DataJenkins.ProjectName, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error("Не получилось получить пайплайны для проекта " + DataJenkins.ProjectName);
                return;
            }
        }

        public void getJobs_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!loading)
            {
                startLoading();
            }
        }

        public void getJobs_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (DataJenkins.Registers == null)
            {
                stopLoading();
                return;
            }
            Log.Info("--- Реестры на докерах ---");
            int i = 0;
            foreach (Register register in DataJenkins.Registers)
            {
                i++;
                Log.Info(i + " job - " + register.name);
            }
            Log.Info("--- *** ---");
            CreateProjectCheckBoxes();
            SelectedBranchName.Text = "Выбранная ветка: " + BranchName.Text;
            BranchHint.Visibility = Visibility.Visible;
            BranchName.IsEnabled = false;
            stopLoading();
        }

        // Получение веток

        public void getBranches_DoWork(object sender, DoWorkEventArgs e)
        {
            getBranchesWorker.ReportProgress(1);
            foreach (Register register in DataJenkins.Registers)
            {
                Log.Info("Получачем ветки для джоба " + register.name + ", jobURL - " + register.url);
                var response = Requests.getRequest($"https://ci-sel.dks.lanit.ru/jenkins/job/{DataJenkins.ProjectName}/job/{register.name}/api/json?pretty=true");
                Jobs branchList = JsonConvert.DeserializeObject<Jobs>(response);
                if (branchList.jobs == null)
                {
                    Log.Error("Не можем найти ветки для " + register.name + ", jobURL - " + register.url);
                    Log.Info(" --- ");
                    continue;
                }
                Log.Info("Ветки: " + branchList.ToString());
                register.BranchList = branchList;
                Log.Info(" --- ");
            }
        }

        public void getBranches_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!loading)
            {
                startLoading();
            }
        }

        public void getBranches_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string project = DataJenkins.Registers[0].project;
            // Добавляем разделение для проекта
            if (project != null)
            {
                jobsRegisterStackPanel.Children.Add(new Label()
                {
                    Content = project.ToUpper(),
                    FontWeight = FontWeights.Bold
                });
            }

            // Отбор реестров с указанной веткой
            foreach (Register regJob in DataJenkins.Registers) 
            {
                JobCheckBox checkBox = new JobCheckBox()
                {
                    Content = regJob.name,
                    JobName = regJob.name,
                    JobUrl = regJob.url,
                    JobProject = regJob.project,
                    IsEnabled = false
                };

                if (regJob.BranchList == null || regJob.BranchList.jobs == null)
                {
                    Log.Error("Не удаётся найти ветки для джоба " + checkBox.Content);
                    continue;
                }

                foreach (Job branch in regJob.BranchList.jobs)
                {
                    String branchF = HttpUtility.UrlDecode(branch.name);
                    if (branchF == BranchName.Text)
                    {
                        checkBox.IsEnabled = true;
                        Log.Info("Ветка " + branchF + " найдена у джоба " + regJob.name);
                        break;
                    }
                }

                jobsRegisterStackPanel.Children.Add(checkBox);
            }
            stopLoading();
            ConfirmButton.IsEnabled = true;
        }

        // Деплой

        public void startDeploy_DoWork(object sender, DoWorkEventArgs e)
        {
            startDeployWorker.ReportProgress(1);
            foreach (DeployEnvironment de in DataJenkins.DeployEnvironments)
            {
                String url = $"https://ci-sel.dks.lanit.ru/jenkins/job/{de.Project}/job/{de.RegisterName}/job/{de.Branch}/buildWithParameters?STAND={de.Stand}&SKIP_DB={de.SKIP_DB}";
                Log.Info($"deploy \"{de.RegisterName}\" on \"{de.Stand}\" url: " + url);
                // Раскомментить для запуска сборок:
                Requests.postRequestAsyncJenkins(url);
                if (SuccessNotificationValue || FailedNotificationValue)
                {
                    Log.Info("Build notification is Enabled");
                    CreateBuildNotification(de.Project, de.RegisterName, de.Branch, de.Stand);
                }
                // Для отладки без запуска делпоя: 
                //Thread.Sleep(2000);

            }
        }
        private void CreateBuildNotification(string projectName, string registerName, string branch, string stand)
        {
            Log.Info($"Create build notification for {projectName}/{registerName}/{branch}");
            CheckStatusJenkinsWorker getStatusWorker = new CheckStatusJenkinsWorker();
            getStatusWorker.DoWork += GetStatusWorker_DoWork;
            getStatusWorker.RunWorkerCompleted += GetStatusWorker_RunWorkerCompleted;
            getStatusWorker.ProjectName = projectName;
            getStatusWorker.RegisterName = registerName;
            getStatusWorker.Branch = branch;
            getStatusWorker.Stand = stand;
            getStatusWorker.StatusType = $"Статус сборки {projectName}/{registerName}/{branch.Replace("%252F", "-")}";
            getStatusWorker.SuccessMessage = "Сборка успешна";
            getStatusWorker.FailedMessage = "Сборка упала";
            getStatusWorker.RunWorkerAsync();
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
            ProjectStackPanel.IsEnabled = false;
            MessageBox.Show("Сборки запущены");
            Log.Info("--- *** ---");
        }

        // Обновление статусов запущенных деплоев
        public void refreshStatus_DoWork(object sender, DoWorkEventArgs e)
        {
            DataJenkins.BuildResults = new List<BuildResult>();
            refreshStatusWorker.ReportProgress(1);
            foreach (DeployEnvironment de in DataJenkins.DeployEnvironments)
            {
                String url = $"https://ci-sel.dks.lanit.ru/jenkins/job/{de.Project}/job/{de.RegisterName}/job/{de.Branch}/api/json?pretty=true";
                string responseLastBuild = Requests.getRequest(url);
                BuildsList BuildsList = JsonConvert.DeserializeObject<BuildsList>(responseLastBuild);

                string responseResult = "";
                int i = 0;
                foreach (Build build in BuildsList.builds)
                {
                    i++;
                    if (i > 10)
                    {
                        break;
                    }

                    string buildUrl = build.url + "api/json?pretty=true";
                    responseResult = Requests.getRequest(buildUrl);
                    BuildResult BuildResult = JsonConvert.DeserializeObject<BuildResult>(responseResult);

                    string stand = BuildResult.getStand();
                    if (stand == null)
                    {
                        continue;
                    }
                    if (stand.Equals(de.Stand))
                    {
                        Log.Info("Найден билд - " + BuildResult.FullDisplayName + " - " + de.Stand);
                        DataJenkins.BuildResults.Add(BuildResult);
                        break;
                    }
                }
            }
        }

        public void refreshStatus_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!loading)
            {
                startLoading();
            }
        }

        public void refreshStatus_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stopLoading();
            setBuildResultInUi();
            Log.Info("--- *** ---");
        }

        private void GetStatusWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            CheckStatusJenkinsWorker worker = sender as CheckStatusJenkinsWorker;
            string status = null;
            do
            {
                Thread.Sleep(10000);
                String url = $"https://ci-sel.dks.lanit.ru/jenkins/job/{worker.ProjectName}/job/{worker.RegisterName}/job/{worker.Branch}/api/json?pretty=true";
                string responseLastBuild = Requests.getSilenceRequest(url);
                BuildsList BuildsList = JsonConvert.DeserializeObject<BuildsList>(responseLastBuild);

                string responseResult = "";
                int i = 0;
                foreach (Build build in BuildsList.builds)
                {
                    i++;
                    if (i > 10)
                    {
                        break;
                    }

                    string buildUrl = build.url + "api/json?pretty=true";
                    responseResult = Requests.getSilenceRequest(buildUrl);
                    BuildResult BuildResult = JsonConvert.DeserializeObject<BuildResult>(responseResult);

                    string stand = BuildResult.getStand();
                    if (stand == null)
                    {
                        continue;
                    }
                    if (stand.Equals(worker.Stand))
                    {
                        status = BuildResult.Result;
                        worker.Link = BuildResult.Url;
                        break;
                    }
                }
            } while (status == null);
            worker.Status = status;
        }

        private void GetStatusWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CheckStatusJenkinsWorker worker = sender as CheckStatusJenkinsWorker;
            var notificationManager = new NotificationManager();

            NotificationType notifType = NotificationType.Error;
            string message = worker.FailedMessage;
            string title = worker.StatusType;

            if (worker.Status == null || (!FailedNotificationValue & !SuccessNotificationValue))
            {
                return;
            }

            if (worker.Status.Equals("SUCCESS") & SuccessNotificationValue)
            {
                notifType = NotificationType.Success;
                message = worker.SuccessMessage;

                notificationManager.Show(new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = notifType,
                },
                expirationTime: TimeSpan.FromSeconds(30),
                onClick: () =>
                {
                    Process.Start(worker.Link);
                });
            }

            if (!worker.Status.Equals("SUCCESS") & FailedNotificationValue)
            {
                notifType = NotificationType.Error;
                message = worker.FailedMessage;

                notificationManager.Show(new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = notifType,
                },
                expirationTime: TimeSpan.FromSeconds(30),
                onClick: () =>
                {
                    Process.Start(worker.Link);
                });
            }
        }
    }
}
