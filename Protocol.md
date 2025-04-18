# SpinStatus Event Reference

> ⚠️ **Warning:** This data structure is **under development** and is **subject to change**.

## Endpoints

By default, the server runs on port `38304` `(S*P*I*N)`.

- `/`: Default endpoint, recieve all game events.

### Example (JS):
```js
const ws = new WebSocket("ws://localhost:38304/");
```

## Object Reference

### Event Object

Every message sent by the server follows this format.\
`type` indicates the event that was triggered, which may or may not contain a relevent `status` object. (More information below)

```js
  {
    "type":   String,
    "status": Object,
  }
```

Type | Status | Description
---- | ------ | -----------
`"hello"` | | Sent on initial connection.
`"noteEvent"` | [NoteStatus](Protocol.md#notestatus-object) | Sent when a note is hit or missed.
`"scoreEvent"` | [ScoreStatus](Protocol.md#scorestatus-object) | Sent when the score changes. Usually coincides with a `noteEvent`, but updates continuously during sustained notes.
`"trackStart"` | [TrackStatus](Protocol.md#trackstatus-object)<br>[PlayerStatus](Protocol.md#playerstatus-object) | Sent when a track starts.
`"trackEnd"` | | Sent when a track ends (returns to song list).
`"trackComplete"`<br>`"trackFail"` | | Sent when a track ends (shows results screen).
`"trackPause"`<br>`"trackResume"` | | Sent when the game is paused or resumed.

### NoteStatus Object

```js
{
  "index": Number, // Note identifier
  "lane" : Number, // Note lane
  "type" : String, // Note type
  "color": Number, // 0 (Red) | 1 (Blue)

  "accuracy": String,
  "timing"  : Number,
}
```

<details>
  <summary><strong>Note Types</strong></summary>

```js
[
  "Tap",
  "Match",
  "Drum",
  "DrumStart",
  "DrumEnd",
  "HoldStart",
  "HoldEnd",
  "SpinLeftStart"
  "SpinLeftEnd"
  "SpinRightStart",
  "SpinRightEnd",
  "ScratchStart",
  "ScratchEnd",
]
```

</details>

<details>
  <summary><strong>Accuracy Values</strong></summary>

```js
[
  "Valid", // Match, Spin
  "PerfectPlus",
  "Perfect",
  "EarlyPerfect",
  "Great",
  "EarlyGreat",
  "Good",
  "EarlyGood",
  "Okay",
  "EarlyOkay",
  "Failed",
]
```

</details>

### ScoreStatus Object

```js
{
  "score"     : Number,
  "combo"     : Number,
  "maxCombo"  : Number,
  "fullCombo" : String,
  "health"    : Number,
  "maxHealth" : Number,
  "multiplier": Number,
}
```

<details>
  <summary><strong>Combo Values</strong></summary>

```js
[
  "PerfectPlus",
  "Perfect",
  "Great",
  "Good",
  "Okay",
  "None",
]
```

</details>

### TrackStatus Object

```js
{
  "title"   : String,
  "subTitle": String
  "artist"  : String,
  "feat"    : String,
  "charter" : String,

  "startTime": Number,
  "endTime"  : Number,

  "difficulty": String,
  "isCustom"  : Boolean,
}
```

<details>
  <summary><strong>Difficulty Values</strong></summary>

```js
[
  "RemiXD",
  "XD",
  "Expert",
  "Hard",
  "Normal",
  "Easy",
]
```

</details>

### PlayerStatus Object

```js
{
  "palette": NotePalette
}
```

#### NotePalette Object

All values in formatted hex, i.e. `"#RRGGBB"`

```js
{
  "NoteA"    : String,
  "NoteB"    : String,
  "Beat"     : String,
  "SpinLeft" : String,
  "SpinRight": String,
  "Scratch"  : String,
  "Ancillary": String, // Highlights
}
```
