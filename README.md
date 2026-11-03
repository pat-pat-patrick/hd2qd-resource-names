# HD2QD Resource Names

A plugin of [HD2 Mod Quick Deploy](https://github.com/pat-pat-patrick/hd2-mod-cli). With one
button, it downloads two lists of
[HD2SDK Community Edition](https://github.com/Boxofbiscuits97/HD2SDK-CommunityEdition) from that
repository: the resource names, and the names of the game's archives. The manager's conflicts and
the Asset check plugin then name each resource (for example "Helmet O-3 Free Spirit", or the game
item it belongs to: "Armor: SR-24 Street Scout") instead of giving its number.

- The lists are compiled by the HD2SDK community. This plugin does not include them: each player
  downloads them from HD2SDK's own repository.
- The lists go into the manager's settings folder (`resource_names\friendlynames.txt` and
  `resource_names\archivehashes.json`), where the manager reads them. Nothing else is changed.

## Use

1. In HD2 Mod Quick Deploy, open the Plugins page (the jigsaw piece beside the gear).
2. Choose **Add plugin…** and select `HD2QD.ResourceNames.dll`, or drop the file on the page.
3. Choose **Download the name list** on the plugin's card. **Download again** gets the newest list.

On the command line: `hd2qd resource-names-fetch`.

## Build

`build.bat` runs the tests and writes `release\HD2QD.ResourceNames.dll`. It needs the .NET SDK 8
or later (`dotnet` on PATH, or `DOTNET` set to its `dotnet.exe`).

## Licence

MIT, for this plugin's code. The name list belongs to its authors and is not part of this plugin.
