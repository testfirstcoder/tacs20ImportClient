using System;

namespace Tacs20ImportClient.JsonObjects
{
    /// <summary>
    /// Repräsentiert einen Nutzniesser
    /// </summary>
    public class Nutzniesser
    {
        /// <summary>
        /// Die Identifikation des Nutzniessers
        /// </summary>
        public string NutzniesserCode { get; set; }
        /// <summary>
        /// Bezeichnung des Nutzniessers
        /// </summary>
        public string Bezeichnung { get; set; }
        /// <summary>
        /// Ab wann dieser Nutzniesser bewirtschaftet werden kann
        /// </summary>
        public DateTime GueltigAb { get; set; }
        /// <summary>
        /// Bis wann dieser Nutzniesser bewirtschaftet werden kann. Wenn null, bis auf weiteres
        /// </summary>
        public DateTime? GueltigBis { get; set; }
    }
}
