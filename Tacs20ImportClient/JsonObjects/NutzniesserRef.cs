using System;

namespace Tacs20ImportClient.JsonObjects
{
    /// <summary>
    /// Eine Referenz auf einen Nutzniesser
    /// </summary>
    public class NutzniesserRef
    {
        #region properties
        /// <summary>
        /// Der Code als Identifikation
        /// </summary>
        public string NutzniesserCode { get; set; }
        /// <summary>
        /// Ab wann dieser Nutzniesser bewirtschaftet werden kann
        /// </summary>
        public DateTime GueltigAb { get; set; }
        /// <summary>
        /// Bis wann dieser Nutzniesser bewirtschaftet werden kann. Wenn null, bis auf weiteres
        /// </summary>
        public DateTime? GueltigBis { get; set; }
        #endregion
    }
}
