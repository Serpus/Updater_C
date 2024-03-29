﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Updater.CustomElements;

namespace Updater
{
    internal static class DataOp
    {
        public static string username { get; set; }
        public static string password { get; set; }
        public static Project[] projects { get; set; }
        public static List<ProjectCheckBox> checkedBoxes { get; set; }
        public static List<Project> startedBuilds { get; set; }
        public static string branchName { get; set; }
        public static bool IsPrepareBuildsDone { get; set; }
        public static bool IsBuildsStarted { get; set; }
        public static List<Stand> selectedStands { get; set; }
        public static bool IsCloseProgram { get; set; }
        public static bool IsRefreshEnd { get; set; }
        public static List<PreparedDeploy> preparedDeploy { get; set; }
        public static List<StartedDeploy> startedDeploys { get; set; }
    }
}
