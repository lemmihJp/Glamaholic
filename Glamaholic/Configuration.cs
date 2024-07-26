using Dalamud.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Glamaholic {
    [Serializable]
    internal class Configuration : IPluginConfiguration {
        private const int CURRENT_VERSION = 2;

        public int Version { get; set; } = CURRENT_VERSION;

        public List<SavedPlate> Plates { get; init; } = new();
        public bool ShowEditorMenu = true;
        public bool ShowExamineMenu = true;
        public bool ShowTryOnMenu = true;
        public bool ShowKofiButton = true;
        public bool ItemFilterShowObtainedOnly;

        internal static void SanitisePlate(SavedPlate plate) {
            var valid = Enum.GetValues<PlateSlot>();
            foreach (var slot in plate.Items.Keys.ToArray()) {
                if (!valid.Contains(slot)) {
                    plate.Items.Remove(slot);
                }
            }
        }

        internal void AddPlate(SavedPlate plate) {
            SanitisePlate(plate);
            this.Plates.Add(plate);
        }

        internal static Configuration LoadAndMigrate(FileInfo fileInfo) {
            if (!fileInfo.Exists)
                return new Configuration();

            JObject cfg;

            // i hate it so much
            using (var fileStream = fileInfo.OpenRead())
            using (var textReader = new StreamReader(fileStream))
            using (var jsonReader = new JsonTextReader(textReader))
                cfg = JObject.Load(jsonReader);

            int version = cfg.Value<int>("Version");

            if (version == CURRENT_VERSION)
                return cfg.ToObject<Configuration>()!;

            CreateBackup(fileInfo);

            Plugin.Log.Info($"Migrating configuration from version {version} to {CURRENT_VERSION}");

            for (int newVersion = version + 1; newVersion <= CURRENT_VERSION; newVersion++) {
                switch (newVersion) {
                    case 2:
                        Migrate_1_2(cfg);
                        break;
                }

                cfg["Version"] = newVersion;
            }

            File.WriteAllText(fileInfo.FullName, cfg.ToString());

            return cfg.ToObject<Configuration>()!;
        }

        internal static void CreateBackup(FileInfo fileInfo) {
            var backupPath = Path.Join(fileInfo.DirectoryName, fileInfo.Name + ".bak");
            File.Copy(fileInfo.FullName, backupPath, true);
        }

        /*
         * SavedGlamourItem renamed field: StainId -> Stain1
         * SavedGlamourItem new field: Stain2
         */
        internal static void Migrate_1_2(JObject cfg) {
            if (!cfg.ContainsKey("Plates"))
                return;

            var plates = cfg["Plates"] as JArray;
            foreach (var plate in plates!) {
                var items = plate["Items"] as JObject;
                if (items == null)
                    return;

                foreach (var kvp in items) {
                    if (kvp.Key == "$type")
                        continue;

                    var item = kvp.Value as JObject;
                    if (item == null || item.ContainsKey("Stain1") || !item.ContainsKey("StainId"))
                        continue;

                    // migrate StainId to Stain1 and add new field with default value
                    item["Stain1"] = item["StainId"];
                    item["Stain2"] = 0;

                    item.Remove("StainId");
                }
            }
        }
    }

    [Serializable]
    internal class SavedPlate {
        public string Name { get; set; }
        public Dictionary<PlateSlot, SavedGlamourItem> Items { get; init; } = new();
        public List<string> Tags { get; } = new();
        public bool FillWithNewEmperor { get; set; } = false;

        public SavedPlate(string name) {
            this.Name = name;
        }

        internal SavedPlate Clone() {
            return new SavedPlate(this.Name) {
                Items = this.Items.ToDictionary(entry => entry.Key, entry => entry.Value.Clone()),
            };
        }
    }

    [Serializable]
    internal class SavedGlamourItem {
        public uint ItemId { get; set; }
        public byte Stain1 { get; set; }
        public byte Stain2 { get; set; }

        internal SavedGlamourItem Clone() {
            return new SavedGlamourItem() {
                ItemId = this.ItemId,
                Stain1 = this.Stain1,
                Stain2 = this.Stain2
            };
        }
    }
}
