using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    internal class Data
    {
        public static string username { get; set; }
        public static string password { get; set; }
        public static Project[] projects { get; set; }
        public static string branchName { get; set; }
        public string getBranchesUrl = "";

        public static string GetBranchesUrl(string planKey)
        {
            return $"https://ci-sel.dks.lanit.ru/rest/api/latest/plan/{planKey}/branch";
        }
    }
}
