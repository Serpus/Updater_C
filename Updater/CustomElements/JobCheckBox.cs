using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Updater.CustomElements;
using Updater.ProjectParser.BuildParser;

/// <summary>
/// Класс для помещения окружения джоба дженкина в чекбокс
/// </summary>
namespace Updater
{
    internal class JobCheckBox : CheckBox
    {
        public String JobName { get; set; }
        public String JobUrl { get; set; }
        public String JobProject { get; set; }

        public JobCheckBox() { }

        public JobCheckBox(Register regJob) 
        {
            Content = regJob.name;
            JobName = regJob.name;
            JobUrl = regJob.url;
            JobProject = regJob.project;
            IsEnabled = false;
            ContextMenu = GenerateCm();
        }

        private ContextMenu GenerateCm()
        {
            ContextMenu cm = new ContextMenu();
            ResultMenuItem menuItem = new ResultMenuItem()
            {
                Header = "Открыть в браузере",
                ResultUrl = JobUrl
            };
            menuItem.Click += JenkinsWindow.OpenCurrentBuild;
            cm.Items.Add(menuItem);
            return cm;
        }
    }
}
