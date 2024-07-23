using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Glamaholic {
    internal class GameFunctions : IDisposable {
        #region Dynamic
        private static class FunctionDelegates {
            internal unsafe delegate void SetGlamourPlateSlotDelegate(AgentMiragePrismMiragePlate* agent, MirageSource mirageSource, int slotOrCabinetId, uint itemId, byte stain1, byte stain2);

            internal unsafe delegate void SetGlamourPlateSlotStainsDelegate(AgentMiragePrismMiragePlate* agent, InventoryItem* stain1Item, byte stain1Idx, uint stain1ItemId, InventoryItem* stain2Item, byte stain2Idx, uint stain2ItemId);

            internal unsafe delegate int GetCabinetItemIdDelegate(Cabinet* _this, uint baseItemId);
        }

        /*
         * The game only calls this function when setting items from the Armoire,
         * however it can safely be used to set items from the Glamour Dresser by providing
         * the dresser slot in place of the cabinetId as the shared field is interpreted depending on source:
         * - If mirageSource is the Armoure then slotOrCabinetId should be the Cabinet Item ID fetched from GetCabinetItemId.
         * - If mirageSource is the Glamour Dresser then slotOrCabinetId should be the item slot in the dresser.
         * 
         * This function will always update the item that is currently selected in the Addon.
         * 
         * In order to set items in specific slots, you must set selectedItemSlotIdx in
         * the AgentMiragePrismMiragePlate data first.
         * 
         * Updating:
         * - TODO
         */
        [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 46 10 8B 1B")]
        private readonly FunctionDelegates.SetGlamourPlateSlotDelegate SetGlamourPlateSlotNative = null!;

        /*
         * An InventoryItem -or- item id can be provided for both stains. If using just an item id, set stain1Item/stain2Item to null.
         * 
         * Updating:
         * - Breakpoint on write to MiragePlateItem->Stain1 or MiragePlateItem->Stain2
         * - ...
         */
        [Signature("48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 48 8B 51 28")]
        private readonly FunctionDelegates.SetGlamourPlateSlotStainsDelegate SetGlamourPlateSlotStainsNative = null!;

        /* 
         * Returns the cabinet id for an item in the Armoire or 0xFFFFFFFFi64 if not found.
         * 
         * _this should always be UIState::Cabinet
         * 
         * Updating:
         * - Xrefs:
         *   - AgentCabinet_ReceiveEvent: before call to SetGlamourPlateSlot
         *   - ...
         */
        [Signature("E8 ?? ?? ?? ?? 44 8B 0B 44 8B C0")]
        private readonly FunctionDelegates.GetCabinetItemIdDelegate GetCabinetItemId = null!;

        #endregion

        private Plugin Plugin { get; }
        private readonly List<uint> _filterIds = new();
        private static List<PrismBoxCachedItem> _cachedDresserItems = [];
        private static int _dresserItemSlotsUsed = 0;

        internal GameFunctions(Plugin plugin) {
            this.Plugin = plugin;
            this.Plugin.GameInteropProvider.InitializeFromAttributes(this);

            this.Plugin.ChatGui.ChatMessage += this.OnChat;
            this.Plugin.Framework.Update += OnFrameworkUpdate;
        }

        public void Dispose() {
            this.Plugin.ChatGui.ChatMessage -= this.OnChat;
            this.Plugin.Framework.Update -= OnFrameworkUpdate;
        }

        private void OnChat(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled) {
            if (this._filterIds.Count == 0 || type != XivChatType.SystemMessage) {
                return;
            }

            if (message.Payloads.Any(payload => payload is ItemPayload item && this._filterIds.Remove(item.ItemId))) {
                isHandled = true;
            }
        }

        private unsafe void OnFrameworkUpdate(IFramework framework) {
            var agent = AgentMiragePrismPrismBox.Instance();
            if (agent == null)
                return;

            if (!agent->IsAddonReady() || agent->Data == null)
                return;

            if (agent->Data->UsedSlots == _dresserItemSlotsUsed)
                return;

            _cachedDresserItems.Clear();
            foreach (var item in agent->Data->PrismBoxItems) {
                if (item.ItemId == 0 || item.Slot >= 800)
                    continue;

                _cachedDresserItems.Add(new PrismBoxCachedItem {
                    Name = item.Name.ToString(),
                    Slot = item.Slot,
                    ItemId = item.ItemId,
                    IconId = item.IconId,
                    Stain1 = item.Stains[0],
                    Stain2 = item.Stains[1],
                });
            }

            _dresserItemSlotsUsed = agent->Data->UsedSlots;
        }

        private static unsafe AgentMiragePrismMiragePlate* MiragePlateAgent => AgentMiragePrismMiragePlate.Instance();

        internal unsafe Cabinet* Armoire => &UIState.Instance()->Cabinet;

        internal unsafe bool ArmoireLoaded => this.Armoire->IsCabinetLoaded();

        internal unsafe string? ExamineName => UIState.Instance()->Inspect.NameString;

        internal static unsafe List<PrismBoxCachedItem> DresserContents {
            get => _cachedDresserItems;
        }

        internal static unsafe Dictionary<PlateSlot, SavedGlamourItem>? CurrentPlate {
            get {
                var agent = MiragePlateAgent;
                if (agent == null) {
                    return null;
                }

                var data = *(AgentMiragePrismMiragePlateData**) ((nint) agent + 0x28);
                if (data == null)
                    return null;

                var plate = new Dictionary<PlateSlot, SavedGlamourItem>();
                foreach (var slot in Enum.GetValues<PlateSlot>()) {
                    ref var item = ref data->Items[(int) slot];

                    if (item.ItemId == 0)
                        continue;

                    var stain1 =
                        item.PreviewStain1 != 0
                            ? item.PreviewStain1
                            : item.Stain1;

                    var stain2 =
                        item.PreviewStain2 != 0
                            ? item.PreviewStain2
                            : item.Stain2;

                    plate[slot] = new SavedGlamourItem {
                        ItemId = item.ItemId,
                        Stain1 = stain1,
                        Stain2 = stain2,
                    };
                }

                return plate;
            }
        }

        internal unsafe void SetGlamourPlateSlotItem(MirageSource source, int slotOrCabinetId, uint itemId, byte stainId, byte stainId2) {
            SetGlamourPlateSlotNative(MiragePlateAgent, source, slotOrCabinetId, itemId, stainId, stainId2);
        }

        internal unsafe void SetGlamourPlateSlotStains(PlateSlot slot, byte stain1Idx, uint stain1Item, byte stain2Idx, uint stain2Item) {
            SetGlamourPlateSlotStainsNative(MiragePlateAgent, null, stain1Idx, stain1Item, null, stain2Idx, stain2Item);
        }

        internal unsafe bool IsInArmoire(uint itemId) {
            var row = this.Plugin.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Cabinet>()!.FirstOrDefault(row => row.Item.Row == itemId);
            if (row == null) {
                return false;
            }

            return this.Armoire->IsItemInCabinet((int) row.RowId);
        }

        internal unsafe uint? ArmoireIndexIfPresent(uint itemId) {
            var row = this.Plugin.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Cabinet>()!.FirstOrDefault(row => row.Item.Row == itemId);
            if (row == null) {
                return null;
            }

            var isInArmoire = this.Armoire->IsItemInCabinet((int) row.RowId);
            return isInArmoire
                ? row.RowId
                : null;
        }

        internal unsafe void LoadPlate(SavedPlate plate) {
            var agent = MiragePlateAgent;
            if (agent == null) {
                return;
            }

            var data = *(AgentMiragePrismMiragePlateData**) ((nint) agent + 0x28);
            if (data == null)
                return;

            var dresser = DresserContents;
            var current = CurrentPlate;
            var usedStains = new Dictionary<(uint, uint), uint>();

            var initialSlot = data->SelectedItemIndex;
            foreach (var (slot, item) in plate.Items) {
                if (current != null && current.TryGetValue(slot, out var currentItem)) {
                    if (currentItem.ItemId == item.ItemId
                        && currentItem.Stain1 == item.Stain1
                        && currentItem.Stain2 == item.Stain2) {
                        // ignore already-correct items
                        continue;
                    }
                }

                data->SelectedItemIndex = (uint) slot;
                if (item.ItemId == 0) {
                    uint previousContextSlot = data->ContextMenuItemIndex;
                    data->ContextMenuItemIndex = (uint) slot;

                    AtkValue rv;
                    agent->ReceiveEvent(&rv, null, 0, 1); // "Remove Item Image from Plate"

                    data->ContextMenuItemIndex = previousContextSlot;
                    continue;
                }

                var source = MirageSource.GlamourDresser;

                var info = (0, 0u, (byte) 0, (byte) 0);

                // find an item in the dresser that matches
                var matchingIds = dresser.FindAll(mirage => mirage.ItemId % Util.HqItemOffset == item.ItemId);
                if (matchingIds.Count == 0) {
                    // if not in the glamour dresser, look in the armoire
                    if (this.ArmoireIndexIfPresent(item.ItemId) is { } armoireIdx) {
                        source = MirageSource.Armoire;
                        int cabinetId = GetCabinetItemId(&UIState.Instance()->Cabinet, item.ItemId);

                        info = (cabinetId, item.ItemId, 0, 0);
                    }
                } else {
                    // try to find an item with matching stains
                    var idx = matchingIds.FindIndex(mirage =>
                        mirage.Stain1 == item.Stain1
                        && mirage.Stain2 == item.Stain2);

                    if (idx == -1)
                        idx = 0;

                    var mirage = matchingIds[idx];
                    info = ((int) mirage.Slot, mirage.ItemId, mirage.Stain1, mirage.Stain2);
                }

                if (info.Item1 == 0) {
                    continue;
                }

                SetGlamourPlateSlotItem(
                    source,
                    info.Item1, // slot or cabinet id
                    info.Item2, // item id
                    info.Item3, // stain 1
                    info.Item4  // stain 2
                );

                // TODO
                if (item.Stain1 != info.Item3 || item.Stain2 != info.Item4) {
                    // mirage in dresser did not have stain for this item, so apply it
                    this.ApplyStains(slot, item, usedStains);
                }
            }

            // restore initial slot, since changing this does not update the ui
            data->SelectedItemIndex = initialSlot;
        }

        private static readonly InventoryType[] PlayerInventories = {
            InventoryType.Inventory1,
            InventoryType.Inventory2,
            InventoryType.Inventory3,
            InventoryType.Inventory4,
        };

        private unsafe uint SelectStainItemId(byte stainId, Dictionary<(uint, uint), uint> usedStains) {
            var inventory = InventoryManager.Instance();
            var transient = this.Plugin.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.StainTransient>()!.GetRow(stainId);

            uint itemId = 0;

            var items = new[] { transient?.Item1?.Value, transient?.Item2?.Value };
            foreach (var dyeItem in items) {
                if (dyeItem == null || dyeItem.RowId == 0) {
                    continue;
                }

                if (itemId == 0) {
                    // use the first one (free one) as placeholder
                    itemId = dyeItem.RowId;
                }

                foreach (var type in PlayerInventories) {
                    var inv = inventory->GetInventoryContainer(type);
                    if (inv == null) {
                        continue;
                    }

                    for (var i = 0; i < inv->Size; i++) {
                        var address = ((uint) type, (uint) i);
                        var invItem = inv->Items[i];

                        if (invItem.ItemId != dyeItem.RowId) {
                            continue;
                        }

                        if (usedStains.TryGetValue(address, out var numUsed) && numUsed >= invItem.Quantity) {
                            continue;
                        }

                        // first one that we find in the inventory is the one we'll use
                        itemId = dyeItem.RowId;

                        if (usedStains.ContainsKey(address)) {
                            usedStains[address] += 1;
                        } else {
                            usedStains[address] = 1;
                        }

                        goto NoBreakLabels;
                    }
                }

            NoBreakLabels:
                {
                }
            }

            return itemId;
        }

        private unsafe void ApplyStains(PlateSlot slot, SavedGlamourItem item, Dictionary<(uint, uint), uint> usedStains) {
            var stain1ItemId =
                item.Stain1 != 0
                ? SelectStainItemId(item.Stain1, usedStains)
                : 0;

            var stain2ItemId =
                item.Stain2 != 0
                ? SelectStainItemId(item.Stain2, usedStains)
                : 0;

            // TODO: should this be the correct behaviour?
            // Perhaps we should just omit one of the stains if not found
            if (stain1ItemId == 0 || stain2ItemId == 0)
                return;

            SetGlamourPlateSlotStains(slot, item.Stain1, stain1ItemId, item.Stain2, stain2ItemId);
        }

        internal void TryOn(uint itemId, byte stainId, byte stainId2, bool suppress = true) {
            if (suppress) {
                this._filterIds.Add(itemId);
            }

            AgentTryon.TryOn(0, itemId % Util.HqItemOffset, stainId, stainId2);
        }

        internal struct PrismBoxCachedItem {
            public string Name { get; set; }
            public uint Slot { get; set; }
            public uint ItemId { get; set; }
            public uint IconId { get; set; }
            public byte Stain1 { get; set; }
            public byte Stain2 { get; set; }
        }
    }
}
