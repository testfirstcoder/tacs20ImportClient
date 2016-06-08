namespace Tacs20ImportClient.JsonObjects
{
    /// <summary>
    /// Einstiegspunkt der Schnittstelle. Stellt URLs zur Verfügung, um die Daten abzuholen
    /// </summary>
    public class BaseNavigation
    {
        #region properties
        /// <summary>
        /// URL zur Ressource, die alle Statistikcodes des Mandanten zurückgibt
        /// </summary>
        public string StatistikCodeUrl { get; set; }
        /// <summary>
        /// URL zur Ressource, die alle Nutzniesser des Mandanten zurückgibt
        /// </summary>
        public string NutzniesserUrl { get; set; }
        /// <summary>
        /// URL zur Ressource, die alle Organisationen des Mandanten zurückgibt
        /// </summary>
        public string OrganisationUrl { get; set; }
        /// <summary>
        /// URL zur Ressource, die alle Variablen des Mandanten zurückgibt
        /// </summary>
        public string VariablenUrl { get; set; }
        /// <summary>
        /// URL zur Ressource, die alle Personalkategorien (-gruppe, -typ) des Mandanten zurückgibt
        /// </summary>
        public string PersonalkategorieUrl { get; set; }
        /// <summary>
        /// URL zur Ressource, die alle Anstellungen des Mandanten zurückgibt, die spezifische Variablen 
        /// zugewiesen bekamen
        /// </summary>
        public string AnstellungLink { get; set; }
        #endregion
    }
}
