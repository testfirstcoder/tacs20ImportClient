using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tacs20ImportClient.JsonObjects;

namespace Tacs20ImportClient
{
    public class ApiClient
    {
        #region fields
        private TokenResponse _currentToken;
        private DateTime _currentTokenExpiresAt;
        private readonly string _clientId = Credentials.ClientId;
        private readonly string _clientSecret = Credentials.ClientSecret;
        private readonly string _resource = Credentials.Resource;
        #endregion

        #region public methods
        /// <summary>
        /// Hier wird demonstriert, die der initiale Import funktionieren kann. Dies muss nur einmal nach
        /// der ersten Konfiguration des Mandanten geschehen, oder wenn die ganze Konfiguration neu 
        /// eingelesen werden soll.
        /// </summary>
        /// <returns>Ein Task für die Asynchronität</returns>
        public async Task GetCompleteImport()
        {
            var tokenResponse = await GetToken();
            using (var client = GetClient(tokenResponse))
            {
                var responseMessage = await client.GetAsync("api/v1");
                if (responseMessage.IsSuccessStatusCode)
                {
                    string content = await responseMessage.Content.ReadAsStringAsync();
                    var baseNavigation = JsonConvert.DeserializeObject<BaseNavigation>(content);

                    // Mit bestimmten URLs der BaseNavigation der Grundkatalog abholen
                    var statistikCodes = GetCollection<StatistikCodeImport>(baseNavigation.StatistikCodeUrl);
                    var nutzniesser = GetCollection<Nutzniesser>(baseNavigation.NutzniesserUrl);
                    var variablen = GetCollection<Variable>(baseNavigation.VariablenUrl);
                    var organisations = GetCollection<Organisation>(baseNavigation.OrganisationUrl);

                    // Liste um alle Tasks zu synchronisieren.
                    List<Task> allTasks = new List<Task>();
                    // Die Daten des Grundkatalogs speichern
                    allTasks.Add(SaveData(nutzniesser));
                    allTasks.Add(SaveData(variablen));
                    allTasks.Add(SaveData(statistikCodes));
                    allTasks.Add(SaveData(organisations));

                    // Zuweisungen der Variablen, Nutzniesser und Statistikcodes zu den Organisationen abholen
                    ProcessOrganisations(await organisations);

                    // Die Mappings zu anderen Katalogen importieren
                    allTasks.Add(GetMappings(baseNavigation.MappingsUrl));

                    // Massnahmen sind Variablen der tiefsten Hierarchiestufe und werden zusammen mit den 
                    // Variablen zurückgegeben. Sie werden jedoch auch zu Dokumentationszwecken verwendet.
                    // Deshalb bietet die API als Vereinfachung zusätzlich diese Ressource
                    allTasks.Add(GetMassnahmen(baseNavigation.MassnahmenUrl));

                    Task.WaitAll(allTasks.ToArray());
                }
            }
        }

        /// <summary>
        /// Nach dem ersten Export der Anstellungen können in der Administration Zuweisungen zu einzelnen 
        /// Anstellungen vorgenommen werden. Sobald das geschehen ist, können diese Zuweisungen importiert werden.
        /// </summary>
        /// <returns>Ein Task für die Asynchronität</returns>
        public async Task GetEmploymentAssignments()
        {
            var tokenResponse = await GetToken();
            using (var client = GetClient(tokenResponse))
            {
                var responseMessage = await client.GetAsync("api/v1");
                if (responseMessage.IsSuccessStatusCode)
                {
                    string content = await responseMessage.Content.ReadAsStringAsync();
                    var baseNavigation = JsonConvert.DeserializeObject<BaseNavigation>(content);

                    // Zuweisungen der Variablen, Nutzniesser und Statistikcode zu einzelnen Anstellungen abholen
                    ProcessAnstellungen(baseNavigation.AnstellungLink).Wait();
                }
            }
        }

        /// <summary>
        /// Hier wird demonstriert, wie die Aktualisierung der Daten funktionieren kann. Dies ist der 
        /// Normalfall.
        /// </summary>
        /// <param name="changesSince">Das Datum des letzten Imports.</param>
        /// <returns>Ein Task für die Asynchronität</returns>
        public async Task GetChanges(DateTime changesSince)
        {
            var tokenResponse = await GetToken();
            using (var client = GetClient(tokenResponse))
            {
                var response = await client.GetAsync("api/v1");
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var baseNavigation = JsonConvert.DeserializeObject<BaseNavigation>(content);

                    // Den URLs den Query-Parameter changesSince hinzufügen
                    string statistikCodeUrl = AddChangesSince(baseNavigation.StatistikCodeUrl, changesSince);
                    string nutzniesserUrl = AddChangesSince(baseNavigation.NutzniesserUrl, changesSince);
                    string variablenUrl = AddChangesSince(baseNavigation.VariablenUrl, changesSince);
                    string organisationsUrl = AddChangesSince(baseNavigation.OrganisationUrl, changesSince);

                    // Mit bestimmten URLs der BaseNavigation die Änderungen des Grundkatalogs abholen
                    var statistikCodes = GetCollection<StatistikCodeImport>(statistikCodeUrl);
                    var nutzniesser = GetCollection<Nutzniesser>(nutzniesserUrl);
                    var variablen = GetCollection<Variable>(variablenUrl);
                    var organisations = GetCollection<Organisation>(organisationsUrl);

                    // Liste um alle Tasks zu synchronisieren.
                    var allTasks = new List<Task>();
                    // Die alten Daten löschen (oder deaktivieren) und mit den Änderungen ersetzen
                    allTasks.Add(DeleteAndSave(statistikCodes));
                    allTasks.Add(DeleteAndSave(nutzniesser));
                    allTasks.Add(DeleteAndSave(variablen));
                    allTasks.Add(DeleteAndSave(organisations));

                    // Zuweisungen der Variablen, Nutzniesser und Statistikcodes zu den Organisationen abholen
                    ProcessOrganisations(await organisations, changesSince);

                    // Zuweisungen der Variablen, Nutzniesser und Statistikcode zu einzelnen Anstellungen abholen
                    allTasks.Add(ProcessAnstellungen(baseNavigation.AnstellungLink, changesSince));

                    // Mappings ändern sich nur, wenn sich auch im Grundkatalog der Variablen etwas geändert hat.
                    if ((await variablen).Any())
                    {
                        allTasks.Add(GetMappings(baseNavigation.MappingsUrl));
                    }

                    Task.WaitAll(allTasks.ToArray());
                }
            }
        }
        #endregion

        #region private methods
        /// <summary>
        /// Holt alle Zuweisungen der Variablen, Nutzniesser und Statistikcodes zu den Organisationen ab 
        /// und speichert sie
        /// </summary>
        /// <param name="organisations">Die Organisationen, dessen Zuweisungen abgeholt werden sollen</param>
        private void ProcessOrganisations(IEnumerable<Organisation> organisations)
        {
            // Liste um alle Tasks zu synchronisieren.
            var allTasks = new List<Task>();
            foreach (var organisation in organisations)
            {
                // Die Zuweisungen vom Server holen
                var variablenRef = GetCollection<VariablenRef>(organisation.VariablenSetUrl);
                var statistikCodeRef = GetCollection<StatistikCodeRef>(organisation.StatistikCodeUrl);
                var nutzniesserRef = GetCollection<NutzniesserRef>(organisation.NutzniesserUrl);

                // Die Daten speichern
                allTasks.Add(SaveData(variablenRef, organisation.OrganisationId));
                allTasks.Add(SaveData(statistikCodeRef, organisation.OrganisationId));
                allTasks.Add(SaveData(nutzniesserRef, organisation.OrganisationId));

                // Zuweisungen zu Personalkategorien innerhalb einer Organisation abholen
                string url = organisation.PersonalkategorieUrl;
                string id = organisation.OrganisationId;
                allTasks.Add(ProcessPersonalkategorien(url, id));

            }

            Task.WaitAll(allTasks.ToArray());
        }

        /// <summary>
        /// Holt alle Änderungen der Zuweisungen der Variablen, Nutzniesser und Statistikcodes zu den 
        /// Organisationen ab und speichert sie
        /// </summary>
        /// <param name="organisations">Die Organisationen, dessen Zuweisungen abgeholt werden sollen</param>
        /// <param name="changesSince">Das Datum der letzten Synchronisation</param>
        private void ProcessOrganisations(IEnumerable<Organisation> organisations, DateTime changesSince)
        {
            // Liste um alle Tasks zu synchronisieren.
            var allTasks = new List<Task>();

            foreach (var organisation in organisations)
            {
                // Den URLs den Query-Parameter changesSince hinzufügen
                // Wenn es keine Änderungen gibt, ist die URL nicht belegt 
                // (wird in einer späteren Version implementiert)
                var variablenUrl = string.IsNullOrEmpty(organisation.VariablenSetUrl)
                    ? null
                    : AddChangesSince(organisation.VariablenSetUrl, changesSince);
                var statistikCodeUrl = string.IsNullOrEmpty(organisation.StatistikCodeUrl)
                    ? null
                    : AddChangesSince(organisation.StatistikCodeUrl, changesSince);
                var nutzniesserUrl = string.IsNullOrEmpty(organisation.NutzniesserUrl)
                    ? null
                    : AddChangesSince(organisation.NutzniesserUrl, changesSince);

                // Die Änderungen seit changesSince abholen
                var variablenRef = GetCollection<VariablenRef>(variablenUrl);
                var nutzniesserRef = GetCollection<NutzniesserRef>(nutzniesserUrl);
                var statistikCodeRef = GetCollection<StatistikCodeRef>(statistikCodeUrl);

                // Die alten Daten löschen (oder deaktivieren) und mit den Änderungen ersetzen
                allTasks.Add(DeleteAndSave(variablenRef, organisation.OrganisationId));
                allTasks.Add(DeleteAndSave(nutzniesserRef, organisation.OrganisationId));
                allTasks.Add(DeleteAndSave(statistikCodeRef, organisation.OrganisationId));

                // Zuweisungen zu Personalkategorien innerhalb einer Organisation abholen
                string url = organisation.PersonalkategorieUrl;
                string organisationsId = organisation.OrganisationId;
                allTasks.Add(ProcessPersonalkategorien(url, organisationsId, changesSince));
            }

            Task.WaitAll(allTasks.ToArray());
        }

        /// <summary>
        /// Holt alle Zuweisungen der Variablen, Nutzniesser und Statistikcodes zu den Personalkategorien 
        /// (-typ, -gruppe) und speichert sie
        /// </summary>
        /// <param name="url">Die URL, unter welcher die Personalkategorien abgeholt 
        /// werden können</param>
        /// <param name="organisationId">Der tacs-Code der Organisation, zu welcher die Personalkategorien gehören</param>
        private async Task ProcessPersonalkategorien(string url, string organisationId)
        {
            // Personalkategorien sind nur Navigationsobjekte. Deshalb macht es keinen Sinn, die zu speichern.
            var persKat = await GetCollection<PersonalkategorieNav>(url);

            // Liste um alle Tasks zu synchronisieren.
            var allTasks = new List<Task>();
            foreach (var kategorieNav in persKat)
            {
                // Die Zuweisungen vom Server holen
                var variablen = GetCollection<VariablenRef>(kategorieNav.VariablenUrl);
                var nutzniesser = GetCollection<NutzniesserRef>(kategorieNav.NutzniesserUrl);
                var codeRef = GetCollection<StatistikCodeRef>(kategorieNav.StatistikCodeUrl);

                // Die Daten speichern
                string id = kategorieNav.PersonalkategorieId;
                allTasks.Add(SaveData(variablen, organisationId, id));
                allTasks.Add(SaveData(nutzniesser, organisationId, id));
                allTasks.Add(SaveData(codeRef, organisationId, id));
            }

            Task.WaitAll(allTasks.ToArray());
        }

        /// <summary>
        /// Holt alle Änderungen der Zuweisungen der Variablen, Nutzniesser und Statistikcodes zu den 
        /// Personalkategorien (-typ, -gruppe) und speichert sie
        /// </summary>
        /// <param name="url">Die URL, unter welcher die Personalkategorien abgeholt 
        /// werden können</param>
        /// <param name="organisationId">Der tacs-Code der Organisation, zu welcher die Personalkategorien gehören</param>
        /// <param name="changesSince">Das Datum der letzten Synchronisation</param>
        private async Task ProcessPersonalkategorien(string url, string organisationId, DateTime changesSince)
        {
            url = AddChangesSince(url, changesSince);
            var persKat = await GetCollection<PersonalkategorieNav>(url);

            // Liste um alle Tasks zu synchronisieren.
            var allTasks = new List<Task>();
            foreach (var nav in persKat)
            {
                // Den URLs den Query-Parameter changesSince hinzufügen
                // Wenn es keine Änderungen gibt, ist die URL nicht belegt 
                // (wird in einer späteren Version implementiert)
                var variablenUrl = string.IsNullOrEmpty(nav.VariablenUrl)
                    ? null
                    : AddChangesSince(nav.VariablenUrl, changesSince);
                var nutzniesserUrl = string.IsNullOrEmpty(nav.NutzniesserUrl)
                    ? null
                    : AddChangesSince(nav.NutzniesserUrl, changesSince);
                var statistikCodeUrl = string.IsNullOrEmpty(nav.StatistikCodeUrl)
                    ? null
                    : AddChangesSince(nav.StatistikCodeUrl, changesSince);

                // Die Änderungen seit changesSince abholen
                var variablenRef = GetCollection<VariablenRef>(variablenUrl);
                var nutzniesserRef = GetCollection<NutzniesserRef>(nutzniesserUrl);
                var statistikCode = GetCollection<StatistikCodeRef>(statistikCodeUrl);

                // Die alten Daten löschen (oder deaktivieren) und mit den Änderungen ersetzen
                allTasks.Add(DeleteAndSave(variablenRef, organisationId, nav.PersonalkategorieId));
                allTasks.Add(DeleteAndSave(nutzniesserRef, organisationId, nav.PersonalkategorieId));
                allTasks.Add(DeleteAndSave(statistikCode, organisationId, nav.PersonalkategorieId));
            }

            Task.WaitAll(allTasks.ToArray());
        }

        /// <summary>
        /// Holt alle Zuweisungen der Variablen, Nutzniesser und Statistikcodes, die nur für einzelne 
        /// Anstellungen gelten und speichert sie
        /// </summary>
        /// <param name="anstellungLink">Die URL unter welcher die Anstellungen mit separaten Zuweisungen 
        /// zu finden sind</param>
        private async Task ProcessAnstellungen(string anstellungLink)
        {
            // Anstellungen sind nur Navigationsobjekte. Deshalb macht es keinen Sinn, die zu speichern.
            IEnumerable<Anstellung> anstellungen = await GetCollection<Anstellung>(anstellungLink);

            // Liste um alle Tasks zu synchronisieren.
            List<Task> allTasks = new List<Task>();
            foreach (var anstellung in anstellungen)
            {
                // Die Zuweisungen vom Server holen
                var variablenRef = GetCollection<VariablenRef>(anstellung.VariablenUrl);
                var nutzniesserRef = GetCollection<NutzniesserRef>(anstellung.NutzniesserUrl);
                var statistikCodeRef = GetCollection<StatistikCodeRef>(anstellung.StatistikCodeUrl);

                // Die Daten speichern
                allTasks.Add(SaveData(variablenRef, anstellung.AnstellungsId));
                allTasks.Add(SaveData(nutzniesserRef, anstellung.AnstellungsId));
                allTasks.Add(SaveData(statistikCodeRef, anstellung.AnstellungsId));
            }

            Task.WaitAll(allTasks.ToArray());
        }

        /// <summary>
        /// Holt alle Änderungen der Zuweisungen der Variablen, Nutzniesser und Statistikcodes, die nur 
        /// für einzelne Anstellungen gelten und speichert sie
        /// </summary>
        /// <param name="anstellungLink">Die URL unter welcher die Anstellungen mit separaten Zuweisungen 
        /// zu finden sind</param>
        /// <param name="changesSince">Das Datum der letzten Synchronisation</param>
        private async Task ProcessAnstellungen(string anstellungLink, DateTime changesSince)
        {
            // Anstellungen sind nur Navigationsobjekte. Deshalb macht es keinen Sinn, die zu speichern.
            anstellungLink = AddChangesSince(anstellungLink, changesSince);
            var anstellungen = await GetCollection<Anstellung>(anstellungLink);

            // Liste um alle Tasks zu synchronisieren.
            var allTasks = new List<Task>();
            foreach (var anstellung in anstellungen)
            {
                // Den URLs den Query-Parameter changesSince hinzufügen
                // Wenn es keine Änderungen gibt, ist die URL nicht belegt (wird in einer späteren Version implementiert)
                var variablenUrl = string.IsNullOrEmpty(anstellung.VariablenUrl)
                    ? null
                    : AddChangesSince(anstellung.VariablenUrl, changesSince);
                var nutzniesserUrl = string.IsNullOrEmpty(anstellung.NutzniesserUrl)
                    ? null
                    : AddChangesSince(anstellung.NutzniesserUrl, changesSince);
                var statistikCodeUrl = string.IsNullOrEmpty(anstellung.StatistikCodeUrl)
                    ? null
                    : AddChangesSince(anstellung.StatistikCodeUrl, changesSince);

                // Die Änderungen seit changesSince abholen
                var variablenRef = GetCollection<VariablenRef>(variablenUrl);
                var nutzniesserRef = GetCollection<NutzniesserRef>(nutzniesserUrl);
                var statistikCodeRef = GetCollection<StatistikCodeRef>(statistikCodeUrl);

                // Die alten Daten löschen (oder deaktivieren) und mit den Änderungen ersetzen
                allTasks.Add(DeleteAndSave(variablenRef, anstellung.AnstellungsId));
                allTasks.Add(DeleteAndSave(nutzniesserRef, anstellung.AnstellungsId));
                allTasks.Add(DeleteAndSave(statistikCodeRef, anstellung.AnstellungsId));
            }

            Task.WaitAll(allTasks.ToArray());
        }

        private static async Task GetMassnahmen(string massnahmenUrl)
        {
            // Diese Ressource wurde noch nicht umgesetzt. Die Funktionalität und der Beispielcode sind für die 
            // zweite Jahreshälfte 2016 geplant.
        }

        /// <summary>
        /// Hier wird demonstriert, wie die Mappings zu Fremdkatalogen abgeholt werden
        /// </summary>
        /// <param name="mappingsUrl">URL zur Ressource</param>
        /// <returns>Ein Task für die Asynchronität</returns>
        private static async Task GetMappings(string url)
        {
            // Diese Ressource wurde noch nicht umgesetzt. Die Funktionalität und der Beispielcode sind für die 
            // zweite Jahreshälfte 2016 geplant.
        }

        /// <summary>
        /// Liest die Daten von der REST-Schnittstelle, deserialisiert sie und gibt sie zurück
        /// </summary>
        /// <typeparam name="T">Der Type der Daten, die abgeholt werden sollen</typeparam>
        /// <param name="url">Die URL, unter welcher die Daten zu finden sind</param>
        /// <returns>Der deserialisierte JSON-Response, mindestens eine leere Liste</returns>
        private async Task<IEnumerable<T>> GetCollection<T>(string url)
        {
            IEnumerable<T> result = new List<T>(0);
            // Wenn die URL nicht gesetzt ist, gibt es keine Daten unter der Ressource
            if (string.IsNullOrEmpty(url)) return result;

            TokenResponse tokenResponse = await GetToken();
            using (HttpClient client = GetClient(tokenResponse))
            {
                HttpResponseMessage response = await client.GetAsync(url);
                // Wenn die URL korrekt ist, aber keine Daten vorhanden sind, returniert der Server 204
                if (response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<IEnumerable<T>>(content);

                    string length = GetContentLength(response);
                    Console.WriteLine();
                    Console.WriteLine($"{result.Count()} {typeof(T).Name} geholt");
                    Console.WriteLine($"dies sind {length} bytes");
                }

            }
            return result;
        }

        /// <summary>
        /// Stub-Methode, wo die Daten gespeichert werden könnten
        /// </summary>
        /// <typeparam name="T">Der Type der Daten, die gespeichert werden sollen</typeparam>
        /// <param name="data">Die Liste mit den Daten, die gespeichert werden sollen</param>
        /// <param name="organisationOrAnstellungId">Wird verwendet, wenn es sich um Zuweisungen zu 
        /// Organisationen oder Anstellungen handelt, die gespeichert werden sollen.
        /// Diese Zusammenlegung  wurde aus Bequemlichkeit gewählt</param>
        /// <param name="personalKategorieId">Wird verwendet, wenn es sich um Zuweisungen zu Personalkategorien
        /// handelt, die gespeichert werden sollen</param>
        private static async Task SaveData<T>(Task<IEnumerable<T>> data, string organisationOrAnstellungId = null,
                                        string personalKategorieId = null)
        {
            // Hier können die Daten gespeichert werden.
            await data;
        }


        /// <summary>
        /// Stub-Methode, wo die aktualisierten Daten gespeichert werden können
        /// </summary>
        /// <typeparam name="T">Der Type der Daten, die gespeichert werden sollen</typeparam>
        /// <param name="data">Die Liste mit den Daten, die gespeichert werden sollen</param>
        /// <param name="organisationOrAnstellungId">Wird verwendet, wenn es sich um Zuweisungen zu 
        /// Organisationen oder Anstellungen handelt, die gespeichert werden sollen.
        /// Diese Zusammenlegung  wurde aus Bequemlichkeit gewählt</param>
        /// <param name="personalKategorieId">Wird verwendet, wenn es sich um Zuweisungen zu Personalkategorien
        /// handelt, die gespeichert werden sollen</param>
        private static async Task DeleteAndSave<T>(Task<IEnumerable<T>> data, string organisationOrAnstellungId = null,
                                      string personalKategorieId = null)
        {
            // Hier können die Daten, die aktualisiert werden sollen, gelöscht oder deaktiviert 
            // und die neuen gespeichert werden
            await data;
        }

        private static string AddChangesSince(string url, DateTime changesSince)
        {
            return string.Concat(url, "?changesSince=", changesSince.ToString("yyyy-MM-dd"));
        }

        private async Task<TokenResponse> GetToken()
        {
            if (_currentToken == null || _currentTokenExpiresAt <= DateTime.UtcNow)
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("https://auth.cloudaccess.ch");

                    var formData = new Dictionary<string, string>
                    {
                        ["client_id"] = _clientId,
                        ["client_secret"] = _clientSecret,
                        ["grant_type"] = "client_credentials",
                        ["scope"] = "code",
                        ["resource"] = _resource
                    };

                    HttpResponseMessage response = await httpClient.PostAsync("connect/token", new FormUrlEncodedContent(formData));
                    
                    string content = await response.Content.ReadAsStringAsync();

                    _currentToken = new TokenResponse(content);
                    _currentTokenExpiresAt = DateTime.UtcNow.AddSeconds(_currentToken.ExpiresIn);
                }
            }

            return _currentToken;
        }

        private static HttpClient GetClient(TokenResponse token)
        {
            string accessToken = token.AccessToken;
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://import.tacs.ch");

            var authHeader = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Authorization = authHeader;

            var acceptHeader = new MediaTypeWithQualityHeaderValue("application/json");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(acceptHeader);

            return client;
        }

        private static string GetContentLength(HttpResponseMessage response)
        {
            IEnumerable<string> contentLenght;
            response.Content.Headers.TryGetValues("Content-Length", out contentLenght);
            var length = contentLenght.First();
            return length;
        }
        #endregion
    }
}
