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

                    // Die Daten des Grundkatalogs speichern
                    SaveData(await nutzniesser);
                    SaveData(await variablen);
                    SaveData(await statistikCodes);
                    SaveData(await organisations);

                    // Zuweisungen der Variablen, Nutzniesser und Statistikcodes zu den Organisationen abholen
                    ProcessOrganisations(await organisations);

                    // Zuweisungen der Variablen, Nutzniesser und Statistikcode zu einzelnen Anstellungen abholen
                    ProcessAnstellungen(baseNavigation.AnstellungLink);
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

                    // Die alten Daten löschen (oder deaktivieren) und mit den Änderungen ersetzen
                    DeleteAndSave(await statistikCodes);
                    DeleteAndSave(await nutzniesser);
                    DeleteAndSave(await variablen);
                    DeleteAndSave(await organisations);

                    // Zuweisungen der Variablen, Nutzniesser und Statistikcodes zu den Organisationen abholen
                    ProcessOrganisations(await organisations, changesSince);

                    // Zuweisungen der Variablen, Nutzniesser und Statistikcode zu einzelnen Anstellungen abholen
                    ProcessAnstellungen(baseNavigation.AnstellungLink, changesSince);
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
        private async void ProcessOrganisations(IEnumerable<Organisation> organisations)
        {
            foreach (var organisation in organisations)
            {
                var variablenRef = GetCollection<VariablenRef>(organisation.VariablenSetUrl);
                var statistikCodeRef = GetCollection<StatistikCodeRef>(organisation.StatistikCodeUrl);
                var nutzniesserRef = GetCollection<NutzniesserRef>(organisation.NutzniesserUrl);
                SaveData(await variablenRef, organisation.OrganisationId);
                SaveData(await statistikCodeRef, organisation.OrganisationId);
                SaveData(await nutzniesserRef, organisation.OrganisationId);

                // Zuweisungen zu Personalkategorien innerhalb einer Organisation abholen
                ProcessPersonalkategorien(organisation.PersonalkategorieUrl, organisation.OrganisationId);
            }
        }

        /// <summary>
        /// Holt alle Änderungen der Zuweisungen der Variablen, Nutzniesser und Statistikcodes zu den 
        /// Organisationen ab und speichert sie
        /// </summary>
        /// <param name="organisations">Die Organisationen, dessen Zuweisungen abgeholt werden sollen</param>
        /// <param name="changesSince">Das Datum der letzten Synchronisation</param>
        private async void ProcessOrganisations(IEnumerable<Organisation> organisations, DateTime changesSince)
        {
            foreach (var organisation in organisations)
            {
                // Den URLs den Query-Parameter changesSince hinzufügen
                var variablenUrl = AddChangesSince(organisation.VariablenSetUrl, changesSince);
                var statistikCodeUrl = AddChangesSince(organisation.StatistikCodeUrl, changesSince);
                var nutzniesserUrl = AddChangesSince(organisation.NutzniesserUrl, changesSince);

                // Die Änderungen seit changesSince abholen
                var variablenRef = GetCollection<VariablenRef>(variablenUrl);
                var nutzniesserRef = GetCollection<NutzniesserRef>(nutzniesserUrl);
                var statistikCodeRef = GetCollection<StatistikCodeRef>(statistikCodeUrl);

                // Die alten Daten löschen (oder deaktivieren) und mit den Änderungen ersetzen
                DeleteAndSave(await variablenRef, organisation.OrganisationId);
                DeleteAndSave(await nutzniesserRef, organisation.OrganisationId);
                DeleteAndSave(await statistikCodeRef, organisation.OrganisationId);

                // Zuweisungen zu Personalkategorien innerhalb einer Organisation abholen
                ProcessPersonalkategorien(organisation.PersonalkategorieUrl, organisation.OrganisationId, changesSince);
            }
        }

        /// <summary>
        /// Holt alle Zuweisungen der Variablen, Nutzniesser und Statistikcodes zu den Personalkategorien 
        /// (-typ, -gruppe) und speichert sie
        /// </summary>
        /// <param name="personalkategorieUrl">Die URL, unter welcher die Personalkategorien abgeholt 
        /// werden können</param>
        /// <param name="organisationId">Der tacs-Code der Organisation, zu welcher die Personalkategorien gehören</param>
        private async void ProcessPersonalkategorien(string personalkategorieUrl, string organisationId)
        {
            // Dies sind nur Navigationsobjekte. Deshalb macht es keinen Sinn, die zu speichern.
            var persKat = await GetCollection<PersonalkategorieNav>(personalkategorieUrl);
            foreach (var personalkategorieNav in persKat)
            {
                var variablenRef = GetCollection<VariablenRef>(personalkategorieNav.VariablenUrl);
                var nutzniesserRef = GetCollection<NutzniesserRef>(personalkategorieNav.NutzniesserUrl);
                var statistikCodeRef = GetCollection<StatistikCodeRef>(personalkategorieNav.StatistikCodeUrl);
                SaveData(await variablenRef, organisationId, personalkategorieNav.PersonalkategorieId);
                SaveData(await nutzniesserRef, organisationId, personalkategorieNav.PersonalkategorieId);
                SaveData(await statistikCodeRef, organisationId, personalkategorieNav.PersonalkategorieId);
            }
        }

        /// <summary>
        /// Holt alle Änderungen der Zuweisungen der Variablen, Nutzniesser und Statistikcodes zu den 
        /// Personalkategorien (-typ, -gruppe) und speichert sie
        /// </summary>
        /// <param name="personalkategorieUrl">Die URL, unter welcher die Personalkategorien abgeholt 
        /// werden können</param>
        /// <param name="organisationId">Der tacs-Code der Organisation, zu welcher die Personalkategorien gehören</param>
        /// <param name="changesSince">Das Datum der letzten Synchronisation</param>
        private async void ProcessPersonalkategorien(string personalkategorieUrl, string organisationId, DateTime changesSince)
        {
            personalkategorieUrl = AddChangesSince(personalkategorieUrl, changesSince);
            var persKat = await GetCollection<PersonalkategorieNav>(personalkategorieUrl);

            foreach (var nav in persKat)
            {
                var variablenUrl = AddChangesSince(nav.VariablenUrl, changesSince);
                var nutzniesserUrl = AddChangesSince(nav.NutzniesserUrl, changesSince);
                var statistikCodeUrl = AddChangesSince(nav.StatistikCodeUrl, changesSince);

                var variablenRef = GetCollection<VariablenRef>(variablenUrl);
                var nutzniesserRef = GetCollection<NutzniesserRef>(nutzniesserUrl);
                var statistikCode = GetCollection<StatistikCodeRef>(statistikCodeUrl);

                DeleteAndSave(await variablenRef, organisationId, nav.PersonalkategorieId);
                DeleteAndSave(await nutzniesserRef, organisationId, nav.PersonalkategorieId);
                DeleteAndSave(await statistikCode, organisationId, nav.PersonalkategorieId);
            }
        }

        /// <summary>
        /// Holt alle Zuweisungen der Variablen, Nutzniesser und Statistikcodes, die nur für einzelne 
        /// Anstellungen gelten und speichert sie
        /// </summary>
        /// <param name="anstellungLink">Die URL unter welcher die Anstellungen mit separaten Zuweisungen 
        /// zu finden sind</param>
        private async void ProcessAnstellungen(string anstellungLink)
        {
            // Dies sind nur Navigationsobjekte. Deshalb macht es keinen Sinn, die zu speichern.
            IEnumerable<Anstellung> anstellungen = await GetCollection<Anstellung>(anstellungLink);
            foreach (var anstellung in anstellungen)
            {
                var variablenRef = await GetCollection<VariablenRef>(anstellung.VariablenUrl);
                var nutzniesserRef = await GetCollection<NutzniesserRef>(anstellung.NutzniesserUrl);
                var statistikCodeRef = await GetCollection<StatistikCodeRef>(anstellung.StatistikCodeUrl);

                SaveData(variablenRef, anstellung.AnstellungsId);
                SaveData(nutzniesserRef, anstellung.AnstellungsId);
                SaveData(statistikCodeRef, anstellung.AnstellungsId);
            }
        }

        /// <summary>
        /// Holt alle Änderungen der Zuweisungen der Variablen, Nutzniesser und Statistikcodes, die nur 
        /// für einzelne Anstellungen gelten und speichert sie
        /// </summary>
        /// <param name="anstellungLink">Die URL unter welcher die Anstellungen mit separaten Zuweisungen 
        /// zu finden sind</param>
        /// <param name="changesSince">Das Datum der letzten Synchronisation</param>
        private async void ProcessAnstellungen(string anstellungLink, DateTime changesSince)
        {
            anstellungLink = AddChangesSince(anstellungLink, changesSince);
            var anstellungen = await GetCollection<Anstellung>(anstellungLink);
            foreach (var anstellung in anstellungen)
            {
                var variablenUrl = AddChangesSince(anstellung.VariablenUrl, changesSince);
                var nutzniesserUrl = AddChangesSince(anstellung.NutzniesserUrl, changesSince);
                var statistikCodeUrl = AddChangesSince(anstellung.StatistikCodeUrl, changesSince);

                var variablenRef = GetCollection<VariablenRef>(variablenUrl);
                var nutzniesserRef = GetCollection<NutzniesserRef>(nutzniesserUrl);
                var statistikCodeRef = GetCollection<StatistikCodeRef>(statistikCodeUrl);

                DeleteAndSave(await variablenRef, anstellung.AnstellungsId);
                DeleteAndSave(await nutzniesserRef, anstellung.AnstellungsId);
                DeleteAndSave(await statistikCodeRef, anstellung.AnstellungsId);
            }
        }

        /// <summary>
        /// Liest die Daten von der REST-Schnittstelle, deserialisiert sie und gibt sie zurück
        /// </summary>
        /// <typeparam name="T">Der Type der Daten, die abgeholt werden sollen</typeparam>
        /// <param name="url">Die URL, unter welcher die Daten zu finden sind</param>
        /// <returns>Der deserialisierte JSON-Response, mindestens eine leere Liste</returns>
        private async Task<IEnumerable<T>> GetCollection<T>(string url)
        {
            TokenResponse tokenResponse = await GetToken();
            using (HttpClient client = GetClient(tokenResponse))
            {
                HttpResponseMessage response = await client.GetAsync(url);
                // Wenn die URL korrekt ist, aber keine Daten vorhanden sind, returniert der Server 204
                IEnumerable<T> result = new List<T>(0);
                if (response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<IEnumerable<T>>(content);

                    string length = GetContentLength(response);
                    Console.WriteLine();
                    Console.WriteLine($"{result.Count()} {typeof(T).Name} geholt");
                    Console.WriteLine($"dies sind {length} bytes");
                }

                return result;
            }
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
        private static void SaveData<T>(IEnumerable<T> data, string organisationOrAnstellungId = null,
                                        string personalKategorieId = null)
        {
            // Hier können die Daten gespeichert werden.
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
        private static void DeleteAndSave<T>(IEnumerable<T> data, string organisationOrAnstellungId = null,
                                      string personalKategorieId = null)
        {
            // Hier können die Daten, die aktualisiert werden sollen, gelöscht oder deaktiviert 
            // und die neuen gespeichert werden
        }

        private static string AddChangesSince(string statistikCodeUrl, DateTime changesSince)
        {
            return string.Concat(statistikCodeUrl, "?changesSince=", changesSince.ToString("yyyy-MM-dd"));
        }

        private async Task<TokenResponse> GetToken()
        {
            if (_currentToken == null || _currentTokenExpiresAt <= DateTime.UtcNow)
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("https://auth.cloudaccess.ch");

                    var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("client_id", _clientId),
                        new KeyValuePair<string, string>("client_secret", _clientSecret),
                        new KeyValuePair<string, string>("grant_type", "client_credentials"),
                        new KeyValuePair<string, string>("scope", "code"),
                        new KeyValuePair<string, string>("resource", _resource)
                    });

                    HttpResponseMessage response = await httpClient.PostAsync("connect/token", formData);
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
