using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    internal class CreateVersionBodyResponse
    {
        public string id { get; set; }
        public string name { get; set; }
        public string creationDate { get; set; }
        public string creatorUserName { get; set; }
    }
}
