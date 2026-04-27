using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApexSystemMonitor
{
    public class GameSenseClient
    {
        private readonly HttpClient _httpClient;
        private string? _address;
        private const string GAME_NAME = "SYSTEM_MONITOR";
        private const string EVENT_NAME = "STATS";

        public GameSenseClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> InitializeAsync()
        {
            string corePropsPath = @"C:\ProgramData\SteelSeries\SteelSeries Engine 3\coreProps.json";

            if (!File.Exists(corePropsPath))
            {
                Console.WriteLine("Erreur : Le fichier coreProps.json est introuvable. SteelSeries Engine est-il lancé ?");
                return false;
            }

            try
            {
                string json = await File.ReadAllTextAsync(corePropsPath);
                using var document = JsonDocument.Parse(json);

                if (document.RootElement.TryGetProperty("address", out var addressProp))
                {
                    _address = addressProp.GetString();
                    Console.WriteLine($"Connecté au serveur GameSense à l'adresse : {_address}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la lecture de coreProps.json : {ex.Message}");
            }

            return false;
        }

        public async Task RegisterGameAsync()
        {
            var payload = new
            {
                game = GAME_NAME,
                game_display_name = "PC System Monitor",
                developer = "User"
            };

            await SendPostRequestAsync("/game_metadata", payload);
        }

        public async Task BindEventAsync()
        {
            // La configuration d'affichage pour l'écran OLED
            var payload = new
            {
                game = GAME_NAME,
                @event = EVENT_NAME,
                min_value = 0,
                max_value = 100,
                icon_id = 1, // ESSGS_EventIconId::Health
                handlers = new[]
                {
                    new
                    {
                        device_type = "screened", // L'OLED de l'Apex 7
                        zone = "one",             // Zone d'écran standard
                        mode = "screen",
                        datas = new[]
                        {
                            new
                            {
                                lines = new[]
                                {
                                    new { has_text = true, context_frame_key = "line1" },
                                    new { has_text = true, context_frame_key = "line2" }
                                }
                            }
                        }
                    }
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new KebabCaseNamingPolicy()
            };

            await SendPostRequestAsync("/bind_game_event", payload, jsonOptions);
        }

        private int _eventCounter = 0;

        public async Task SendStatsAsync(string line1, string line2)
        {
            // On incrémente la valeur pour forcer le moteur SteelSeries à rafraîchir l'écran
            // (Sinon il met le texte en cache pensant que l'événement n'a pas changé)
            _eventCounter++;
            if (_eventCounter > 100) _eventCounter = 1;

            var payload = new
            {
                game = GAME_NAME,
                @event = EVENT_NAME,
                data = new
                {
                    value = _eventCounter,
                    frame = new
                    {
                        line1 = line1,
                        line2 = line2
                    }
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new KebabCaseNamingPolicy()
            };

            await SendPostRequestAsync("/game_event", payload, jsonOptions);
        }

        public async Task StopGameAsync()
        {
            var payload = new
            {
                game = GAME_NAME
            };

            await SendPostRequestAsync("/remove_game", payload);
            Console.WriteLine("Ressources GameSense libérées (Jeu dé-enregistré).");
        }

        private async Task SendPostRequestAsync(string endpoint, object payload, JsonSerializerOptions? options = null)
        {
            if (string.IsNullOrEmpty(_address)) return;

            string url = $"http://{_address}{endpoint}";
            
            // Si pas d'options, on utilise le standard
            string json = JsonSerializer.Serialize(payload, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erreur API sur {endpoint} : {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception lors de la requête vers {endpoint} : {ex.Message}");
            }
        }
    }

    // Custom Naming Policy pour sérialiser avec des tirets (ex: device_type -> device-type)
    public class KebabCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            // On ignore le @ des mots réservés C#
            name = name.Replace("@", "");
            return name.Replace("_", "-");
        }
    }
}
