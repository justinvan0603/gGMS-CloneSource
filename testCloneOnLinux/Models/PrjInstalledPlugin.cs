using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace testCloneOnLinux.Models
{
    public class PrjInstalledPlugin
    {
        
        public string PLUGIN_ID { get; set; }
        
        public string PROJECT_ID { get; set; }
        public string PROJECT_NAME { get; set; }
        public string IS_CHECKED { get; set; }
        public string SUBDOMAIN { get; set; }
    }
}
