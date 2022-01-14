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

            if (Data.IsPrepareBuildsDone)
            {
                prepareBuildsButton.IsEnabled = false;
                openBuildsWindow.IsEnabled = true;
                branchName.IsEnabled = false;
            }

            if (Data.IsBuildsStarted)
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

        private void Stand1Plus(object sender, RoutedEventArgs e)
        {
            Stand2.Visibility = Visibility.Visible;
            Stand2Panel.Visibility = Visibility.Visible;
            Stand1Panel.Visibility = Visibility.Hidden;
        }

        private void Stand2Plus(object sender, RoutedEventArgs e)
        {
            Stand3.Visibility = Visibility.Visible;
            Stand3Panel.Visibility = Visibility.Visible;
            Stand2Panel.Visibility= Visibility.Hidden;
        }

        private void Stand2Minus(object sender, RoutedEventArgs e)
        {
            Stand2.Visibility = Visibility.Hidden;
            Stand2Panel.Visibility = Visibility.Hidden;
            Stand1Panel.Visibility = Visibility.Visible;
            Stand2.SelectedIndex = -1;
        }

        private void Stand3Minus(object sender, RoutedEventArgs e)
        {
            Stand2.Visibility = Visibility.Visible;
            Stand2Panel.Visibility = Visibility.Visible;
            Stand3Panel.Visibility= Visibility.Hidden;
            Stand3.Visibility= Visibility.Hidden;
            Stand3.SelectedIndex = -1;
        }

        private void SelectStands(object sender, RoutedEventArgs e)
        {
            //selectStands.IsEnabled = false;
            List<Stand> stands = new List<Stand>();

            foreach (var children in selectStandsGrid.Children)
            {
                ComboBox cb;
                TextBlock tb;
                if (children is ComboBox)
                {
                    cb = children as ComboBox;
                    Console.WriteLine(cb.Text);
                    if (cb.Text != null)
                    {
                        switch (cb.Text)
                        {
                            case "ЕИС-3":
                                stands.Add(new EIS3());
                                break;
                            case "ЕИС-4":
                                stands.Add(new EIS4());
                                break;
                            case "ЕИС-5":
                                stands.Add(new EIS5());
                                break;
                            case "ЕИС-6":
                                stands.Add(new EIS6());
                                break;
                            case "ЕИС-7":
                                stands.Add(new EIS7());
                                break;
                        }
                    }
                }
            }
            Data.stands = stands;
        }

        private void refreshDeploysButton_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }

}
