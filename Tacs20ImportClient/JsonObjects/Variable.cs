using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tacs20ImportClient.JsonObjects
{
    /// <summary>
    /// Repräsentiert eine tacs-Variable. Es sind nicht alle Properties vorhanden, die geliefert werden
    /// </summary>
    public class Variable
    {
        /// <summary>
        /// Die eindeutige tacs-Id
        /// </summary>
        public string TacsCode { get; set; }
        /// <summary>
        /// Die Bezeichnung der Variable
        /// </summary>
        public string Bezeichung { get; set; }
        /// <summary>
        /// Wie rodix die Variable beschreibt
        /// </summary>
        public string BeschreibungMethodisch { get; set; }
        /// <summary>
        /// Damit können die Variablen sortiert dargestellt werden
        /// </summary>
        public int SortierNummer { get; set; }
    }
}
