# Example Overlays

### [Accuracy Chart](AccuracyChart/)

A live-updating graph that charts note accuracy over time, with a per-lane breakdown.

### [SRXD Score Overlay](SRXD/)

A polished overlay that uses all the information currently exposed by SpinStatus.\
Includes a score counter and accuracy log that emulate the in-game HUD.

**Direct URL: [srxd.overlays.bacur.xyz](https://srxd.overlays.bacur.xyz)**\
Download: [DownGit](https://downgit.github.io/#/home?url=https://github.com/TakingFire/SpinStatus/tree/main/Overlays/SRXD)

## Usage

1. Create a new Browser Source.
2. Add the overlay URL to the source.
   - If downloading, check `Local file` and select `index.html`.
3. Set the Width and Height. For the [Score Overlay](https://srxd.overlays.bacur.xyz), a height of 400 is recommended.
4. To apply config changes, enable `Shutdown source when not visible`, then hide/show the source.

## Configuring

If you download the overlay, you may configure it directly from [`config.js`](SRXD/config.js). The changes will be applied when the overlay is refreshed.

If using the direct URL, you may add any value in [`config.js`](SRXD/config.js) to the URL. For example, to hide the accuracy log and make the overlay 50% bigger, you can use the following URL:\
[`srxd.overlays.bacur.xyz/?showAccuracyLog=false&overlayScale=1.5`](https://srxd.overlays.bacur.xyz/?showAccuracyLog=false&overlayScale=1.5)
