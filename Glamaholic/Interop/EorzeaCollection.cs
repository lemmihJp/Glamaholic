using Dalamud.Game;
using HtmlAgilityPack;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Glamaholic.Interop {
    internal class EorzeaCollection {
        private const string TITLE_NODE_PATH = "//b[contains(@class, 'b-title-text-bold')]";
        private const string ITEM_CONTAINER_PATH = "//div[contains(@class, 'b-info-box-item-wrapper')]";
        private const string ITEM_LINK_PATH = ".//a[contains(@class, 'c-gear-slot')]";
        private const string ITEM_NAME_PATH = ".//span[contains(@class, 'c-gear-slot-item-name')]";
        private const string ITEM_STAIN_PATH = ".//span[contains(@class, 'c-gear-slot-item-info-color')]";

        // The default HttpClient UA is blocked by CloudFlare
        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

        private const bool DEBUG_LOG_ENABLED = false;

        private static readonly List<(string, PlateSlot)> ItemSlotClasses = new() {
            ( "c-gear-slot-weapon", PlateSlot.MainHand ),
            ( "c-gear-slot-offhand", PlateSlot.OffHand ),
            ( "c-gear-slot-head", PlateSlot.Head ),
            ( "c-gear-slot-body", PlateSlot.Body ),
            ( "c-gear-slot-hands", PlateSlot.Hands ),
            ( "c-gear-slot-legs", PlateSlot.Legs ),
            ( "c-gear-slot-feet", PlateSlot.Feet ),
            ( "c-gear-slot-earrings", PlateSlot.Ears ),
            ( "c-gear-slot-necklace", PlateSlot.Neck ),
            ( "c-gear-slot-bracelets", PlateSlot.Wrists ),
            ( "c-gear-slot-ring", PlateSlot.LeftRing),
        };

        private static bool HasLoggedError { get; set; } = false;

        internal struct ECGlamour {
            public string Name;
            public Dictionary<PlateSlot, SavedGlamourItem> Items;
        }

        public static async Task<ECGlamour?> ImportFromURL(string url) {
            var itemSheet = Plugin.DataManager.GetExcelSheet<Item>(ClientLanguage.English);
            var stainSheet = Plugin.DataManager.GetExcelSheet<Stain>(ClientLanguage.English);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

            var resp = await httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode) {
                LogDebug($"{url} returned status code {resp.StatusCode}");
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(await resp.Content.ReadAsStringAsync());

            var root = doc.DocumentNode;

            string title = "New EC Import";
            var titleNode = root.SelectSingleNode(TITLE_NODE_PATH);
            if (titleNode != null)
                title = HttpUtility.HtmlDecode(titleNode.InnerText);

            LogDebug($"Title is {title}");

            var gearSlots = root.SelectNodes(ITEM_CONTAINER_PATH);
            if (gearSlots.Count == 0) {
                LogDebug("No gear slots found in the document");
                return null;
            }

            var items = new Dictionary<PlateSlot, SavedGlamourItem>();

            LogDebug($"Parsing {gearSlots.Count} item elements");

            bool hasRing = false;
            foreach (var element in gearSlots) {
                var item = ParseItem(element, itemSheet!, stainSheet!);
                if (item == null)
                    continue;

                PlateSlot slot = item.Value.Item1;
                if (slot == PlateSlot.LeftRing) {
                    if (hasRing) {
                        slot = PlateSlot.RightRing;
                    } else
                        hasRing = true;
                }

                items.Add(slot, item.Value.Item2);
            }

            return new ECGlamour() {
                Name = title,
                Items = items,
            };
        }

        private static (PlateSlot, SavedGlamourItem)? ParseItem(HtmlNode node, ExcelSheet<Item> itemSheet, ExcelSheet<Stain> stainSheet) {
            var slot = ParseSlotFromItemNode(node);
            if (slot == null)
                return null;

            var name = ParseNameFromItemNode(node);
            if (name == null)
                return null;

            name = HttpUtility.HtmlDecode(name);

            LogDebug($"{slot.Value}: {name}");

            name = name.ToLower();

            var itemData = itemSheet.FirstOrDefault(
                i => i.EquipSlotCategory.Row != 0
                    && i.Name.ToString().ToLower() == name,
                null);

            if (itemData == null)
                return null;

            var stains = ParseStainsFromItemNode(node, stainSheet);

            return (
                slot.Value,
                new SavedGlamourItem {
                    ItemId = itemData.RowId,
                    Stain1 = stains.Item1,
                    Stain2 = stains.Item2,
                }
            );
        }

        private static PlateSlot? ParseSlotFromItemNode(HtmlNode node) {
            var dbLinkElement = node.SelectSingleNode(ITEM_LINK_PATH);
            if (dbLinkElement == null) {
                LogParsingError("ParseSlotFromItemNode is missing the c-gear-slot element");
                return null;
            }

            PlateSlot? slot = null;
            foreach (var slotClasses in ItemSlotClasses) {
                if (dbLinkElement.HasClass(slotClasses.Item1)) {
                    slot = slotClasses.Item2;
                    break;
                }
            }

            if (slot == null) {
                LogParsingError("ParseSlotFromItemNode could not determine slot from class");
                return null;
            }

            return slot;
        }

        private static string? ParseNameFromItemNode(HtmlNode node) {
            var nameElement = node.SelectSingleNode(ITEM_NAME_PATH);
            if (nameElement == null) {
                LogParsingError("ParseNameFromItemNode is missing the name element");
                return null;
            }

            return nameElement.InnerText;
        }

        private static (byte, byte) ParseStainsFromItemNode(HtmlNode node, ExcelSheet<Stain> stainSheet) {
            var stainElements = node.SelectNodes(ITEM_STAIN_PATH);
            if (stainElements == null || stainElements.Count == 0) {
                return (0, 0);
            }

            string stain1Name = stainElements[0].InnerText.ToLower().Substring(2);
            uint stain1 = stainSheet.FirstOrDefault(s => s.RowId != 0 && s.Name.ToString().ToLower() == stain1Name, null)?.RowId ?? 0;

            LogDebug($"Stain 1: {stain1Name}");

            if (stainElements.Count == 1 || stainElements.Count == 2)
                return ((byte) stain1, 0);

            int secondIdx = stainElements.Count == 2 ? 1 : 2;

            string stain2Name = stainElements[secondIdx].InnerText.ToLower().Substring(2);
            uint stain2 = stainSheet.FirstOrDefault(s => s.RowId != 0 && s.Name.ToString().ToLower() == stain2Name, null)?.RowId ?? 0;

            LogDebug($"Stain 2: {stain2Name}");

            return ((byte) stain1, (byte) stain2);
        }

        private static void LogParsingError(string error) {
            if (HasLoggedError && !DEBUG_LOG_ENABLED)
                return;

            HasLoggedError = true;
            Plugin.Log.Error("EorzeaCollection parsing error: " + error);

            if (!DEBUG_LOG_ENABLED)
                Plugin.Log.Error("Further parsing errors will be suppressed.");

            Plugin.ChatGui.PrintError("EorzeaCollection parsing error: " + error);
            Plugin.ChatGui.PrintError("If this issue persists, please report it to the plugin developer.");

            if (!DEBUG_LOG_ENABLED)
                Plugin.ChatGui.PrintError("Further parsing errors will be suppressed.");
        }

        private static void LogDebug(string message) {
            if (!DEBUG_LOG_ENABLED)
                return;

            Plugin.Log.Info("[EorzeaCollection Debug] " + message);
        }
    }
}