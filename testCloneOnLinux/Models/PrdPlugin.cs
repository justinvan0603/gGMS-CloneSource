using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace testCloneOnLinux.Models
{
    public class PrdPlugin
    {
        public string PluginId { get; set; }
        public string PluginCode { get; set; }
        public string PluginLocation { get; set; }
        public string PluginName { get; set; }
        public string PluginDescription { get; set; }
        public decimal? Price { get; set; }
        public decimal? PriceVat { get; set; }
        public decimal? Vat { get; set; }
        public decimal? DiscountAmt { get; set; }
        public string Notes { get; set; }
        public string RecordStatus { get; set; }
        public string MakerId { get; set; }
        public string DepId { get; set; }
        public DateTime? CreateDt { get; set; }
        public string AuthStatus { get; set; }
        public string CheckerId { get; set; }
        public DateTime? ApproveDt { get; set; }
        public string EditorId { get; set; }
        public DateTime? EditDt { get; set; }
    }
}
