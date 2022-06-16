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
using Updater.CustomElements;
using Updater.ProjectParser.DeployParser;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private BackgroundWorker prepareBuildsWorker = new BackgroundWorker();
        private BackgroundWorker refreshBuildStatusWorker = new BackgroundWorker();
        private BackgroundWorker refreshDeployStatusWorker = new BackgroundWorker();

        private PrepareBuildsWindow prepareBuildsWindow = new PrepareBuildsWindow();
        private JenkinsWindow jenkinsWindow = new JenkinsWindow();
        private OpDeploysWindow opDeploysWindow = new OpDeploysWindow();
        private bool loading;

        public MainWindow()
        {
            InitializeComponent();

            this.Closed += MainWindow_Closed;

            prepareBuildsWorker.WorkerReportsProgress = true;
            prepareBuildsWorker.WorkerSupportsCancellation = true;
            prepareBuildsWorker.DoWork += worker_DoWork;
            prepareBuildsWorker.ProgressChanged += worker_ProgressChanged;
            prepareBuildsWorker.RunWorkerCompleted += worker_RunWorkerCompleted;

            refreshBuildStatusWorker.WorkerReportsProgress = true;
            refreshBuildStatusWorker.WorkerSupportsCancellation = true;
            refreshBuildStatusWorker.DoWork += worker2_DoWork;
            refreshBuildStatusWorker.ProgressChanged += worker2_ProgressChanged;
            refreshBuildStatusWorker.RunWorkerCompleted += worker2_RunWorkerCompleted;

            refreshDeployStatusWorker.WorkerSupportsCancellation = true;
            refreshDeployStatusWorker.WorkerReportsProgress = true;
            refreshDeployStatusWorker.DoWork += RefreshDeployStatusWorker_DoWork;
            refreshDeployStatusWorker.ProgressChanged += RefreshDeployStatusWorker_ProgressChanged;
            refreshDeployStatusWorker.RunWorkerCompleted += RefreshDeployStatusWorker_RunWorkerCompleted;

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            Version.Content = $"v. {Data.GetVersion()}";
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception.Message + "\n" + e.Exception.StackTrace);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Data.IsCloseProgram = true;
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
                changeBrunchButton.IsEnabled = true;
                openBuildsWindow.IsEnabled = true;
                branchName.IsEnabled = false;
                branchStackPanel.IsEnabled = false;
            }

            if (Data.IsBuildsStarted)
            {
                buildsStatusGrid.IsEnabled = true;
                openBuildsWindow.IsEnabled = false;
                changeBrunchButton.IsEnabled = false;
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
            prepareBuildsWorker.RunWorkerAsync();
        }

        private void openBuildsWindow_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("Открываем окно с отобранными билд-планами");
            prepareBuildsWindow.Owner = this;
            prepareBuildsWindow.ShowDialog();
        }

        private void RefreshBuildsStatus(object sender, RoutedEventArgs e)
        {
            Log.Info("--- Обновление статусов запущенных билдов ---");
            Data.IsRefreshEnd = false;
            buildsStatusList.Items.Clear();
            refreshBuildStatusWorker.RunWorkerAsync();
        }

        private void OpenAllBuildsButton_Click(object sender, RoutedEventArgs e)
        {
            if (buildsStatusList.Items.Count == 0)
            {
                MessageBox.Show("Отсутствуют билды на панеле статусов");
            }
            Log.Info("Открываем все билды с панели статусов");
            foreach (Label item in buildsStatusList.Items)
            {
                String url = item.Content.ToString().Substring(item.Content.ToString().IndexOf("https://ci-sel.dks.lanit.ru/browse/"));
                System.Diagnostics.Process.Start(url);
            }
        }

        private void AddUpdatedBuilds()
        {
            foreach (Project project in Data.startedBuilds)
            {
                Log.Info($"{project.branch.name} - #{project.startingBuildResult.buildNumber} - {project.buildStatus.state}\n" +
                    "https://ci-sel.dks.lanit.ru/browse/" + project.startingBuildResult.buildResultkey);

                ContextMenu cm = new ContextMenu();
                ResultMenuItem menuItem = new ResultMenuItem()
                {
                    Header = "Открыть в браузере",
                    ResultUrl = "https://ci-sel.dks.lanit.ru/browse/" + project.startingBuildResult.buildResultkey,
                };
                menuItem.Click += OpenCurrentBuild;
                cm.Items.Add(menuItem);
                Label label = new Label()
                {
                    ContextMenu = cm,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Content = $"{project.branch.name} - #{project.startingBuildResult.buildNumber} - {project.buildStatus.state}\n" +
                    "https://ci-sel.dks.lanit.ru/browse/" + project.startingBuildResult.buildResultkey
                };

                if (project.buildStatus.state.Equals("Successful"))
                {
                    label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#12BF0F");
                }
                else if (project.buildStatus.state.Equals("Failed"))
                {
                    label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#DF0E0E");
                }

                if (project.buildStatus.state.Equals("Unknown")) 
                {
                    project.buildStatus.state = "In Progress"; 
                }

                buildsStatusList.Items.Add(label);
            }
        }

        private void ChangeBranch(Object sender, RoutedEventArgs args)
        {
            Log.Info("Сбрасываем выбранную ветку");
            openBuildsWindow.IsEnabled = false;
            prepareBuildsButton.IsEnabled = true;
            changeBrunchButton.IsEnabled = false;
            branchName.IsEnabled = true;
            branchStackPanel.IsEnabled = true;
            Data.IsPrepareBuildsDone = false;
            prepareBuildsWindow.CleanBranchesCheckBoxes();
        }

        private void SetBranchName(Object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem)
            {
                MenuItem menuItem = (MenuItem)sender;
                branchName.Text = menuItem.Header.ToString();
            }
        }

        private void OpenCurrentBuild(object sender, RoutedEventArgs e)
        {
            if (sender is ResultMenuItem Result)
            {
                System.Diagnostics.Process.Start(Result.ResultUrl);
            }
        }




        private void startLoading()
        {
            LoadingGrid.Visibility = Visibility.Visible;
            loading = true;
        }

        private void startLoading(string loadingLabel)
        {
            LoadingGrid.Visibility = Visibility.Visible;
            LoadingLabel.Content = loadingLabel;
            loading = true;
        }

        private void stopLoading()
        {
            LoadingGrid.Visibility = Visibility.Hidden;
            loading = false;
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            prepareBuildsWorker.ReportProgress(50);
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
            List<Stand> stands = new List<Stand>();

            foreach (var children in selectStandsGrid.Children)
            {
                if (children is ComboBox)
                {
                    ComboBox cb = children as ComboBox;
                    if (cb.Text != null)
                    {
                        switch (cb.Text)
                        {
                            case "ЕИС-3":
                                stands.Add(new EIS3() { Name = "ЕИС-3" });
                                break;
                            case "ЕИС-4":
                                stands.Add(new EIS4() { Name = "ЕИС-4" });
                                break;
                            case "ЕИС-5":
                                stands.Add(new EIS5() { Name = "ЕИС-5" });
                                break;
                            case "ЕИС-6":
                                stands.Add(new EIS6() { Name = "ЕИС-6" });
                                break;
                            case "ЕИС-7":
                                stands.Add(new EIS7() { Name = "ЕИС-7" });
                                break;
                        }
                    }
                }
            }
            if (stands.Count == 0)
            {
                Log.Info("Не выбрано ни одного стенда");
                MessageBox.Show("Выберите хотя бы один стенд");
                return;
            }

            Data.selectedStands = stands;
            String standsList = "";
            foreach(Stand stand in stands)
            {
               standsList += stand.Name + ", ";
            }

            standsList = standsList.Substring(0, standsList.Length - 2);

            Log.Info("Выбранные стенды: " + standsList);

            selectStands.IsEnabled = false;
            selectStandsGrid.IsEnabled = false;
            ResetStands.IsEnabled = true;
            openPreparedeployButton.IsEnabled = true;
        }

        private void ResetStands_Click(object sender, RoutedEventArgs e)
        {
            selectStands.IsEnabled = true;
            selectStandsGrid.IsEnabled = true;
            ResetStands.IsEnabled = false;
            Data.selectedStands = null;
            openPreparedeployButton.IsEnabled = false;
        }

        private void openPreparedeployButton_Click(object sender, RoutedEventArgs e)
        {
            List<Stand> stands = new List<Stand>();

            foreach (var children in selectStandsGrid.Children)
            {
                if (children is ComboBox)
                {
                    ComboBox cb = children as ComboBox;
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
            if (stands.Count == 0)
            {
                Log.Info("Не выбрано ни одного стенда");
                MessageBox.Show("Выберите хотя бы один стенд");
                return;
            }
            if (Data.startedBuilds == null)
            {
                Log.Info("Ни один билд не запущен");
                MessageBox.Show("Ни один билд не запущен");
                return;
            }
            Log.Info("Открываем окно с подготовкой деплоев");
            PrepareDeployWindow PrepareDeployWindow = new PrepareDeployWindow();
            PrepareDeployWindow.Owner = this;
            PrepareDeployWindow.startLoading();
            if (PrepareDeployWindow.ShowDialog().Value)
            {
                DeployStatusPanel.IsEnabled = true;
            }
        }

        private void refreshDeploysButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("--- Обновляем статусы деплоев ---");
            ClearDeployStatusList();
            refreshDeployStatusWorker.RunWorkerAsync();
        }

        private void ClearDeployStatusList()
        {
            deploysEIS3.Items.Clear();
            deploysEIS4.Items.Clear();
            deploysEIS5.Items.Clear();
            deploysEIS6.Items.Clear();
            deploysEIS7.Items.Clear();
        }

        private void SetDeployResultInPanel()
        {
            foreach (StartedDeploy deploy in Data.startedDeploys)
            {
                ContextMenu cm = new ContextMenu();
                ResultMenuItem menuItem = new ResultMenuItem()
                {
                    Header = "Открыть в браузере",
                    ResultUrl = $"https://ci-sel.dks.lanit.ru/deploy/viewDeploymentResult.action?deploymentResultId={deploy.DeployResult.deploymentResultId}",
                };
                menuItem.Click += OpenCurrentBuild;
                cm.Items.Add(menuItem);
                DeployStatusLabel label = new DeployStatusLabel(deploy)
                {
                    ContextMenu = cm,
                    Content = deploy.Project.branch.name + " - " + deploy.CurrentStatus,
                    Width = deploysEIS3.ActualWidth - 50,
                };

                if (deploy.CurrentStatus.Contains("SUCCESS"))
                {
                    label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#12BF0F");
                }
                else if (deploy.CurrentStatus.Contains("FAILURE"))
                {
                    label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#DF0E0E");
                } else if (deploy.CurrentStatus.Contains("UNKNOWN"))
                {
                    label.Content = deploy.Project.branch.name + " - В процессе";
                }

                if (deploy.Stand.Name.Contains("ЕИС-3"))
                {
                    deploysEIS3.Items.Add(label);
                }
                if (deploy.Stand.Name.Contains("ЕИС-4"))
                {
                    deploysEIS4.Items.Add(label);
                }
                if (deploy.Stand.Name.Contains("ЕИС-5"))
                {
                    deploysEIS5.Items.Add(label);
                }
                if (deploy.Stand.Name.Contains("ЕИС-6"))
                {
                    deploysEIS6.Items.Add(label);
                }
                if (deploy.Stand.Name.Contains("ЕИС-7"))
                {
                    deploysEIS7.Items.Add(label);
                }
            }
        }

        private void OpenAllDeploys(object sender, RoutedEventArgs e)
        {
            Log.Info("--- Открываем все деплом в браузере ---");
            foreach (TabItem item in DeploysTabControl.Items)
            {
                if (item is TabItem tabItem)
                {
                    if (!tabItem.IsSelected)
                    {
                        continue;
                    }

                    if(item.Content is ListView listView)
                    {
                        foreach (DeployStatusLabel deployStatusLabel in listView.Items)
                        {
                            string url = $"https://ci-sel.dks.lanit.ru/deploy/viewDeploymentResult.action?deploymentResultId={deployStatusLabel.DeployResult.deploymentResultId}";
                            Log.Info($"Открываем {deployStatusLabel.Project.name}: " + url);
                            System.Diagnostics.Process.Start(url);
                        }
                    }
                }
            }
            Log.Info("--- *** ---");
        }






        private void worker2_DoWork(object sender, DoWorkEventArgs e)
        {
            refreshBuildStatusWorker.ReportProgress(1);
            foreach (Project project in Data.startedBuilds)
            {
                string buildResultkey = project.startingBuildResult.buildResultkey;
                string result = Requests.getRequest("https://ci-sel.dks.lanit.ru/rest/api/latest/result/" + buildResultkey);
                BuildStatus buildStatus = JsonConvert.DeserializeObject<BuildStatus>(result);
                project.buildStatus = buildStatus;
            }
        }

        private void worker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!loading)
            {
                startLoading("Обновляем статусы запущенных билдов");
            }
        }

        private void worker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stopLoading();
            AddUpdatedBuilds();
            Data.IsRefreshEnd = true;
            Log.Info("--- *** ---");
        }

        

        private void RefreshDeployStatusWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            refreshDeployStatusWorker.ReportProgress(50);
            foreach (StartedDeploy deploy in Data.startedDeploys)
            {
                string response = Requests.getRequest("https://ci-sel.dks.lanit.ru/rest/api/latest/deploy/result/" + deploy.DeployResult.deploymentResultId);
                DeployCurrentStatus currentStatus = JsonConvert.DeserializeObject<DeployCurrentStatus>(response);
                deploy.CurrentStatus = currentStatus.DeploymentState;
            }
        }
        private void RefreshDeployStatusWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!loading)
            {
                startLoading("Обновляем статусы запущенных деплоев");
            }
        }
        private void RefreshDeployStatusWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetDeployResultInPanel();
            stopLoading();
            Log.Info("--- *** ---");
        }

        private void OpenNoBuildDeploysWindow(object sender, RoutedEventArgs e)
        {
            NoBuildDeploysWindow noBuildDeploysTabWindow = new NoBuildDeploysWindow()
            {
                Owner = this
            };
            noBuildDeploysTabWindow.ShowDialog();
        }

        private void OpenJenkinsWindow(object sender, RoutedEventArgs e)
        {
            Log.Info("Открываем окно Дженкинса");
            jenkinsWindow.Show();
        }

        private void OpenOpDeploys(object sender, RoutedEventArgs e)
        {
            Log.Info("Открываем окно деплоев открытой части");
            opDeploysWindow.Show();
        }
    }
}
