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
        public async Task GetCompleteImport()
        {
            Console.WriteLine("--- get complete import ---" + Environment.NewLine);
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
                var variablenRef = await GetCollection<VariablenRef>(organisation.VariablenSetUrl);
                var statistikCodeRef = await GetCollection<StatistikCodeRef>(organisation.StatistikCodeUrl);
                var nutzniesserRef = await GetCollection<NutzniesserRef>(organisation.NutzniesserUrl);
                SaveData(variablenRef, organisation.OrganisationId);
                SaveData(statistikCodeRef, organisation.OrganisationId);
                SaveData(nutzniesserRef, organisation.OrganisationId);

                // Zuweisungen zu Personalkategorien innerhalb einer Organisation abholen
                ProcessPersonalkategorien(organisation.PersonalkategorieUrl, organisation.OrganisationId);
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
                var variablenRef = await GetCollection<VariablenRef>(personalkategorieNav.VariablenUrl);
                var nutzniesserRef = await GetCollection<NutzniesserRef>(personalkategorieNav.NutzniesserUrl);
                var statistikCodeRef = await GetCollection<StatistikCodeRef>(personalkategorieNav.StatistikCodeUrl);
                SaveData(variablenRef, organisationId, personalkategorieNav.PersonalkategorieId);
                SaveData(nutzniesserRef, organisationId, personalkategorieNav.PersonalkategorieId);
                SaveData(statistikCodeRef, organisationId, personalkategorieNav.PersonalkategorieId);
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

        private async void SaveCollection<T>(string url)
        {
            TokenResponse tokenResponse = await GetToken();
            using (HttpClient client = GetClient(tokenResponse))
            {
                var response = await client.GetAsync(url);
                // Wenn die URL korrekt ist, aber keine Daten vorhanden sind, returniert der Server 204
                if (response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    IEnumerable<T> result = JsonConvert.DeserializeObject<IEnumerable<T>>(content);
                    // Hier können die Daten gespeichert werden

                    string length = GetContentLength(response);
                    Console.WriteLine();
                    Console.WriteLine($"{result.Count()} {typeof(T).Name} geholt");
                    Console.WriteLine($"dies sind {length} bytes");
                }
            }
        }

        private async Task<IEnumerable<T>> SaveAndReturnCollection<T>(string url)
        {
            TokenResponse tokenResponse = await GetToken();
            using (var client = GetClient(tokenResponse))
            {
                var response = await client.GetAsync(url);
                IEnumerable<T> result = new List<T>();
                if (response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<IEnumerable<T>>(content);
                    // here you can save the data

                    string length = GetContentLength(response);
                    Console.WriteLine();
                    Console.WriteLine($"{result.Count()} Organisationen geholt");
                    Console.WriteLine($"dies sind {length} bytes");
                }

                return result;
            }

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

        private HttpClient GetClient(TokenResponse token)
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
