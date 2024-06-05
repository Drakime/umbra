/* Umbra.Game | (c) 2024 by Una         ____ ___        ___.
 * Licensed under the terms of AGPL-3  |    |   \ _____ \_ |__ _______ _____
 *                                     |    |   //     \ | __ \\_  __ \\__  \
 * https://github.com/una-xiv/umbra    |    |  /|  Y Y  \| \_\ \|  | \/ / __ \_
 *                                     |______//__|_|  /____  /|__|   (____  /
 *     Umbra.Game is free software: you can          \/     \/             \/
 *     redistribute it and/or modify it under the terms of the GNU Affero
 *     General Public License as published by the Free Software Foundation,
 *     either version 3 of the License, or (at your option) any later version.
 *
 *     Umbra.Game is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU Affero General Public License for more details.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using Umbra.Common;

namespace Umbra.Game;

[Service]
internal sealed class MainMenuRepository : IMainMenuRepository
{
    public readonly Dictionary<MenuCategory, MainMenuCategory> Categories = [];

    private readonly IDataManager                 _dataManager;
    private readonly ITravelDestinationRepository _travelDestinationRepository;
    private readonly IPlayer                      _player;

    public MainMenuRepository(
        IDataManager                 dataManager,
        ITravelDestinationRepository travelDestinationRepository,
        IPlayer                      player
    )
    {
        _dataManager                 = dataManager;
        _travelDestinationRepository = travelDestinationRepository;
        _player                      = player;

        dataManager.GetExcelSheet<MainCommandCategory>()!
            .ToList()
            .ForEach(
                cmd => {
                    if (cmd.Name == "" || null == Enum.GetName(typeof(MenuCategory), cmd.RowId)) return;
                    Categories[(MenuCategory)cmd.RowId] = new((MenuCategory)cmd.RowId, cmd.Name);
                }
            );

        Categories
            .Values.ToList()
            .ForEach(
                category => {
                    dataManager.GetExcelSheet<MainCommand>()!
                        .Where(cmd => cmd.MainCommandCategory?.Row == (uint)category.Category)
                        .ToList()
                        .ForEach(
                            cmd => {
                                category.AddItem(
                                    new(cmd.Name, cmd.SortID, cmd.RowId) { Icon = cmd.Icon > 0 ? (uint)cmd.Icon : null }
                                );
                            }
                        );
                }
            );

        // Add Dalamud items to the system menu.
        Categories[MenuCategory.System].AddItem(new(-998));

        Categories[MenuCategory.System]
            .AddItem(
                new(I18N.Translate("Widget.MainMenu.CustomItem.UmbraSettings"), 999, "/umbra") {
                    Icon           = SeIconChar.BoxedLetterU,
                    IconColor      = 0xFF40A0AC,
                    ItemGroupId    = "Dalamud",
                    ItemGroupLabel = "Dalamud",
                }
            );

        Categories[MenuCategory.System].AddItem(new(-1000));

        Categories[MenuCategory.System]
            .AddItem(
                new(I18N.Translate("Widget.MainMenu.CustomItem.DalamudSettings"), 1001, "/xlsettings") {
                    Icon           = SeIconChar.BoxedLetterD,
                    IconColor      = 0xFF5151FF,
                    ItemGroupId    = "Dalamud",
                    ItemGroupLabel = "Dalamud",
                }
            );

        Categories[MenuCategory.System]
            .AddItem(
                new(I18N.Translate("Widget.MainMenu.CustomItem.DalamudPlugins"), 1002, "/xlplugins") {
                    Icon           = SeIconChar.BoxedLetterD,
                    IconColor      = 0xFF5151FF,
                    ItemGroupId    = "Dalamud",
                    ItemGroupLabel = "Dalamud",
                }
            );
    }

    public List<MainMenuCategory> GetCategories()
    {
        return [.. Categories.Values];
    }

    public MainMenuCategory GetCategory(MenuCategory category)
    {
        return Categories.GetValueOrDefault(category)
            ?? throw new Exception($"Category {category} not found.");
    }

    [OnTick(interval: 500)]
    public void OnTick()
    {
        SyncTravelDestinations();

        foreach (var category in Categories) {
            category.Value.Update();
        }
    }

    private void SyncTravelDestinations()
    {
        if (!Categories.TryGetValue(MenuCategory.Travel, out var category)) return;

        var favorites = _travelDestinationRepository.Destinations.Where(d => !d.IsHousing).ToList();
        var housing   = _travelDestinationRepository.Destinations.Where(d => d.IsHousing).ToList();

        const uint eternityRingId = 8575;

        var item = _dataManager.GetExcelSheet<Item>()!.GetRow(eternityRingId);

        if (item != null) {
            MainMenuItem? entry = category.Items.FirstOrDefault(i => i.MetadataKey == "Favorite:EternityRing");

            if (entry is not null && !_player.HasItemInInventory(eternityRingId)) {
                category.RemoveItem(entry);
            } else if (entry is null && _player.HasItemInInventory(eternityRingId)) {
                category.AddItem(
                    new(item.Name, 901, () => { unsafe { Telepo.Instance()->Teleport(eternityRingId, 0); } }
                    ) {
                        MetadataKey    = "Favorite:EternityRing",
                        Name           = item.Name.ToDalamudString().TextValue,
                        ItemGroupId    = "Travel",
                        ItemGroupLabel = "Destinations",
                        Callback       = () => {
                            _player.UseInventoryItem(eternityRingId);
                        }
                    }
                );
            }
        }

        SyncTravelDestinationMenuEntries(category, favorites, "Favorite", 900);
        SyncTravelDestinationMenuEntries(category, housing,   "Housing",  950);
    }

    private void SyncTravelDestinationMenuEntries(
        MainMenuCategory        category,
        List<TravelDestination> destinations,
        string                  key,
        int                     sortIndex
    )
    {
        if (destinations.Count == 0) {
            category
                .Items.Where(item => item.MetadataKey?.StartsWith(key) ?? false)
                .ToList()
                .ForEach(category.RemoveItem);

            return;
        }

        if (null == category.Items.FirstOrDefault(item => item.MetadataKey == $"{key}:Separator")) {
            category.AddItem(new((short)sortIndex) { MetadataKey = $"{key}:Separator" });
            sortIndex++;
        }

        List<string> usedKeys = [$"{key}:Separator"];

        foreach (var dest in destinations) {
            var cacheKey     = $"{key}:{dest.GetCacheKey()}";
            var gilCost      = $"{dest.GilCost} gil";
            var existingItem = category.Items.FirstOrDefault(item => item.MetadataKey == cacheKey);

            bool isDisabled = !_player.CanUseTeleportAction
                || (dest.IsHousing && _player.CurrentWorldName != _player.HomeWorldName);

            usedKeys.Add(cacheKey);

            if (existingItem != null) {
                if (existingItem.Name != dest.Name) {
                    existingItem.Name = dest.Name;
                }

                if (existingItem.ShortKey != gilCost) {
                    existingItem.ShortKey = gilCost;
                }

                existingItem.IsDisabled = isDisabled;
                continue;
            }

            category.AddItem(
                new(
                    dest.Name,
                    (short)(sortIndex + (dest.IsHousing ? 10 : 0)),
                    () => {
                        unsafe {
                            Telepo.Instance()->Teleport(dest.Id, (byte)dest.SubId);
                        }
                    }
                ) {
                    MetadataKey    = cacheKey,
                    IsDisabled     = isDisabled,
                    ShortKey       = gilCost,
                    ItemGroupId    = "Travel",
                    ItemGroupLabel = "Destinations",
                }
            );

            sortIndex++;
        }

        // Remove unused items.
        category
            .Items
            .Where(item => (item.MetadataKey?.StartsWith(key) ?? false) && !usedKeys.Contains(item.MetadataKey))
            .ToList()
            .ForEach(category.RemoveItem);
    }
}
