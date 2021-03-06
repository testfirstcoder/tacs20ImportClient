﻿using System;

namespace Tacs20ImportClient.JsonObjects
{
    /// <summary>
    /// Repräsentiert einen Statistikcode
    /// </summary>
    public class StatistikCodeImport
    {
        /// <summary>
        /// Die Identifikation des Statistikcodes
        /// </summary>
        public string StatistikCode { get; set; }
        /// <summary>
        /// Die Bezeichung des Statistikcodes
        /// </summary>
        public string Bezeichnung { get; set; }
        /// <summary>
        /// Ab wann dieser Statistikcode bewirtschaftet werden kann
        /// </summary>
        public DateTime GueltigAb { get; set; }
        /// <summary>
        /// Bis wann dieser StatusCode bewirtschaftet werden kann. Wenn null, bis auf weiteres
        /// </summary>
        public DateTime? GueltigBis { get; set; }
    }
}
