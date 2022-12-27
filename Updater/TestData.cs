using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Updater.CustomElements;
using Updater.ProjectParser.BuildParser;

namespace Updater
{
    internal static class TestData
    {
        public static String SuccessResultBuild = "SUCCESS";
        public static String FailedResultBuild = "FAILURE";
        public static int TestStandCounter = 0;
        public static List<BuildStatusLabel> GetBuildStatusLabels(int size, String buildResult)
        {
            List<BuildStatusLabel> labelList = new List<BuildStatusLabel>();

            for (int i = 1; i <= size; i++) 
            {
                BuildResult result = GetBuildResult(i, buildResult);
                ContextMenu cm = new ContextMenu();
                ResultMenuItem menuItem = new ResultMenuItem()
                {
                    Header = "Открыть в браузере",
                    ResultUrl = result.Url
                };
                menuItem.Click += JenkinsWindow.OpenCurrentBuild;
                cm.Items.Add(menuItem);
                BuildStatusLabel label = new BuildStatusLabel()
                {
                    Content = result.FullDisplayName + " - " + result.Result,
                    BuildResult = result,
                    ContextMenu = cm,
                };

                if (label.BuildResult.Result == null)
                {
                    label.Content = result.FullDisplayName + " - В процессе";
                }
                else
                {
                    if (label.BuildResult.Result.Equals("SUCCESS"))
                    {
                        label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#12BF0F");
                    }
                    else if (label.BuildResult.Result.Equals("FAILURE"))
                    {
                        label.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#DF0E0E");
                    }
                }

                labelList.Add(label);
            }

            return labelList;
        }

        public static BuildResult GetBuildResult(int buildNumber, String resultName)
        {
            BuildResult result = new BuildResult()
            {
                Result = resultName,
                FullDisplayName = $"Test Build {buildNumber} FullDisplayName",
                Url = $"https://test-debug.comru/jenkins/test_build_result_url_{buildNumber}"
            };

            return result;
        }
    }
}
