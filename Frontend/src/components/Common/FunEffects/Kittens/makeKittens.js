import p5 from 'p5';

function getRandomInt(max) {
  return Math.floor(Math.random() * max);
}

function heighestHeight() {
  var body = document.body,
    html = document.documentElement;

  return Math.max(
    body.scrollHeight,
    body.offsetHeight,
    html.clientHeight,
    html.scrollHeight,
    html.offsetHeight
  );
}

let sketch = function (p) {
  // Tweakable parameters
  const STREAMS_PER_LAYER = 1;
  const STREAM_SIZE = 50;
  const GRAVITY = 0.05;
  const COLORS = [
    '#FF3636',
    '#FFA13C',
    '#FFFD0B',
    '#62FF4E',
    '#58D5F6',
    '#CB6FFF',
    '#F45FB1',
  ];
  const LAYER_COUNT = COLORS.length;

  let STREAMS = [];
  let cats = 0;
  let coordinates = [];

  let windowWidth = window.innerWidth;
  let windowHeight = heighestHeight();
  let isDrawing = false;
  let offset = 0;

  window.addEventListener('click', (e) => {
    coordinates[cats] = { x: e.pageX, y: e.pageY };
    cats++;
  });

  window.addEventListener('mousedown', (e) => {
    isDrawing = true;
  });

  window.addEventListener('mouseup', (e) => {
    isDrawing = false;
  });

  window.addEventListener('mousemove', (e) => {
    if (isDrawing) {
      coordinates[cats] = { x: e.pageX, y: e.pageY };
      cats++;
    }
  });

  // Will run once when the sketch is opened
  p.setup = function setup() {
    p.createCanvas(windowWidth, windowHeight);
    p.noStroke();

    // Initialize the stream with random positions
    STREAMS = [];
    offset = windowWidth - 350;
    for (let l = 0; l < LAYER_COUNT; l++) {
      STREAMS.push([]);
      for (let i = 0; i < STREAMS_PER_LAYER; i++) {
        STREAMS[l].push({
          x: offset,
          y: 0, // from the top
          mass: p.random(0.75, 1.25),
          l: l + 1,
          color: 'red',
        });
        offset += STREAM_SIZE;
      }
    }
    coordinates = [...Array(100)].map((_) => ({
      x: Math.random() * windowWidth,
      y: Math.random() * windowHeight,
    }));
  };

  function catHead(x, y) {
    p.noStroke();
    //p.fill(p.random(255), p.random(255), p.random(255));
    p.fill(COLORS[getRandomInt(LAYER_COUNT - 1)]);
    p.ellipse(x, y, 20, 20);
    p.triangle(x - 10, y - 15, x - 10, y, x - 2, y - 10);
    p.triangle(x + 10, y - 15, x + 10, y, x + 2, y - 10);
    p.fill(0);
    p.ellipse(x - 5, y - 2, 5, 5);
    p.ellipse(x + 5, y - 2, 5, 5);
  }

  // Helper function to prepare a given stream for the next frame
  function updateStream(stream) {
    const diameter = STREAM_SIZE;
    if (stream.y > p.height + diameter) stream.y = -diameter;
    else stream.y += GRAVITY * stream.l * stream.mass;
  }

  // Will run every frame (refreshes many times per second)
  p.draw = function draw() {
    if (windowWidth > 1000) {
      catHead(106, 259);
      catHead(119, 257);

      coordinates.forEach((cat, i) => {
        if (i < cats) {
          catHead(cat.x, cat.y);
        }
      });
    }

    // Iterate through each stream to draw and update them
    for (let l = 0; l < STREAMS.length; l++) {
      const LAYER = STREAMS[l];
      const color = COLORS[l];

      for (let i = 0; i < LAYER.length; i++) {
        const stream = LAYER[i];
        p.noStroke();
        p.fill(color);

        p.circle(stream.x, stream.y, STREAM_SIZE);
        updateStream(stream);
      }
    }
  };

  p.windowResized = function windowResized() {
    windowWidth = window.innerWidth;
    windowHeight = heighestHeight();
    p.resizeCanvas(windowWidth, windowHeight);
    p.setup();
  };

  new ResizeObserver(() => p.windowResized()).observe(document.body);
};

export const myp5 = (ref) => new p5(sketch, ref);
