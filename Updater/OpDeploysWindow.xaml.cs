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
using Updater.ProjectParser.DeployParser;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для OpDeploysWindow.xaml
    /// </summary>
    public partial class OpDeploysWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        private BackgroundWorker refreshDeployStatusWorker = new BackgroundWorker();
        private bool loading = false;

        public OpDeploysWindow()
        {
            InitializeComponent();

            this.Closing += OpDeploysWindow_Closing;

            refreshDeployStatusWorker.WorkerSupportsCancellation = true;
            refreshDeployStatusWorker.WorkerReportsProgress = true;
            refreshDeployStatusWorker.DoWork += RefreshDeployStatusWorker_DoWork;
            refreshDeployStatusWorker.ProgressChanged += RefreshDeployStatusWorker_ProgressChanged;
            refreshDeployStatusWorker.RunWorkerCompleted += RefreshDeployStatusWorker_RunWorkerCompleted;

            Version.Content = $"v. {Data.GetVersion()}";
        }

        private void OpDeploysWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (Data.IsCloseProgram)
            {
                e.Cancel = false;
            }
            this.Visibility = Visibility.Hidden;
            Log.Info("Закрываем окно деплоев открытой части");
        }

        private void PrepareEpzBd(object sender, RoutedEventArgs e)
        {
            Project selectedProject = new Project();
            foreach (Project project in Data.projects)
            {
                if (project.planKey.key.Equals("EPZ-EPZDATABASE"))
                {
                    selectedProject = project;
                }
            }
            PrepareOpWindow prepareWindow = new PrepareOpWindow()
            {
                Owner = this,
                SelectedProject = selectedProject,
            };
            if (prepareWindow.ShowDialog().Value)
            {
                DeployStatusPanel.IsEnabled = true;
            }
        }

        private void PrepareEpz(object sender, RoutedEventArgs e)
        {
            Project selectedProject = new Project();
            foreach (Project project in Data.projects)
            {
                if (project.planKey.key.Equals("EPZ-EPZWF"))
                {
                    selectedProject = project;
                }
            }
            PrepareOpWindow prepareWindow = new PrepareOpWindow()
            {
                Owner = this,
                SelectedProject = selectedProject,
            };
            if (prepareWindow.ShowDialog().Value)
            {
                DeployStatusPanel.IsEnabled = true;
            }
        }

        private void PrepareLko(object sender, RoutedEventArgs e)
        {
            Project selectedProject = new Project();
            foreach (Project project in Data.projects)
            {
                if (project.planKey.key.Equals("EPZ-LKO"))
                {
                    selectedProject = project;
                }
            }
            PrepareOpWindow prepareWindow = new PrepareOpWindow()
            {
                Owner = this,
                SelectedProject = selectedProject,
            };
            if (prepareWindow.ShowDialog().Value)
            {
                DeployStatusPanel.IsEnabled = true;
            }
        }

        private void SelectStands_Click(object sender, RoutedEventArgs e)
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
                Log.Info("ЕПЗ: Не выбрано ни одного стенда");
                MessageBox.Show("Выберите хотя бы один стенд", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (BranchName.Text == "")
            {
                MessageBox.Show("Введите название ветки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DataOp.selectedStands = stands;
            DataOp.branchName = BranchName.Text;

            String standsList = "";
            foreach (Stand stand in stands)
            {
                standsList += stand.Name + ", ";
            }
            standsList = standsList.Substring(0, standsList.Length - 2);
            Log.Info("ЕПЗ: Выбранные стенды: " + standsList);

            selectStandsGrid.IsEnabled = false;
            selectStands.IsEnabled = false;
            ResetStands.IsEnabled = true;
            BranchStackPanel.IsEnabled = false;

            EpzDbButton.IsEnabled = true;
            EpzButton.IsEnabled = true;
            LkoButton.IsEnabled = true;
        }

        private void ResetStands_Click(object sender, RoutedEventArgs e)
        {
            ResetStands.IsEnabled = false;
            selectStands.IsEnabled = true;
            selectStandsGrid.IsEnabled = true;
            BranchStackPanel.IsEnabled = true;
            DeployStatusPanel.IsEnabled = false;
            DataOp.selectedStands = null;

            EpzDbButton.IsEnabled = false;
            EpzButton.IsEnabled = false;
            LkoButton.IsEnabled = false;
        }

        private void RefreshDeploysStatus(object sender, RoutedEventArgs e)
        {
            Log.Info("--- ЕПЗ: Обновляем статусы деплоев ---");
            ClearDeployStatusList();
            refreshDeployStatusWorker.RunWorkerAsync();
        }

        private void SetDeployResultInPanel()
        {
            foreach (StartedDeploy deploy in DataOp.startedDeploys)
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
                } else if (deploy.CurrentStatus.Contains("FAILURE"))
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





        private void SetBranchName(Object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                BranchName.Text = menuItem.Header.ToString();
            }
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
        private void OpenCurrentBuild(object sender, RoutedEventArgs e)
        {
            if (sender is ResultMenuItem Result)
            {
                System.Diagnostics.Process.Start(Result.ResultUrl);
            }
        }
        private void ClearDeployStatusList()
        {
            deploysEIS3.Items.Clear();
            deploysEIS4.Items.Clear();
            deploysEIS5.Items.Clear();
            deploysEIS6.Items.Clear();
            deploysEIS7.Items.Clear();
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


        private void RefreshDeployStatusWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            refreshDeployStatusWorker.ReportProgress(50);
            foreach (StartedDeploy deploy in DataOp.startedDeploys)
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
