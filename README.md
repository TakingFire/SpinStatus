# SpinStatus

This plugin exposes real-time game information for Spin Rhythm XD using a local WebSocket, inspired by [HttpSiraStatus](https://github.com/denpadokei/HttpSiraStatus/tree/master). \
Also included in this repository are example [Overlays](Overlays/) that demonstrate the plugin.

## Installing

1. This plugin requires [BepInEx](https://github.com/BepInEx/BepInEx). Follow [these instructions](https://steamcommunity.com/sharedfiles/filedetails/?id=3339937862) to install BepInEx for Spin Rhythm.
2. Download the [latest release](https://github.com/TakingFire/SpinStatus/releases/latest), and put it in the `BepInEx/plugins` folder.
3. Launch Spin Rhythm, and a compatible overlay. Or, [make your own](README.md#using-the-plugin)!

## Using the Plugin

**SpinStatus** does not display anything directly. Rather, it exposes information that can be used to create custom, dynamic interfaces. \
For a detailed overview of this information, see the [protocol.md](Protocol.md) file. For practical examples, see the included [overlays](Overlays/).

> **Note:** This plugin is under active development. Expect frequent changes to the data format and structure!
