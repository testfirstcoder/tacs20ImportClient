using System;

namespace Tacs20ImportClient.JsonObjects
{
    /// <summary>
    /// Eine Referenz auf einen Statistikcode
    /// </summary>
    public class StatistikCodeRef
    {
        /// <summary>
        /// Der Code als Identifikation
        /// </summary>
        public string StatistikCode { get; set; }
        /// <summary>
        /// Ab wann dieser Statistikcode bewirtschaftet werden kann
        /// </summary>
        public DateTime GueltigAb { get; set; }
        /// <summary>
        /// Bis wann dieser Statistikcode bewirtschaftet werden kann. Wenn null, bis auf weiteres
        /// </summary>
        public DateTime? GueltigBis { get; set; }
    }
}
