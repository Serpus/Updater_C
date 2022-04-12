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
    /// Логика взаимодействия для JenkinsWindow.xaml
    /// </summary>
    public partial class JenkinsWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        public JenkinsWindow()
        {
            InitializeComponent();
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
            }
            var response = Requests.getRequest($"https://ci-sel.dks.lanit.ru/jenkins/job/{ProjectButton.ProjectName}/api/json?pretty=true");
            Jobs jobsList = JsonConvert.DeserializeObject<Jobs>(response);

            Log.Info("--- Реестры на докерах ---");
            int i = 0;
            foreach (Job job in jobsList.jobs)
            {
                i++;
                Log.Info(i + " job - " + job.name);
            }
            Log.Info("--- *** ---");
            CreateButtons(jobsList);
        }

        /**
         * Создание кнопок в части окна (с реестрами)
         */
        private void CreateButtons(Jobs jobslist)
        {
            Log.Debug("--- Создаём кнопки ---");
            foreach (Job job in jobslist.jobs)
            {
                JobCheckBox checkBox = new JobCheckBox()
                {
                    Content = job.name,
                    JobName = job.name,
                    JobUrl = job.url,
                };

                Log.Debug("jobName - " + job.name + ", jobURL - " + job.url);
                jobsRegisterStackPanel.Children.Add(checkBox);
            }
            Log.Debug("--- *** ---");
        }

        /**
         * Отметить все чекбоксы
         */
        private void CheckAll(object sender, RoutedEventArgs e)
        {
            foreach(JobCheckBox checkBox in jobsRegisterStackPanel.Children)
            {
                checkBox.IsChecked = true;
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
            Log.Info("Чекбоксы все сняты");
        }
    }
}
