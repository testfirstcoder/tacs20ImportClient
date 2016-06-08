namespace Tacs20ImportClient.JsonObjects
{
    /// <summary>
    /// Repräsentiert ein Navigationsobjekt für Zuweisungen an eine spezifische Anstellung
    /// </summary>
    public class Anstellung
    {
        #region properties
        /// <summary>
        /// Identifikation, zusammengesetzt aus Personalnummer und Anstellungsnummer
        /// </summary>
        public string AnstellungsId { get; set; }
        /// <summary>
        /// URL zur Ressource, die Statistikcodes zurückgibt, die von diesem Mitarbeiter verwendet werden dürfen
        /// </summary>
        public string StatistikCodeUrl { get; set; }
        /// <summary>
        /// URL zur Ressource, die Nutzniesser zurückgibt, die von diesem Mitarbeiter verwendet werden dürfen
        /// </summary>
        public string NutzniesserUrl { get; set; }
        /// <summary>
        /// URL zur Ressource, die Variablen zurückgibt, die von diesem Mitarbeiter verwendet werden dürfen
        /// </summary>
        public string VariablenUrl { get; set; }
        #endregion
    }
}
