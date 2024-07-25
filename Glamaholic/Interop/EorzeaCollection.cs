using Dalamud.Game;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Glamaholic.Interop {
    internal class EorzeaCollection {
        private const string BASE_URL = "https://ffxiv.eorzeacollection.com/api/glamour/";

        // The default HttpClient UA is blocked by CloudFlare
        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

        private static bool HasLoggedError { get; set; } = false;

        public static async Task<SavedPlate?> ImportFromURL(string userFacingURL) {
            string? url = ConvertURLForAPI(userFacingURL);
            if (url == null) {
                Plugin.Log.Error($"EorzeaCollection Import: Invalid URL '{userFacingURL}'");
                return null;
            }

            var itemSheet = Plugin.DataManager.GetExcelSheet<Item>(ClientLanguage.English)!;
            var stainSheet = Plugin.DataManager.GetExcelSheet<Stain>(ClientLanguage.English)!;

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

            HttpResponseMessage? resp = null;
            try {
                resp = await httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode) {
                    Plugin.Log.Warning($"EorzeaCollection Import returned status code {resp.StatusCode}:\n{await resp.Content.ReadAsStringAsync()}");
                    return null;
                }
            } catch (Exception e) {
                Plugin.Log.Warning(e, $"EorzeaCollection Import: Request failed with Exception");
                return null;
            }

            if (resp == null)
                return null;

            try {
                var glam = JsonConvert.DeserializeObject<Glamour>(await resp.Content.ReadAsStringAsync());
                SavedPlate plate = new(glam.Name);

                foreach (var gearSlot in glam.Gear) {
                    var ecSlotName = gearSlot.Key;
                    if (ecSlotName == "fashion" || ecSlotName == "facewear")
                        continue;

                    if (!gearSlot.Value.HasValue)
                        continue;

                    var plateSlot = ParseSlot(ecSlotName);
                    if (plateSlot == null)
                        continue;

                    var name = gearSlot.Value.Value.Name.ToLower();
                    var dyes = gearSlot.Value.Value.Dyes;

                    if (name.Length == 0)
                        continue;

                    var item = itemSheet.FirstOrDefault(i => i?.Name.ToString().ToLower() == name, null);
                    if (item == null) {
                        Plugin.Log.Warning($"EorzeaCollection Import: Item '{name}' not found in Item sheet");
                        continue;
                    }

                    if (dyes == null) {
                        plate.Items.Add(plateSlot.Value, new SavedGlamourItem() { ItemId = item.RowId });
                        continue;
                    }

                    byte stain1, stain2 = stain1 = 0;

                    if (dyes[0] != "none") {
                        var stain = stainSheet.FirstOrDefault(s => s?.Name.ToString().ToLower() == dyes[0], null);

                        if (stain != null) {
                            stain1 = (byte) stain.RowId;
                        } else
                            Plugin.Log.Warning($"EorzeaCollection Import: Stain '{dyes[0]}' not found in Stain sheet");
                    }

                    if (dyes.Length > 1 && dyes[1] != "none") {
                        var stain = stainSheet.FirstOrDefault(s => s?.Name.ToString().ToLower() == dyes[1], null);

                        if (stain != null) {
                            stain2 = (byte) stain.RowId;
                        } else
                            Plugin.Log.Warning($"EorzeaCollection Import: Stain '{dyes[1]}' not found in Stain sheet");
                    }

                    plate.Items.Add(plateSlot.Value, new SavedGlamourItem() { ItemId = item.RowId, Stain1 = stain1, Stain2 = stain2 });
                } // end foreach in gear

                return plate;
            } catch (Exception e) {
                Plugin.Log.Warning($"EorzeaCollection Import: Failed to parse response: {e.Message}");
                return null;
            }
        }

        private static string? ConvertURLForAPI(string userFacingURL) {
            if (!Uri.TryCreate(userFacingURL, UriKind.Absolute, out var uri))
                return null;

            if (uri.Host != "ffxiv.eorzeacollection.com")
                return null;

            var path = uri.AbsolutePath;
            if (!path.StartsWith("/glamour/"))
                return null;

            var parts = path.Split("/");
            // [0] is empty, [1] is "glamour", [2] is the ID

            if (!Int64.TryParse(parts[2], out _))
                return null;

            return BASE_URL + parts[2];
        }

        private static PlateSlot? ParseSlot(string slot) {
            switch (slot) {
                case "head":
                    return PlateSlot.Head;
                case "body":
                    return PlateSlot.Body;
                case "hands":
                    return PlateSlot.Hands;
                case "legs":
                    return PlateSlot.Legs;
                case "feet":
                    return PlateSlot.Feet;
                case "weapon":
                    return PlateSlot.MainHand;
                case "offhand":
                    return PlateSlot.OffHand;
                case "earrings":
                    return PlateSlot.Ears;
                case "necklace":
                    return PlateSlot.Neck;
                case "bracelets":
                    return PlateSlot.Wrists;
                case "left_ring":
                    return PlateSlot.LeftRing;
                case "right_ring":
                    return PlateSlot.RightRing;
                default:
                    Plugin.Log.Warning($"EorzeaCollection Import: Unknown slot '{slot}'");
                    return null;
            }
        }

        [Serializable]
        private struct Glamour {
            [JsonProperty("id")]
            public uint ID { get; private set; }

            [JsonProperty("name")]
            public string Name { get; private set; }

            [JsonProperty("character")]
            public string Character { get; private set; }

            [JsonProperty("server")]
            public string Server { get; private set; }

            [JsonProperty("gear")]
            public Dictionary<string, GlamourSlot?> Gear { get; private set; }
        }

        [Serializable]
        private struct GlamourSlot {
            [JsonProperty("name")]
            public string Name { get; private set; }

            [JsonProperty("dyes")]
            private string _Dyes { get; set; }

            public string[]? Dyes => _Dyes?.Replace("-", " ").Split(',');
        }
    }
}