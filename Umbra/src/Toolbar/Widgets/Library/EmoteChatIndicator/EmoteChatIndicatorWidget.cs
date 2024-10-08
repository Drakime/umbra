﻿using Dalamud.Interface;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using Umbra.Common;

namespace Umbra.Widgets.Library.EmoteChatIndicator;

[ToolbarWidget("EmoteChatIndicator", "Widget.EmoteChatIndicator.Name", "Widget.EmoteChatIndicator.Description")]
[ToolbarWidgetTags(["emote", "chat", "indicator"])]
internal class EmoteChatIndicatorWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    Dictionary<string, object>? configValues = null
) : IconToolbarWidget(info, guid, configValues)
{
    private IGameConfig GameConfig { get; } = Framework.Service<IGameConfig>();

    /// <inheritdoc/>
    public override WidgetPopup? Popup => null;

    /// <inheritdoc/>
    protected override IEnumerable<IWidgetConfigVariable> GetConfigVariables()
    {
        return [
            new BooleanWidgetConfigVariable(
                "OnlyShowWhenEnabled",
                I18N.Translate("Widget.EmoteChatIndicator.Config.OnlyShowWhenEnabled.Name"),
                I18N.Translate("Widget.EmoteChatIndicator.Config.OnlyShowWhenEnabled.Description"),
                true
            ) { Category = I18N.Translate("Widget.ConfigCategory.WidgetAppearance") },
            new FaIconWidgetConfigVariable(
                "OffIcon",
                I18N.Translate("Widget.EmoteChatIndicator.Config.OffIcon.Name"),
                I18N.Translate("Widget.EmoteChatIndicator.Config.OffIcon.Description"),
                FontAwesomeIcon.CommentSlash
            ) { Category = I18N.Translate("Widget.ConfigCategory.WidgetAppearance") },
            new FaIconWidgetConfigVariable(
                "OnIcon",
                I18N.Translate("Widget.EmoteChatIndicator.Config.OnIcon.Name"),
                I18N.Translate("Widget.EmoteChatIndicator.Config.OnIcon.Description"),
                FontAwesomeIcon.CommentDots
            ) { Category = I18N.Translate("Widget.ConfigCategory.WidgetAppearance") },
            ..DefaultIconToolbarWidgetConfigVariables,
        ];
    }

    /// <inheritdoc/>
    protected override void Initialize()
    {
        Node.OnMouseUp += _ => IsEmoteChatEnabled = !IsEmoteChatEnabled;
    }

    protected override void OnUpdate()
    {
        if (IsEmoteChatEnabled) {
            Node.Style.IsVisible = true;
            Node.Tooltip         = I18N.Translate("Widget.EmoteChatIndicator.Tooltip.Enabled");

            SetIcon(GetConfigValue<FontAwesomeIcon>("OnIcon"));
        } else {
            Node.Style.IsVisible = !GetConfigValue<bool>("OnlyShowWhenEnabled");
            Node.Tooltip         = I18N.Translate("Widget.EmoteChatIndicator.Tooltip.Disabled");

            SetIcon(GetConfigValue<FontAwesomeIcon>("OffIcon"));
        }

        base.OnUpdate();
    }

    private bool IsEmoteChatEnabled {
        get => GameConfig.UiConfig.GetBool("EmoteTextType");
        set => GameConfig.UiConfig.Set("EmoteTextType", value);
    }
}
