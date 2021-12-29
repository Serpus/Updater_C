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

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();
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
        }

        private void branchName_KeyDown(object sender, KeyEventArgs e)
        {
            prepareBuildsButton.IsEnabled = true;
        }

        private void prepareBuildsButton_Click(object sender, RoutedEventArgs e)
        {
            Data.branchName = branchName.Text;
            prepareBuildsWindow.Owner = this;
            worker.RunWorkerAsync();
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
            prepareBuildsWindow.ShowDialog();
        }
    }

}
