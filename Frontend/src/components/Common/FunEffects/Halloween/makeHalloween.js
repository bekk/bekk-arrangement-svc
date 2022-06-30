import p5 from 'p5';

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
  const STREAMS_PER_LAYER = 3;
  const MAX_SIZE = 20;
  const GRAVITY = 0.04;
  const LAYER_COUNT = 5;

  const WIND_SPEED = 0.02;
  const WIND_CHANGE = 0.0001;

  let STREAMS = [];
  let pumpkins = 0;
  let pumpkinCoordinates = [];

  let windowWidth = window.innerWidth;
  let windowHeight = heighestHeight();
  let frameCount = 0;

  window.addEventListener('click', (e) => {
    pumpkinCoordinates[pumpkins] = { x: e.pageX - 90, y: e.pageY + 20 };
    pumpkins++;
  });

  // Will run once when the sketch is opened
  p.setup = function setup() {
    p.createCanvas(windowWidth, windowHeight);
    p.noStroke();

    // Initialize the stream with random positions
    STREAMS = [];
    for (let l = 0; l < LAYER_COUNT; l++) {
      STREAMS.push([]);
      for (let i = 0; i < STREAMS_PER_LAYER; i++) {
        STREAMS[l].push({
          x: p.random(windowWidth),
          y: -50, // from the top with delay
          mass: p.random(0.75, 1.25),
          l: l + 1,
          color: 'red',
        });
      }
    }
    pumpkinCoordinates = [...Array(100)].map((_) => ({
      x: Math.random() * windowWidth,
      y: Math.random() * windowHeight,
    }));
  };

  function makePumpkin(x, y) {
    //Jack-O-Lantern
    //stem
    p.fill(100, 90, 0);
    p.rect(x + 95, y - 20, 10, 20);
    //pumpkin body
    p.fill(244, 167, 66);
    p.ellipse(x + 110, y + 20, 70, 60);
    p.ellipse(x + 90, y + 20, 70, 60);
    //smile
    p.fill(0);
    p.ellipse(x + 100, y + 30, 70, 20);
    p.fill(244, 167, 66);
    p.ellipse(x + 100, y + 20, 70, 20);
    //fill(100, 0, 0);
    //fangs
    p.triangle(x + 120, y + 35, x + 130, y + 20, x + 110, y + 20);
    p.triangle(x + 80, y + 35, x + 70, y + 20, x + 90, y + 20);
    //eyes
    p.fill(0);
    p.triangle(x + 115, y + 5, x + 125, y + 20, x + 105, y + 20);
    p.triangle(x + 85, y + 5, x + 75, y + 20, x + 95, y + 20);
  }

  // Helper function to prepare a given stream for the next frame
  function updateStream(stream) {
    const diameter = (stream.l * MAX_SIZE) / LAYER_COUNT;
    if (stream.y > p.height + diameter) stream.y = -diameter;
    else stream.y += GRAVITY * stream.l * stream.mass;

    // Get the wind speed at the given layer and area of the page
    const wind =
      p.noise(stream.l, stream.y * WIND_CHANGE, frameCount * WIND_CHANGE) - 0.5;
    if (stream.x > p.width + diameter) stream.x = -diameter;
    else if (stream.x < -diameter) stream.x = p.width + diameter;
    else stream.x += wind * WIND_SPEED * stream.l;
  }

  // Will run every frame (refreshes many times per second)
  p.draw = function draw() {
    if (windowWidth > 1000) {
      makePumpkin(0, 250);
      makePumpkin(windowWidth - 200, 900);
      makePumpkin(windowWidth - 200, 500);
      makePumpkin(windowWidth - 300, 700);
      pumpkinCoordinates.forEach((pk, i) => {
        if (i < pumpkins) {
          makePumpkin(pk.x, pk.y);
        }
      });
    }

    // Iterate through each stream to draw and update them
    for (let l = 0; l < STREAMS.length; l++) {
      const LAYER = STREAMS[l];

      for (let i = 0; i < LAYER.length; i++) {
        const stream = LAYER[i];
        p.noStroke();
        p.fill(255, 0, 0);

        p.circle(stream.x, stream.y, (stream.l * MAX_SIZE) / LAYER_COUNT);
        updateStream(stream);
        frameCount++;
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
