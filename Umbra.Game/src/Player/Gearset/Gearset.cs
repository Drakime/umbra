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
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Umbra.Game;

public sealed class Gearset(ushort id, Player player)
{
    public ushort Id { get; } = id;

    public bool IsValid { get; private set; }

    public string          Name       { get; private set; } = string.Empty;
    public byte            JobId      { get; private set; }
    public short           ItemLevel  { get; private set; }
    public bool            IsCurrent  { get; private set; }
    public GearsetCategory Category   { get; private set; } = GearsetCategory.None;
    public short           JobLevel   { get; private set; }
    public byte            JobXp      { get; private set; }
    public string          JobName    { get; private set; } = string.Empty;
    public bool            IsMaxLevel { get; private set; }

    public event Action? OnCreated;
    public event Action? OnChanged;
    public event Action? OnRemoved;

    /// <summary>
    /// Synchronizes the gearset information from the game client.
    /// </summary>
    public unsafe void Sync()
    {
        RaptureGearsetModule* gsm         = RaptureGearsetModule.Instance();
        PlayerState*          playerState = PlayerState.Instance();

        if (gsm         == null || playerState == null) {
            if (IsValid) OnRemoved?.Invoke();
            IsValid = false;
            return;
        }

        var gearset = gsm->GetGearset(Id);
        if (gearset == null || !gsm->IsValidGearset(Id)) {
            if (IsValid) OnRemoved?.Invoke();
            IsValid   = false;
            IsCurrent = false;
            return;
        }

        bool isNew = !IsValid;
        IsValid = true;

        // Intermediate values.
        bool   isChanged  = false;
        string name       = Marshal.PtrToStringAnsi((IntPtr)gearset->Name) ?? "Unknown Gearset";
        byte   jobId      = gearset->ClassJob;
        short  itemLevel  = gearset->ItemLevel;
        bool   isCurrent  = gsm->CurrentGearsetIndex == Id;
        byte   jobXp      = player.GetJobInfo(jobId).XpPercent;
        string jobName    = player.GetJobInfo(jobId).Name;
        short  jobLevel   = player.GetJobInfo(jobId).Level;
        bool   isMaxLevel = player.GetJobInfo(jobId).IsMaxLevel;

        // Check for changes.
        if (Name != name) {
            Name      = name;
            isChanged = true;
        }

        if (JobId != jobId) {
            JobId    = jobId;
            Category = GearsetCategoryRepository.GetCategoryFromJobId(jobId);
        }

        if (ItemLevel != itemLevel) {
            ItemLevel = itemLevel;
            isChanged = true;
        }

        if (IsCurrent != isCurrent) {
            IsCurrent = isCurrent;
            isChanged = true;
        }

        if (IsMaxLevel != isMaxLevel) {
            IsMaxLevel = isMaxLevel;
            isChanged  = true;
        }

        if (JobXp != jobXp) {
            JobXp     = jobXp;
            isChanged = true;
        }

        if (JobName != jobName) {
            JobName   = jobName;
            isChanged = true;
        }

        if (JobLevel != jobLevel) {
            JobLevel  = jobLevel;
            isChanged = true;
        }

        if (isNew)
            OnCreated?.Invoke();
        else if (isChanged) OnChanged?.Invoke();
    }
}
