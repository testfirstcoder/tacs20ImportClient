namespace Tacs20ImportClient.JsonObjects
{
    /// <summary>
    /// Repräsentiert eine Personalkategorie aus dem tacs-Katalog. Kann auch 
    /// </summary>
    public class Personalkategorie
    {
        /// <summary>
        /// Die eindeutige Tacs-Id
        /// </summary>
        public string TacsCode { get; set; }
        /// <summary>
        /// Die Bezeichnung der Personalkategorie
        /// </summary>
        public string Bezeichnung { get; set; }
        /// <summary>
        /// Damit können die Kategorien sortiert dargestellt werden
        /// </summary>
        public int SortierNummer { get; set; }
    }
}
