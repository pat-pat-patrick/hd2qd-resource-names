# HD2QD Resource Names

A plugin of [HD2 Mod Quick Deploy](https://github.com/pat-pat-patrick/hd2-mod-cli). With one
button, it downloads the resource name list of
[HD2SDK Community Edition](https://github.com/Boxofbiscuits97/HD2SDK-CommunityEdition) from that
repository. The manager's conflicts then name each resource (for example "Helmet O-3 Free Spirit")
instead of giving its number.

- The list is compiled by the HD2SDK community. This plugin does not include it: each player
  downloads it from HD2SDK's own repository.
- The list goes into the manager's settings folder (`resource_names\friendlynames.txt`), where the
  manager reads it. Nothing else is changed.

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
