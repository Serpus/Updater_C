using NLog.Fluent;
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
using Updater.MO;
using Updater.CustomElements;
using System.ComponentModel;
using Updater.ProjectParser.DeployParser;
using Newtonsoft.Json;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для NoBuildDeploysTabWindow.xaml
    /// </summary>
    public partial class NoBuildDeploysWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        private BackgroundWorker refreshDeployStatusWorker = new BackgroundWorker();
        private bool loading = false;

        public NoBuildDeploysWindow()
        {
            InitializeComponent();

            refreshDeployStatusWorker.WorkerSupportsCancellation = true;
            refreshDeployStatusWorker.WorkerReportsProgress = true;
            refreshDeployStatusWorker.DoWork += RefreshDeployStatusWorker_DoWork;
            refreshDeployStatusWorker.ProgressChanged += RefreshDeployStatusWorker_ProgressChanged;
            refreshDeployStatusWorker.RunWorkerCompleted += RefreshDeployStatusWorker_RunWorkerCompleted;

            this.Closing += NoBuildDeploysTabWindow_Closed;

            Version.Content = $"v. {Data.GetVersion()}";
        }

        private void NoBuildDeploysTabWindow_Closed(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (Data.IsCloseProgram)
            {
                e.Cancel = false;
            }
            Log.Info("Закрываем окно деплоев без предварительной сборки");
            this.Visibility = Visibility.Hidden;
            this.Owner.Show();
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
            Stand2Panel.Visibility = Visibility.Hidden;
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
            Stand3Panel.Visibility = Visibility.Hidden;
            Stand3.Visibility = Visibility.Hidden;
            Stand3.SelectedIndex = -1;
        }

        private void selectStands_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show("Выберите хотя бы один стенд", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (BranchName.Text == "")
            {
                MessageBox.Show("Введите название ветки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Data.selectedStands = stands;
            Data.branchName = BranchName.Text;

            String standsList = "";
            foreach (Stand stand in stands)
            {
                standsList += stand.Name + ", ";
            }
            standsList = standsList.Substring(0, standsList.Length - 2);
            Log.Info("Выбранные стенды: " + standsList);

            selectStandsGrid.IsEnabled = false;
            selectStands.IsEnabled = false;
            openPreparedeployButton.IsEnabled = true;
            ResetStands.IsEnabled = true;
            branchSelectPanel.IsEnabled = false;
        }

        private void ResetStands_Click(object sender, RoutedEventArgs e)
        {
            ResetStands.IsEnabled = false;
            openPreparedeployButton.IsEnabled = false;
            selectStands.IsEnabled = true;
            selectStandsGrid.IsEnabled = true;
            branchSelectPanel.IsEnabled = true;
            Data.selectedStands = null;
        }

        private void SetBranchName(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem)
            {
                MenuItem item = (MenuItem)sender;
                BranchName.Text = item.Header.ToString();
            }
        }

        private void openPrepareDeployButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareNoBuildsDeployWindow window = new PrepareNoBuildsDeployWindow()
            {
                Owner = this,
            };
            if (window.ShowDialog().Value)
            {
                DeployStatusGrid.IsEnabled = true;
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
                }
                else if (deploy.CurrentStatus.Contains("UNKNOWN"))
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

                    if (item.Content is ListView listView)
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
    }
}
