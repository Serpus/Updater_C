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

            this.Closing += JenkinsWindow_Closing;

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
            ProjectButton ProjectButton = new ProjectButton();
            if (sender is ProjectButton)
            {
                ProjectButton = (ProjectButton)sender;
                DataJenkins.ProjectName = ProjectButton.ProjectName;
                ProjectButton.IsEnabled = false;
            } else
            {
                Log.Info("sender in GetJobs method is not ProjectButton");
                MessageBox.Show("sender in GetJobs method is not ProjectButton");
                return;
            }
            SelectedBranchName.Text = "Выбранная ветка: " + BranchName.Text;

            getJobsWorker.RunWorkerAsync();
        }

        /**
         * Создание чекбоксов в части окна (с реестрами)
         */
        public void CreateProjectCheckBoxes(List<Register> jobslist)
        {
            getBranchesWorker.RunWorkerAsync();
        }

        /**
         * Отметить все чекбоксы
         */
        private void CheckAll(object sender, RoutedEventArgs e)
        {
            foreach(JobCheckBox checkBox in jobsRegisterStackPanel.Children)
            {
                if (checkBox.IsEnabled)
                {
                    checkBox.IsChecked = true;
                }
            }
            Log.Info("Отмечены все чекбоксы");
        }

        /**
         * Снять все чекбоксы
         */
        private void UncheckAll(object sender, RoutedEventArgs e)
        {
            foreach (JobCheckBox checkBox in jobsRegisterStackPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            Log.Info("Все чекбоксы сняты");
        }

        /**
         * Сброс выбранного проекта
         */
        private void ResetProject(object sender, RoutedEventArgs e)
        {
            Log.Info("Сброс выбранного проекта");
            jobsRegisterStackPanel.Children.Clear();
            SelectedBranchName.Text = "";
            ProjectStackPanel.IsEnabled = true;
            RegisterList.IsEnabled = true;
            ConfirmButton.IsEnabled = true;
            BuildStatusGrid.IsEnabled = false;
            BuildStatusTabs.Items.Clear();

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
        private void Confirm(object sender, RoutedEventArgs e)
        {
            int i = 0;
            foreach (CheckBox checkBox in jobsRegisterStackPanel.Children)
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

            i = 0;
            foreach (CheckBox checkBox in StandCheckBoxes.Children)
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
                        foreach (JobCheckBox regCB in jobsRegisterStackPanel.Children)
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
                RegisterList.IsEnabled = false;
                ConfirmButton.IsEnabled = false;
                BuildStatusGrid.IsEnabled = true;
                startDeployWorker.RunWorkerAsync();
            }
        }

        private void RefreshStatus(object sender, RoutedEventArgs e)
        {
            Log.Info("Обновляем статусы запущенных билдов");
            refreshStatusWorker.RunWorkerAsync();
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
                    label.Content = result.FullDisplayName + " - В очереди, либо статус неизвестен";
                }
                if (label.BuildResult.Result.Equals("SUCCESS"))
                {
                    label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#12BF0F");
                }
                else if (label.BuildResult.Result.Equals("FAILURE"))
                {
                    label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#DF0E0E");
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
                BuildStatusTabs.Items.Add(new TabItem { Header = stand, Content = listBox });
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
                        System.Diagnostics.Process.Start(label.BuildResult.Url);
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
            getJobsWorker.ReportProgress(1);
            var response = Requests.getRequest($"https://ci-sel.dks.lanit.ru/jenkins/job/{DataJenkins.ProjectName}/api/json?pretty=true");
            Jobs jobsList = JsonConvert.DeserializeObject<Jobs>(response);
            if (jobsList != null)
            {
                DataJenkins.Registers = new List<Register>();
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
            Log.Info("--- Реестры на докерах ---");
            int i = 0;
            foreach (Register register in DataJenkins.Registers)
            {
                i++;
                Log.Info(i + " job - " + register.name);
            }
            Log.Info("--- *** ---");
            CreateProjectCheckBoxes(DataJenkins.Registers);
            stopLoading();
        }

        // Получение веток

        public void getBranches_DoWork(object sender, DoWorkEventArgs e)
        {
            getBranchesWorker.ReportProgress(1);
            foreach (Register register in DataJenkins.Registers)
            {
                Log.Debug("Получачем ветки для джоба " + register.name + ", jobURL - " + register.url);
                var response = Requests.getRequest($"https://ci-sel.dks.lanit.ru/jenkins/job/{DataJenkins.ProjectName}/job/{register.name}/api/json?pretty=true");
                Jobs branchList = JsonConvert.DeserializeObject<Jobs>(response);
                register.BranchList = branchList;
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
        }

        // Деплой

        public void startDeploy_DoWork(object sender, DoWorkEventArgs e)
        {
            startDeployWorker.ReportProgress(1);
            foreach (DeployEnvironment de in DataJenkins.DeployEnvironments)
            {
                String url = $"https://ci-sel.dks.lanit.ru/jenkins/job/{de.Project}/job/{de.RegisterName}/job/{de.Branch}/buildWithParameters?STAND={de.Stand}&SKIP_DB={de.SKIP_DB}&OLD_BUILD=";
                Log.Info($"deploy \"{de.RegisterName}\" on \"{de.Stand}\" url: " + url);
                Requests.postRequestAsyncJenkins(url);
                // Для отладки без запуска делпоя: 
                //Thread.Sleep(2000);
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

                    var standParameter = BuildResult.getFirstAction().getStandParameter();
                    if (standParameter == null)
                    {
                        continue;
                    }
                    if (standParameter.Value.Equals(de.Stand))
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
    }
}
