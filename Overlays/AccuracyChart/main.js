function connect() {
  const ws = new WebSocket("ws://localhost:38304");
  ws.onopen = onOpen;
  ws.onmessage = onMessage;
  ws.onclose = onClose;
  ws.onerror = onError;
  return ws;
}

function onOpen() {
  console.log("Connected");
}

function onMessage(e) {
  const event = JSON.parse(e.data);

  switch (event["type"]) {
    case "noteEvent":
      handleNoteEvent(event["status"]);
      break;
    case "trackStart":
      handleTrackStart(event["status"]);
      break;
    case "trackComplete":
    case "trackFail":
    case "trackEnd":
      handleTrackEnd();
      break;
    case "trackPause":
      trackClock.active = false;
      break;
    case "trackResume":
      trackClock.active = true;
      break;
  }

  if (event["type"] == "noteEvent") {
  }
}

function onClose() {
  console.log("Disconnected");
}

function onError() {
  console.log("Socket Error");
}

function handleNoteEvent(event) {
  if (event["timing"] == 0) {
    if (event["accuracy"] == "Failed") {
      event["timing"] = 100 / 1000;
    } else return;
  }
  noteGraph.drawNote(event);
  timeGraph.drawNote(event);
  timingHistory.push(event["timing"]);

  if (event["timing"] < 0) earlyAvg.add(event["timing"]);
  if (event["timing"] > 0) lateAvg.add(event["timing"]);

  const $earlyAvg = document.getElementById("early-avg-val");
  const $lateAvg = document.getElementById("late-avg-val");
  const $totalAvg = document.getElementById("total-avg-val");

  const early = Math.abs(earlyAvg.get());
  const late = Math.abs(lateAvg.get());
  const total = earlyAvg.get() + lateAvg.get();

  $earlyAvg.textContent = (early * 1000).toFixed(2);
  $lateAvg.textContent = (late * 1000).toFixed(2);
  $totalAvg.textContent = (total * 1000).toFixed(2);

  if (globalConfig.coloredText) {
    $earlyAvg.style.color = Graph.getTimingColor(early * 2);
    $lateAvg.style.color = Graph.getTimingColor(late * 2);
    $totalAvg.style.color = Graph.getTimingColor(total * 2);
  }
}

function handleTrackStart(event) {
  trackClock.startTime = event["startTime"];
  trackClock.endTime = event["endTime"];
  trackClock.start();
  noteGraph.clear();
  timeGraph.clear();
  timingHistory.clear();
}

function handleTrackEnd() {
  trackClock.stop();
}

function signedCurve(x) {
  // return Math.sign(x) * Math.sqrt(Math.abs(x));
  return Math.sign(x) * (0.99 - (1 - Math.abs(x)) * (1 - Math.abs(x)));
  // return Math.sign(x) * (1 - Math.pow(1 - x, 3));
  // return Math.sign(x) * Math.sqrt(1 - Math.pow(Math.abs(x) - 1, 2));
}

class TrackClock {
  constructor() {
    this.callback = undefined;
    this.interval = 1000;
    this.active = false;
    this.startTime = 0;
    this.currentTime = 0;
    this.endTime = 0;
  }

  start() {
    this.active = true;
    this.currentTime = this.startTime;

    this.callback = setInterval(() => {
      if (!this.active) return;
      this.currentTime += this.interval / 1000;

      timeGraph.drawLine();
    }, this.interval);
  }

  stop() {
    this.active = false;
    clearInterval(this.callback) / 0.02;
  }
}

class Graph {
  constructor(id) {
    this.element = document.getElementById(id);
    this.width = this.element.getBoundingClientRect().width;
    this.height = this.element.getBoundingClientRect().height;
    this.element.width = this.width * window.devicePixelRatio;
    this.element.height = this.height * window.devicePixelRatio;
    this.ctx = this.element.getContext("2d");
    this.ctx.scale(window.devicePixelRatio, window.devicePixelRatio);
    this.ctx.translate(0.5, 0.5);
  }

  clear() {
    this.ctx.clearRect(0, 0, this.width, this.height);
  }

  drawGrid() {
    this.ctx.beginPath();
    this.ctx.globalAlpha = 1.0;
    this.ctx.lineWidth = 1.0;
    this.ctx.strokeStyle = "#fff6";
    this.ctx.moveTo(0, Math.floor(this.height / 2));
    this.ctx.lineTo(this.width, Math.floor(this.height / 2));
    this.ctx.stroke();

    this.ctx.beginPath();
    this.ctx.strokeStyle = "#fff1";
    for (let i of [20, 35, 50, 75, 100]) {
      let factor = i / 1000 / 0.105;
      if (globalConfig.magnified) factor = signedCurve(factor);

      const y = (this.height / 2) * factor;
      this.ctx.moveTo(0, this.height / 2 + y);
      this.ctx.lineTo(this.width, this.height / 2 + y);
      this.ctx.moveTo(0, this.height / 2 - y);
      this.ctx.lineTo(this.width, this.height / 2 - y);

      const textSize = Math.max(6, 16 / window.devicePixelRatio);

      this.ctx.font = `600 ${textSize}px Montserrat`;
      this.ctx.fillStyle = "#fff1";
      this.ctx.fillText(i, 0, this.height / 2 + y - 2);
      this.ctx.fillText(i, 0, this.height / 2 - y + textSize);
    }
    this.ctx.stroke();
  }

  static getTimingColor(timing) {
    const abs = Math.abs(timing);
    if (abs <= 20 / 1000) return "#faf";
    if (abs <= 35 / 1000) return "#0ff";
    if (abs <= 50 / 1000) return "#0f0";
    if (abs <= 75 / 1000) return "#ff0";
    if (abs < 100 / 1000) return "#fa0";
    return "#f04";
  }
}

class NoteGraph extends Graph {
  constructor(id) {
    super(id);
    this.drawGrid();
  }

  clear() {
    super.clear();
    this.drawGrid();
  }

  drawNote(event) {
    let factor = event["timing"] / 0.105;
    if (globalConfig.magnified) factor = signedCurve(factor);
    const x = this.width / 2 - (this.width / 9) * event["lane"];
    const y = this.height / 2 + (this.height / 2) * factor;

    this.ctx.beginPath();
    this.ctx.globalAlpha = 0.1;
    this.ctx.fillStyle = globalConfig.coloredNotes
      ? Graph.getTimingColor(event["timing"])
      : "#fff";
    this.ctx.roundRect(x - this.width / 18, y - 2, this.width / 9, 4, 2);
    this.ctx.fill();
  }
}

class TimeGraph extends Graph {
  constructor(id) {
    super(id);
    this.clear();
  }

  clear() {
    super.clear();
    this.drawGrid();
    this.previousPos = { x: 0, y: this.height / 2 };
  }

  drawLine() {
    const position =
      trackClock.currentTime / (trackClock.endTime - trackClock.startTime);
    const score = timingHistory.average();
    let factor = score / 0.105;
    if (globalConfig.magnified) factor = signedCurve(factor);
    const x = this.width * position;
    const y = this.height / 2 + factor * (this.height / 2);

    this.ctx.beginPath();
    this.ctx.moveTo(this.previousPos.x, this.previousPos.y);
    this.ctx.globalAlpha = 1.0;
    this.ctx.lineWidth = 2.0;
    this.ctx.lineCap = "round";
    this.ctx.strokeStyle = globalConfig.coloredLine
      ? Graph.getTimingColor(score * 2)
      : "#fff";
    this.ctx.lineTo(x, y);
    this.ctx.stroke();

    this.previousPos.x = x;
    this.previousPos.y = y;
  }

  drawNote(event) {
    const position =
      trackClock.currentTime / (trackClock.endTime - trackClock.startTime);
    let factor = event["timing"] / 0.105;
    if (globalConfig.magnified) factor = signedCurve(factor);
    const x = this.width * position;
    const y = this.height / 2 + (this.height / 2) * factor;

    this.ctx.beginPath();
    this.ctx.globalAlpha = 0.1;
    this.ctx.fillStyle = globalConfig.coloredNotes
      ? Graph.getTimingColor(event["timing"])
      : "#fff";
    this.ctx.roundRect(x - 2, y - 2, 4, 4, 2);
    this.ctx.fill();
  }
}

class CircularBuffer {
  constructor(size) {
    this.size = size;
    this.maxSize = size;
    this.buffer = new Array(size).fill(0);
    this.index = 0;
  }

  push(value) {
    this.buffer[this.index] = value;
    this.index = ++this.index % this.maxSize;
    if (this.size < this.maxSize) this.size++;
  }

  pop() {
    this.size--;
    this.index = (this.index - 1 + this.maxSize) % this.maxSize;
    return this.buffer[this.index];
  }

  clear() {
    // this.size = 0;
    this.buffer.fill(0);
    this.index = 0;
  }

  [Symbol.iterator]() {
    let pos = -1;
    return {
      next: () => ({
        value: this.buffer[(this.index + ++pos) % this.size],
        done: pos == this.size,
      }),
    };
  }

  average() {
    if (this.size == 0) return 0;
    return [...this].reduce((a, b) => a + b) / this.size;
  }
}

class Average {
  constructor() {
    this.clear();
  }

  add(value) {
    this.sum += value;
    this.size++;
  }

  get() {
    if (this.size == 0) return 0;
    return this.sum / this.size;
  }

  clear() {
    this.sum = 0;
    this.size = 0;
  }
}

const ws = connect();
const trackClock = new TrackClock();
const timingHistory = new CircularBuffer(40);
const noteGraph = new NoteGraph("note-graph");
const timeGraph = new TimeGraph("time-graph");
const earlyAvg = new Average();
const lateAvg = new Average();
