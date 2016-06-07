using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tacs20ImportClient.JsonObjects
{
    public class StatistikCodeImport
    {
        public string StatistikCode { get; set; }
        public string Bezeichnung { get; set; }
        public DateTime GueltigAb { get; set; }
        public DateTime? GueltigBis { get; set; }
    }
}
