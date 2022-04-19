﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Threading;

namespace Updater.MO
{
    /// <summary>
    /// Логика взаимодействия для NoBuildsDeployPrepareWindow.xaml
    /// </summary>
    public partial class NoBuildsDeployPrepareWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        private BackgroundWorker preapareBuildsWorker = new BackgroundWorker();
        private bool loading = false;
        public NoBuildsDeployPrepareWindow()
        {
            InitializeComponent();

            preapareBuildsWorker.WorkerSupportsCancellation = true;
            preapareBuildsWorker.WorkerReportsProgress = true;
            preapareBuildsWorker.DoWork += PreapareBuildsWorker_DoWork;
            preapareBuildsWorker.ProgressChanged += PreapareBuildsWorker_ProgressChanged;
            preapareBuildsWorker.RunWorkerCompleted += PreapareBuildsWorker_RunWorkerCompleted;

            preapareBuildsWorker.RunWorkerAsync();
        }

        public void SetBuilds()
        {
            foreach (Project project in Data.startedBuilds)
            {
                if (project.buildStatus == null)
                {
                    continue;
                }

                ProjectCheckBox checkBox = new ProjectCheckBox();
                checkBox.Project = project;
                checkBox.Content = project.branch.name;

                if (project.buildStatus.state.Equals("Successful"))
                {
                    SuccessBuilds.Children.Add(checkBox);
                }
                //else if (project.buildStatus.state.Equals("Failed"))
                //{
                //    checkBox.IsEnabled = false;
                //    ProcessBuilds.Children.Add(checkBox);
                //}

                //if (project.buildStatus.state.Equals("In Progress") | project.buildStatus.state.Equals("Unknown"))
                //{
                //    checkBox.IsEnabled = false;
                //    ProcessBuilds.Children.Add(checkBox);
                //}
            }

            String standsList = "";
            foreach (Stand stand in Data.selectedStands)
            {
                standsList += stand.Name + ", ";
            }
            standsList = standsList.Substring(0, standsList.Length - 2);
            standNames.Content = standsList;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Data.startedBuilds = null;
            this.Close();
        }




        private string message = "";
        private void PreapareBuildsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Data.startedBuilds = new List<Project>();
            preapareBuildsWorker.ReportProgress(1);
            Log.Info("Отбираем из бамбу проекты с веткой " + Data.branchName);
            foreach(Project project in Data.projects)
            {
                message = "Отбираем из бамбу проекты с веткой " + Data.branchName;
                if (project.planKey.key.Contains("EPZ-"))
                {
                    continue;
                }

                string url = $"https://ci-sel.dks.lanit.ru/rest/api/latest/plan/{project.planKey.key}/branch";
                string result = Requests.getRequest(url);

                BranchList branchList = JsonConvert.DeserializeObject<BranchList>(result);
                foreach (Branch branch in branchList.branches.branch)
                {
                    if (branch.shortName.Equals(Data.branchName))
                    {
                        project.branch = branch;
                        Data.startedBuilds.Add(project);
                        break;
                    }
                }
            }

            foreach (Project project in Data.startedBuilds)
            {
                message = "Получаем последние билды";
                if (project.branch == null)
                {
                    continue;
                }
                
                string url = $"https://ci-sel.dks.lanit.ru/rest/api/latest/result/{project.branch.key}-latest";
                string result = Requests.getRequest(url);

                if (result == null)
                {
                    Log.Info("У билд-плана: " + project.name + " отсутствуют сборки");
                    continue;
                }

                BuildStatus buildStatus = JsonConvert.DeserializeObject<BuildStatus>(result);
                Log.Info("Билд-план: " + project.name + " / Ветка: " + project.branch.shortName + " / Последний билд: " + buildStatus.buildResultKey + " - " + buildStatus.state);
                project.buildStatus = buildStatus;
            }
        }
        private void PreapareBuildsWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            startLoading(message);
        }

        private void PreapareBuildsWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetBuilds();
            stopLoading();
            Log.Info("--- *** ---");
        }

        public void startLoading()
        {
            LoadingGrid.Visibility = Visibility.Visible;
            loading = true;
        }

        public void startLoading(string message)
        {
            Message.Content = message;
            if (!loading) {
                LoadingGrid.Visibility = Visibility.Visible;
                loading = true;
            }
        }

        public void stopLoading()
        {
            LoadingGrid.Visibility = Visibility.Hidden;
            loading = false;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            StackPanel somePanel = null;

            if (sender is CheckBox checkBox)
            {
                if (checkBox.Name.Contains("Success"))
                {
                    somePanel = SuccessBuilds;
                }
                //if (checkBox.Name.Contains("Process"))
                //{
                //    somePanel = ProcessBuilds;
                //}

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
    }
}
