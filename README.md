# SpinStatus

This plugin exposes real-time game information for Spin Rhythm XD using a local WebSocket, inspired by [HttpSiraStatus](https://github.com/denpadokei/HttpSiraStatus/tree/master). Also included in this repository are example [Overlays](Overlays/) that demonstrate the plugin.

> <img src="https://github.com/user-attachments/assets/8cd40e31-6470-4de4-9ee5-fef71a6b3159" height="250" alt="Overlay Demo gif">

## Installing

1. This plugin requires [BepInEx](https://github.com/BepInEx/BepInEx). Follow [these instructions](https://steamcommunity.com/sharedfiles/filedetails/?id=3339937862) to install BepInEx for Spin Rhythm.
2. Download the [latest release](https://github.com/TakingFire/SpinStatus/releases/latest), and put it in the `BepInEx/plugins` folder.
3. Launch Spin Rhythm, and a compatible overlay. Or, [make your own](#using-the-plugin)!

## Using the Plugin

**SpinStatus** does not display anything directly. Rather, it exposes information that can be used to create custom, dynamic interfaces.\
For a detailed overview of this information, see the [Protocol.md](Protocol.md) file. For practical examples, see the included [Overlays](Overlays/).

> **Note:** This plugin is under active development. Expect frequent changes to the data format and structure!

## Credits

**Sta:** [websocket-sharp](https://github.com/sta/websocket-sharp)\
**Bunny83:** [SimpleJSON](https://github.com/Bunny83/SimpleJSON)\
**opl-** & **denpadokei:** [HttpSiraStatus](https://github.com/denpadokei/HttpSiraStatus)\
**Xoanon:** Testing & feedback
