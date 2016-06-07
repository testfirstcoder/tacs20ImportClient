namespace Tacs20ImportClient.JsonObjects
{
    /// <summary>
    /// Navigationsobjekt, das die URLs für die Ressourcen trägt, die für Personalkategorien (-typ, -gruppe)
    /// relevant sind 
    /// </summary>
    public class PersonalkategorieNav
    {
        /// <summary>
        /// Die Tacs-Id zur Identifikation
        /// </summary>
        public string PersonalkategorieId { get; set; }
        /// <summary>
        /// URL zur Ressource, die Statistikcodes zurückgibt, die von Mitarbeiter dieser Personalkategorie 
        /// in einer bestimmten Organisation verwendet werden dürfen
        /// </summary>
        public string StatistikCodeUrl { get; set; }
        /// <summary>
        /// URL zur Ressource, die Nutzniesser zurückgibt, die von Mitarbeiter dieser Personalkategorie 
        /// in einer bestimmten Organisation verwendet werden dürfen
        /// </summary>
        public string NutzniesserUrl { get; set; }
        /// <summary>
        /// URL zur Ressource, die Nutzniesser zurückgibt, die von Mitarbeiter dieser Personalkategorie 
        /// in einer bestimmten Organisation verwendet werden dürfen
        /// </summary>
        public string VariablenUrl { get; set; }
    }
}
