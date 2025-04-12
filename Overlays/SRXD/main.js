const { animate, createAnimatable, utils, eases, stagger } = anime;

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
  handleSongEvent({ title: "Main Menu", artist: "SRXD" });
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
      handleSongEvent(event["status"]);
      scoreCounter.setValue(0);
      log.clear();
      break;
    case "trackEnd":
      handleSongEvent({ title: "Main Menu", artist: "SRXD" });
      utils.$("#combo")[0].textContent = "000";
      scoreCounter.setValue(0);
      log.clear();
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
  log.clear();
}

function onError() {
  console.log("Socket Error");
}

function handleNoteEvent(event) {
  if (!["Valid", "Pending"].includes(event["accuracy"])) {
    log.insert(event["accuracy"]);
  }

  // const [$counter] = utils.$("#counter");
  // if (["ScratchStart", "DrumStart"].includes(event["type"])) return;

  // const filter = event["color"]
  //   ? "filter: hue-rotate(-120deg)"
  //   : "hue-rotate(140deg)";

  // animate($counter, {
  //   filter: [filter, "hue-rotate(0deg)"],
  //   duration: 150,
  // });
}

function handleScoreEvent(event) {
  scoreCounter.setValue(event["score"]);
  utils.$("#combo")[0].textContent = String(event["combo"]).padStart(3, "0");
}

function handleSongEvent(event) {
  const [$title] = utils.$(".title");
  const [$artist] = utils.$(".artist");
  const [$cover] = utils.$(".cover");
  $title.textContent = event["title"];
  $artist.textContent = event["artist"];

  const height = utils.get(":root", "--artist-size", false);

  animate([$title, $artist], {
    x: [0, 5 * height, -2 * height, 0],
    color: ["#fff", "#fcf", "#fff"],
    delay: stagger(50),
    duration: 500,
    ease: eases.outBack(1),
  });
  // $cover.src = event["cover"];
}

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
          animate(".combo", {
            x: [0, -5, 5, 0],
            filter: [
              "hue-rotate(-60deg) saturate(2)",
              "hue-rotate(-60deg) saturate(2)",
              "hue-rotate(0deg) saturate(1)",
            ],
            ease: eases.outBack(1),
            duration: 250,
          });
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

const ws = connect();
const scoreCounter = new Counter(5);
const log = new AccuracyLog(4);

animate("body > *", {
  y: ["1rem", "0"],
  opacity: [0, 1],
  delay: stagger(100),
  duration: 1000,
  ease: eases.outBack(2),
  onComplete: (self) => utils.cleanInlineStyles(self),
});
