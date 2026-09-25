# CS2-Poor-MapAdvertisements

This plugin allows for server owners to create spray type advertisements that are placed on wall.<br/>
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/H2H8TK0L9)

## [📺] Video presentation
SoonTM
<p align="center">
    <img src="img/1.jpg" width="500">
</p>

## [📌] Setup
- Download latest release,
- Download [CS2MenuManager by schwarper](https://github.com/schwarper/CS2MenuManager) - For menu API.
- Drag files to /plugins/
- Restart your server,
- Config file should be created in configs/plugins/
- Edit to your liking,

Map advert placements are stored separately in `addons/counterstrikesharp/configs/plugins/CS2_Poor_MapAdvertisements/maps/`. On first start, existing files from the old plugin-local `maps/` folder are copied there automatically.

## [📝] Configuration
| Option  | Description |
| ------------- | ------------- |
| Admin Flag (string) | Which flag will have access to all of the commands  |
| Vip Flag (string) | Which flag would not see advertisements that are not forced on vip users |
| Props Path (string[]) | Paths for all advertisements that your addon have |
| Solid Material Variants (object) | Optional map from each normal `.vmat` to its solid-color `.vmat`. Enables the Current/Solid switch without changing existing defaults. |
| Custom Position Values (int[]) | Custom values that will change position of the advert |
| Custom Angle Values (int[]) | Custom values that will change rotation of the advert |
| Enable commands (bool) | If you want commands to be enabled. (for example, after you placed all of the advertisements you might not need commands anymore) |
| Debug Mode (bool) | If plugin should log errors, etc |

### [📝] Config example:
```
{
  "Admin Flag": "@css/root",
  "Vip Flag": "@vip/noadv",
  "Props Path": [
    "models/advert1.vmdl",
    "materials/decal_1.vmat",
    "materials/advert_3.vmat",
    "materials/advert_1.vmat"
  ],
  "Solid Material Variants": {
    "materials/decal_1.vmat": "materials/decal_1_solid.vmat"
  },
  "Custom Position Values": [1,5,10],
  "Custom Angle Values": [1,5,10],
  "Enable commands": true,
  "Debug Mode": true,
  "ConfigVersion": 1
}
```

## [🛡️] Admin commands
### Optional MatchZy integration

Disabled by default. The two plugins remain usable independently; when enabled,
this plugin reads the current series map from MatchZy's CounterStrikeSharp
capability and selects one of up to five configured materials.

Configure `MatchZy Map Integration` with `Enabled: true` and up to five valid
`materials/.../*.vmat` paths. Set `Show In Best Of One` to `false` to hide only
the map-dependent slots in BO1, then reload the map. Open `css_mapadverts_matchzy`,
choose a preview material and the
new slot's width, height and depth, then enable ping placement. Each ping saves
one slot for the current map; position and rotation can be edited later.

BO1, BO3 and BO5 use one-based MAP numbers. Slots are shown to players during an
active series and previewed to admins outside a series. Stale or invalid state,
map transitions and disabled integration hide the slots. Ordinary adverts and
their storage are unchanged.

Tried to make plugin idiot proof (since I did a lot of mistakes).
| Command  | Description |
| ------------- | ------------- |
| `css_mapadverts` / `!mapadverts` | Opens the main advertisement menu for creating, editing, deleting and saving ordinary decals and props. Admin flag required. |
| `css_mapadverts_matchzy` / `!mapadverts_matchzy` | Opens the MatchZy map-slot editor for map-dependent MAP 1–5 decals. Admin flag required. |
| `css_mapadverts_edit_nearest` / `!mapadverts_edit_nearest` | Opens the editor for the closest saved ordinary decal, including position, rotation, size and deletion. Admin flag required. |
| `css_mapadverts_undo` / `!mapadverts_undo` | Removes the last ordinary advert placement made since the current map load. Admin flag required. |
| `css_mapadverts_toggle` / `!mapadverts_toggle` | Emergency global visibility switch. Hides or shows all advertisements immediately for everyone. Admin flag or server console. |
| `css_mapadverts_audience <all\|spectators>` / `!mapadverts_audience <all\|spectators>` | Chooses whether advertisements are visible to everyone or only spectators. The change is applied in real time. Admin flag or server console. `everyone` and `observers` are accepted aliases. |
| `css_ads` / `!ads` | Opens a short personal visibility menu. Available to every player without an admin flag. A one-time join hint points players to this command when adverts are configured. |
| `css_ads <auto\|hide\|show>` / `!ads <auto\|hide\|show>` | Quick options: `auto` follows the match audience, `hide` hides images only for you, and `show` displays them to you even when the match audience is spectators-only. `default`, `off`, and `on` are accepted aliases. |
| `css_mapadverts_self [auto\|hide\|show]` / `!mapadverts_self [auto\|hide\|show]` | Backwards-compatible long form of `css_ads`; no argument opens the same menu. Available to every player without an admin flag. |

All commands are disabled when `Enable commands` is set to `false`. Personal preferences apply only to the player who selected them and last until the plugin or server restarts. The global emergency switch always overrides personal visibility preferences.

Chat messages use MatchZy's configured `matchzy_chat_prefix` when the matching
optional capability is available; otherwise the plugin uses its localized
`[Map Ads]` prefix.

Decal creation and both ordinary/MatchZy editors support `Blend: Current/Solid`
when a solid variant is configured, plus per-decal opacity from 10% to 100%.
Existing placement JSON remains compatible: omitted values mean Current and 100%.

While “Spawn on Ping” is enabled, the plugin temporarily sets the server's `player_ping_token_cooldown` to `0`, then restores its previous value once no admin is placing adverts via ping. This is a global server setting for that short setup period.


## [❤️] Special thanks to:
- [CS2-SkyboxChanger by samyycX](https://github.com/samyycX/CS2-SkyboxChanger) - For function to find id of cached material.
- [Edgegamers JailBreak](https://github.com/edgegamers/Jailbreak/blob/main/mod/Jailbreak.Warden/Paint/WardenPaintBehavior.cs#L131) - For function to check if player is looking at his pretty feet.
- [CS2MenuManager](https://github.com/schwarper/CS2MenuManager) - For menu API.
- [f3nixCoding](https://github.com/f3nixCodings) - For updated function with keyvalues for decals.

### [🚨] Plugin might be poorly written and have some issues. I have no idea what I am doing, but when tested it worked fine.
