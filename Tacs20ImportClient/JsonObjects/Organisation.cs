namespace Tacs20ImportClient.JsonObjects
{
    /// <summary>
    /// Repräsentiert eine Organisation, die der tacsSuperUser erstellt hat. Es sind nicht alle Properties 
    /// vorhanden, die geliefert werden.
    /// </summary>
    public class Organisation
    {
        /// <summary>
        /// Der eindeutige Tacs-Code
        /// </summary>
        public string OrganisationId { get; set; }
        /// <summary>
        /// Die Bezeichnung der Organisation
        /// </summary>
        public string Bezeichnung { get; set; }
        /// <summary>
        /// Unter dieser URL werden die Variablen zurückgegeben, auf welche Mitarbeiter dieser Organisation 
        /// erfassen dürfen.
        /// </summary>
        public string VariablenSetUrl { get; set; }
        /// <summary>
        /// Unter dieser URL werden die Personalkategorien zurückgegeben, welche in dieser Organisation arbeiten
        /// </summary>
        public string PersonalkategorieUrl { get; set; }
        /// <summary>
        /// Unter dieser URL werden die Statistikcodes zurückgegeben, die die Mitarbeiter dieser Organisation
        /// ihren Leistungen zuweisen können
        /// </summary>
        public string StatistikCodeUrl { get; set; }
        /// <summary>
        /// Unter dieser URL werden die Nutzniesser zurückgegeben, die die Mitarbeiter dieser Organisation
        /// ihren Leistungen zuweisen können
        /// </summary>
        public string NutzniesserUrl { get; set; }
    }
}
