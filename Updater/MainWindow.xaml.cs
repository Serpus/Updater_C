using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private BackgroundWorker worker = new BackgroundWorker();
        private PrepareBuildsWindow prepareBuildsWindow = new PrepareBuildsWindow();
        private bool loading;

        public MainWindow()
        {
            InitializeComponent();

            this.Closed += MainWindow_Closed;

            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            foreach(Window window in Application.Current.Windows)
            {
                window.Close();
            }
        }

        private void buildsGeneralSettings(object sender, MouseEventArgs e)
        {
            if (branchName.Text != "")
            {
                prepareBuildsButton.IsEnabled = true;
            }
            else
            {
                prepareBuildsButton.IsEnabled = false;
            }

            if (Data.PrepareBuildsDone)
            {
                prepareBuildsButton.IsEnabled = false;
                openBuildsWindow.IsEnabled = true;
                branchName.IsEnabled = false;
            }

            if (Data.BuildsStarted)
            {
                buildsStatusGrid.IsEnabled = true;
                openBuildsWindow.IsEnabled = false;
            }
        }

        private void branchName_KeyDown(object sender, KeyEventArgs e)
        {
            prepareBuildsButton.IsEnabled = true;
        }

        private void prepareBuildsButton_Click(object sender, RoutedEventArgs e)
        {
            Data.branchName = branchName.Text;
            Log.Info("Начинаем отбор билд-планов с указанной веткой");
            worker.RunWorkerAsync();
        }

        private void openBuildsWindow_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("Открываем окно с отобранными билд-планами");
            prepareBuildsWindow.Owner = this;
            prepareBuildsWindow.ShowDialog();
        }

        private void RefreshBuildsStatus(object sender, RoutedEventArgs e)
        {
            Log.Info("Обновление статусов запущенных билдов");
            buildsStatusList.Items.Clear();
            foreach (Project project in Data.startedBuilds)
            {
                string buildResultkey = project.startingBuildResult.buildResultkey;
                string result = Requests.getRequest("https://ci-sel.dks.lanit.ru/rest/api/latest/result/" + buildResultkey);
                BuildStatus buildStatus = JsonConvert.DeserializeObject<BuildStatus>(result);
                project.buildStatus = buildStatus;

                Label label = new Label();
                if (project.buildStatus.state.Equals("Successful")) 
                {
                    label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#12BF0F");
                } else if (project.buildStatus.state.Equals("Failed"))
                {
                    label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#DF0E0E");
                }

                if (project.buildStatus.state.Equals("Unknown")) { project.buildStatus.state = "In Progress"; }

                label.Content = $"{project.branch.name} - #{project.startingBuildResult.buildNumber} - {project.buildStatus.state}\n" +
                    "https://ci-sel.dks.lanit.ru/browse/" + buildResultkey;
                label.VerticalAlignment = VerticalAlignment.Stretch;
                label.HorizontalAlignment = HorizontalAlignment.Stretch;
                buildsStatusList.Items.Add(label);
            }
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

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            worker.ReportProgress(50);
            prepareBuildsWindow.PrepareBuilds();
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
            stopLoading();
            prepareBuildsWindow.settingBranchInList();
        }
    }

}
