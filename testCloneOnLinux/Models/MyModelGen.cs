using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace testCloneOnLinux.Models
{
    public class MyModelGen
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public bool IsOverride { get; set; }

        public string DatabaseName { get; set; }
        public string MySQlConnectionString { get; set; }
        public string DatabaseUser { get; set; }
        public string Password { get; set; }
        public string ScriptLocation { get; set; }
        public string Subdomain { get; set; }
        public string Domain { get; set; }
        //public string UsernameHosting { get; set; }
    }
}
