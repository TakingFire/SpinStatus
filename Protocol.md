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
`"noteEvent"` | [NoteEvent](Protocol.md#noteevent-object) | Sent when a note is hit or missed.
`"scoreEvent"` | [ScoreEvent](Protocol.md#scoreevent-object) | Sent when the score changes. Usually coincides with a `noteEvent`, but updates continuously during sustained notes.
`"trackStart"` | [TrackEvent](Protocol.md#trackevent-object) | Sent when a track starts.
`"trackEnd"` | | Sent when a track ends.

### NoteEvent Object

```js
  {
    // Note info
    "index": Number, // Note identifier
    "lane" : Number, // Note lane
    "type" : String, // Note type
    "color": Number, // 0 (Red) | 1 (Blue)

    // Performance
    "accuracy": String,
    "timing"  : Number,
  }
```

### ScoreEvent Object

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

### TrackEvent Object

```js
  {
    "title"   : String,
    "subTitle": String
    "artist"  : String,
    "feat"    : String,
    "charter" : String,

    "difficulty": String,
    "isCustom"  : Boolean,
  }
```
