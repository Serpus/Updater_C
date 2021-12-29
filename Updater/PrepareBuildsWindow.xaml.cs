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
using Newtonsoft.Json;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class PrepareBuildsWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private Project[] eisProjects;
        private Project[] lkpProjects;
        private Project[] epzProjects;
        private Project[] otherProjects;
        public PrepareBuildsWindow()
        {
            InitializeComponent();
            this.Closing += cancelClosing;
        }

        private void cancelClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        public void PrepareBuilds()
        {
            Log.Info("Ветка: " + Data.branchName);
            Log.Info("---Обработка билд-планов:---");
            foreach (Project project in Data.projects)
            {
                string url = Data.GetBranchesUrl(project.planKey.key);
                string result = Requests.getRequest(url);
                Log.Info(project.name);

                BranchList branchList = JsonConvert.DeserializeObject<BranchList>(result);
                foreach (Branch branch in branchList.branches.branch)
                {
                    if (branch.shortName.Equals(Data.branchName))
                    {
                        project.branch = branch;
                    }
                }
            }
        }

        public void settingBranchInList()
        {
            branchName.Content = Data.branchName;
            Log.Info("---Отобранные билд-планы---");
            foreach (Project project in Data.projects)
            {
                if (project.branch == null || !project.branch.shortName.Equals(Data.branchName))
                {
                    continue;
                }
                Log.Info(project.branch.name);

                if (project.planKey.key.Contains("EIS"))
                {
                    CheckBox checkBox1 = new CheckBox();
                    checkBox1.Content = project.branch.name;
                    fcsBuilds.Children.Add(checkBox1);
                    continue;
                }

                if (project.planKey.key.Contains("LKP"))
                {
                    CheckBox checkBox1 = new CheckBox();
                    checkBox1.Content = project.branch.name;
                    lkpBuilds.Children.Add(checkBox1);
                    continue;
                }

                if (project.planKey.key.Contains("EPZ"))
                {
                    CheckBox checkBox1 = new CheckBox();
                    checkBox1.Content = project.branch.name;
                    epzBuilds.Children.Add(checkBox1);
                    continue;
                }

                CheckBox checkBox = new CheckBox();
                checkBox.Content = project.branch.name;
                otherBuilds.Children.Add(checkBox);
            }

            setEnableDisableCheckBoxes();
        }

        private void setEnableDisableCheckBoxes()
        {
            fcsGroupCheckBox.IsEnabled = fcsBuilds.Children.Count > 0;

            lkpGroupCheckBox.IsEnabled = lkpBuilds.Children.Count > 0;

            epzGroupCheckBox.IsEnabled = epzBuilds.Children.Count > 0;

            otherGroupCheckBox.IsEnabled = otherBuilds.Children.Count > 0;
        }

        private void CancelPrepareBuildsWindow(object sender, RoutedEventArgs e)
        {
            fcsBuilds.Children.Clear();
            lkpBuilds.Children.Clear();
            epzBuilds.Children.Clear();
            otherBuilds.Children.Clear();
            this.Visibility = Visibility.Hidden;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            StackPanel somePanel = null;

            if (sender is CheckBox checkBox)
            {
                if (checkBox.Name.Contains("fcs"))
                {
                    somePanel = fcsBuilds;
                }
                if (checkBox.Name.Contains("lkp"))
                {
                    somePanel = lkpBuilds;
                }
                if (checkBox.Name.Contains("epz"))
                {
                    somePanel = epzBuilds;
                }
                if (checkBox.Name.Contains("other"))
                {
                    somePanel = otherBuilds;
                }

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

        private void StartBuilds(object sender, RoutedEventArgs e)
        {

        }

    }
}
