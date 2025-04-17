const { animate, createAnimatable, utils, eases, stagger } = anime;

let hasReconnected = false;

function connect() {
  const ws = new WebSocket("ws://localhost:" + globalConfig.webSocketPort);
  ws.onopen = onOpen;
  ws.onmessage = onMessage;
  ws.onclose = onClose;
  ws.onerror = onError;
  return ws;
}

function onOpen() {
  console.log("Connected");
  handleSongEvent({ title: "Main Menu", artist: "SRXD" });
  if (!globalConfig.showOverlayInMenu) {
    Overlay.hideOverlay();
  }
  hasReconnected = false;
}

function onMessage(e) {
  const event = JSON.parse(e.data);

  switch (event["type"]) {
    case "noteEvent":
      handleNoteEvent(event["status"]);
      break;
    case "scoreEvent":
      handleScoreEvent(event["status"]);
      break;
    case "trackStart":
      handleTrackStart(event["status"]);
      break;
    case "trackEnd":
      handTrackEnd(event);
      break;
    case "trackPause":
    case "trackComplete":
    case "trackFail":
      if (!globalConfig.showOverlayPaused) Overlay.hideOverlay();
      break;
    case "trackResume":
      if (!globalConfig.showOverlayPaused) Overlay.showOverlay();
      break;
    default:
      console.log(event);
      break;
  }
}

function onClose() {
  console.log("Disconnected");
  handleSongEvent({ title: "Disconnected", artist: "SRXD" });
  scoreCounter.setValue(0);
  accuracyLog.clear();
  Overlay.showOverlay();

  if (!hasReconnected) {
    hasReconnected = true;
    connect();
  }
}

function onError() {
  console.log("Socket Error");
}

function handleNoteEvent(event) {
  if (
    globalConfig.showAccuracyLog &&
    !["Valid", "Pending"].includes(event["accuracy"]) &&
    !["HoldEnd", "DrumEnd"].includes(event["type"])
  ) {
    accuracyLog.insert(event["accuracy"]);
  }

  if (globalConfig.showComboCounter && event["type"] == "Failed") {
    animate("#combo", {
      x: [0, -5, 5, 0],
      filter: [
        "hue-rotate(-60deg) saturate(2)",
        "hue-rotate(-60deg) saturate(2)",
        "hue-rotate(0deg) saturate(1)",
      ],
      ease: eases.outBack(1),
      duration: 250,
    });
  }

  if (globalConfig.showScoreCounter && globalConfig.flashScoreCounter) {
    const [$counter] = utils.$("#counter");
    if (["ScratchStart", "DrumStart", "Drum"].includes(event["type"])) return;

    const filter = event["color"]
      ? "filter: hue-rotate(-120deg)"
      : "hue-rotate(140deg)";

    animate($counter, {
      filter: [filter, "hue-rotate(0deg)"],
      duration: 250,
    });
  }
}

function handleScoreEvent(event) {
  if (globalConfig.showScoreCounter) {
    scoreCounter.setValue(event["score"]);
  }
  if (globalConfig.showComboCounter) {
    const combo = String(event["combo"]).padStart(3, "0");
    utils.$("#combo-number")[0].textContent = combo;

    [$comboType] = utils.$("#combo-type");

    switch (event["fullCombo"]) {
      case "PerfectPlus":
      case "Perfect":
        $comboType.textContent = "PFC";
        $comboType.className = "perfect";
        break;
      case "None":
        $comboType.textContent = "";
        break;
      default:
        $comboType.textContent = "FC";
        $comboType.className = "great";
    }
  }
}

function handleSongEvent(event) {
  if (!globalConfig.showSongTitle && !globalConfig.showSongArtist) {
    return;
  }

  const [$title] = utils.$("#title");
  const [$artist] = utils.$("#artist");
  const [$charter] = utils.$("#charter");
  $title.textContent = event["title"];
  $artist.textContent = event["artist"];
  $charter.textContent = event["charter"] ? "Chart: " + event["charter"] : "";

  const height = utils.get(":root", "--artist-size", false);

  animate([$title, $artist], {
    x: [0, 5 * height, -2 * height, 0],
    color: ["#fff", "#fcf", "#fff"],
    delay: stagger(50),
    duration: 500,
    ease: eases.outBack(1),
  });
}

function handleTrackStart(event) {
  handleSongEvent(event);
  scoreCounter.setValue(0);
  accuracyLog.clear();

  if (!globalConfig.showOverlayInMenu) {
    Overlay.showOverlay();
  }
}

function handTrackEnd(event) {
  utils.$("#combo-number")[0].textContent = "000";
  utils.$("#combo-type")[0].textContent = "";
  scoreCounter.setValue(0);
  accuracyLog.clear();

  if (globalConfig.showOverlayInMenu) {
    handleSongEvent({ title: "Main Menu", artist: "SRXD" });
  } else {
    Overlay.hideOverlay();
  }
}

const Overlay = {
  animation: undefined,
  isVisible: true,

  showOverlay: function () {
    if (this.isVisible) return;
    if (this.animation) {
      this.animation.cancel();
    }

    this.isVisible = true;

    this.animation = animate("#overlay", {
      opacity: [0, 1],
      duration: 250,
    });

    this.animation = animate("#overlay > *", {
      y: ["1rem", "0"],
      opacity: [0, 1],
      delay: stagger(75, { start: 250 }),
      duration: 800,
      ease: eases.outBack(2),
    });
  },

  hideOverlay: function () {
    if (!this.isVisible) return;
    if (this.animation) {
      this.animation.cancel();
    }

    this.isVisible = false;

    this.animation = animate("#overlay > *", {
      y: ["0", "1rem"],
      opacity: [1, 0],
      delay: stagger(75, { reversed: true }),
      duration: 800,
      ease: eases.outBack(2),
    });

    this.animation = animate("#overlay", {
      opacity: [1, 0],
      duration: 500,
      delay: 400,
    });
  },
};

class AccuracyLog {
  constructor(length) {
    this.length = length;
    this.log = [];
    this.elements = [];

    const [$log] = utils.$("#log");
    for (let i = 0; i < length + 1; i++) {
      const el = document.createElement("span");
      $log.appendChild(el);
      this.elements.unshift(el);
    }
    utils.set($log, {
      maxHeight:
        this.elements[0].getBoundingClientRect().height *
        (this.elements.length - 1),
    });

    this.textAnimator = createAnimatable("#log span", {
      y: { unit: "px" },
      duration: 250,
      ease: eases.outBack(2),
    });
  }

  insert(text) {
    if (this.log.length > this.length) this.log.pop();
    this.log.unshift(text);
    this.update();
  }

  update() {
    this.elements.forEach((el, i) => {
      switch (this.log[i]) {
        case "PerfectPlus":
          el.textContent = "PERFECT+";
          el.className = "perfectplus";
          break;
        case "Perfect":
        case "EarlyPerfect":
          el.textContent = "PERFECT";
          el.className = "perfect";
          break;
        case "Great":
        case "EarlyGreat":
          el.textContent = "GREAT";
          el.className = "great";
          break;
        case "Good":
        case "EarlyGood":
          el.textContent = "GOOD";
          el.className = "good";
          break;
        case "Okay":
          el.textContent = "LATE";
          el.className = "okay";
          break;
        case "EarlyOkay":
          el.textContent = "EARLY";
          el.className = "okay";
          break;
        case "Failed":
          el.textContent = "MISS";
          el.className = "miss";
          break;
        default:
          el.textContent = this.log[i];
          el.classList = "perfect";
          break;
      }

      const height = 16 * utils.get(":root", "--log-size", false);
      this.textAnimator.y(0, 0);
      this.textAnimator.y(-height, 250);
    });
  }

  clear() {
    for (let i = 0; i < this.length; i++) {
      setTimeout(() => {
        this.insert("");
      }, i * 50);
    }
  }
}

class CounterColumn {
  constructor() {
    this.value = 0;
    const [$template] = utils.$("#counter-column-template");
    const [$counter] = utils.$("#counter");
    this.element = $template.content.cloneNode(true).firstElementChild;
    $counter.appendChild(this.element);

    this.elementAnimator = createAnimatable(this.element, {
      y: { unit: "rem" },
      duration: 0,
    });

    const height = utils.get(":root", "--counter-size", false);
    const maxHeight = height * 10;

    this.valueAnimator = createAnimatable(this, {
      value: 250,
      ease: eases.outBack(0.5),
      onUpdate: () =>
        this.elementAnimator.y(
          (((-height * this.value) % maxHeight) - maxHeight) % maxHeight,
        ),
    });
  }

  setValue(value) {
    this.valueAnimator.value(value);
  }
}

class Counter {
  constructor(length) {
    this.value = 0;
    this.length = length;
    this.columns = [];

    for (let i = 0; i < length; i++) {
      this.columns.push(new CounterColumn());
    }
  }

  setValue(value) {
    this.columns.forEach((e, i) => {
      e.setValue(Math.floor(value / Math.pow(10, this.length - (i + 1))));
    });
    this.value = value;
  }

  addValue(value) {
    this.setValue(this.value + value);
  }
}

if (!globalConfig.showSongTitle) utils.set("#title", { display: "none" });
if (!globalConfig.showSongArtist) utils.set("#artist", { display: "none" });
if (!globalConfig.showCharter) utils.set("#charter", { display: "none" });
if (!globalConfig.showScoreCounter) utils.set("#counter", { display: "none" });
if (!globalConfig.showComboCounter) utils.set("#combo", { display: "none" });
if (!globalConfig.showAccuracyLog) utils.set("#log", { display: "none" });

if (globalConfig.clipLongSongText) {
  utils.set("#title, #artist, #charter", {
    overflow: "hidden",
    whiteSpace: "nowrap",
    textOverflow: "ellipsis",
  });
}

const ws = connect();
const scoreCounter = new Counter(5);
const accuracyLog = new AccuracyLog(4);

Overlay.showOverlay();
