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
        }

        private void JenkinsWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            if (Data.IsCloseProgram)
            {
                e.Cancel = false;
            }
            this.Visibility = Visibility.Hidden;
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
            } else
            {
                Log.Info("sender in GetJobs method is not ProjectButton");
                MessageBox.Show("sender in GetJobs method is not ProjectButton");
                return;
            }

            ProjectStackPanel.IsEnabled = false;
            SelectedProjectName.Text = "Выбранный проект: " + ProjectButton.Content.ToString();
            SelectedBranchName.Text = "Выбранная ветка: " + BranchName.Text;

            getJobsWorker.RunWorkerAsync();
        }

        /**
         * Создание кнопок в части окна (с реестрами)
         */
        public void CreateButtons(List<Register> jobslist)
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
            ProjectStackPanel.IsEnabled = true;
            jobsRegisterStackPanel.Children.Clear();
            SelectedProjectName.Text = "";
            SelectedBranchName.Text = "";
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

            ConfirmMo confirm = new ConfirmMo();
            confirm.ShowDialog();
            if (confirm.DialogResult.Value)
            {
                foreach (CheckBox checkBox in StandCheckBoxes.Children)
                {
                    if (checkBox.IsChecked.Value)
                    {
                        Log.Info("Deploy on " + checkBox.Content);
                        DataJenkins.SKIP_DB = SKIP_DB.IsChecked.Value.ToString();
                        DataJenkins.STAND = checkBox.Content.ToString().ToLower();
                        String branch = HttpUtility.UrlEncode(BranchName.Text);

                        foreach (CheckBox regCB in jobsRegisterStackPanel.Children)
                        {
                            if (regCB.IsChecked.Value)
                            {
                                String url = $"https://ci-sel.dks.lanit.ru/jenkins/job/{DataJenkins.ProjectName}/job/{regCB.Content}/job/{branch}/buildWithParameters?STAND={DataJenkins.STAND}&SKIP_DB={DataJenkins.SKIP_DB}&OLD_BUILD=";
                                Log.Info($"deploy \"{regCB.Content}\" url: " + url);
                            }
                        }
                    }
                }
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
            CreateButtons(DataJenkins.Registers);
            stopLoading();
        }

        public void getBranches_DoWork(object sender, DoWorkEventArgs e)
        {
            getBranchesWorker.ReportProgress(1);
            foreach (Register register in DataJenkins.Registers)
            {
                Log.Debug("jobName - " + register.name + ", jobURL - " + register.url);
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
                    IsEnabled = false
                };

                foreach (Job branch in regJob.BranchList.jobs)
                {
                    String branchF = HttpUtility.UrlDecode(branch.name);
                    if (branchF == BranchName.Text)
                    {
                        checkBox.IsEnabled = true;
                        break;
                    }
                }

                jobsRegisterStackPanel.Children.Add(checkBox);
            }
            stopLoading();
        }
    }
}
