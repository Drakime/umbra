﻿/* Umbra | (c) 2024 by Una              ____ ___        ___.
 * Licensed under the terms of AGPL-3  |    |   \ _____ \_ |__ _______ _____
 *                                     |    |   //     \ | __ \\_  __ \\__  \
 * https://github.com/una-xiv/umbra    |    |  /|  Y Y  \| \_\ \|  | \/ / __ \_
 *                                     |______//__|_|  /____  /|__|   (____  /
 *     Umbra is free software: you can redistribute  \/     \/             \/
 *     it and/or modify it under the terms of the GNU Affero General Public
 *     License as published by the Free Software Foundation, either version 3
 *     of the License, or (at your option) any later version.
 *
 *     Umbra UI is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU Affero General Public License for more details.
 */

using Dalamud.Plugin;
using System.Collections.Generic;

namespace Umbra.Widgets;

public class WidgetInfo(
    string          id,
    string          name,
    string          description,
    IExposedPlugin? plugin = null,
    List<string>?   tags   = null
)
{
    public string          Id             { get; } = id;
    public string          Name           { get; } = name;
    public string          Description    { get; } = description;
    public IExposedPlugin? RequiredPlugin { get; } = plugin;
    public List<string>    Tags           { get; } = tags ?? [];
}
