﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    internal class Project
    {
        public string id { get; set; }
        public Key key { get; set; }
        public string name { get; set; }
        public PlanKey planKey { get; set; }
        public Branch branch { get; set; }
        public List<EnvironmentBuild> environments { get; set; }
        public List<Stand> stands { get; set; }
        public StartingBuildResult startingBuildResult { get; set; }
        public BuildStatus buildStatus { get; set; }
        public StartingDeployResult startingDeployResult { get; set; }
    }
}
