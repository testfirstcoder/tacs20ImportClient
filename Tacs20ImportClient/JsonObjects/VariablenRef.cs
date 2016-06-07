using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tacs20ImportClient.JsonObjects
{
    /// <summary>
    /// Referenzobjekt einer tacs-Variable
    /// </summary>
    public class VariablenRef
    {
        /// <summary>
        /// Die Tacs-Id als Identifikation
        /// </summary>
        public string TacsCode { get; set; }
        /// <summary>
        /// Ab wann diese Variable bewirtschaftet werden kann
        /// </summary>
        public DateTime GueltigAb { get; set; }
        /// <summary>
        /// Bis wann diese Variable bewirtschaftet werden kann. Wenn null, bis auf weiteres
        /// </summary>
        public DateTime? GueltigBis { get; set; }
    }
}
