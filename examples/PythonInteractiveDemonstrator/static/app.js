"use strict";

/* ==========================================================================
   Canvas & Coordinate Setup
   ========================================================================== */
const canvas       = document.getElementById("c");
const ctx          = canvas.getContext("2d");
const overlay      = document.getElementById("overlay");
const topSolPanel  = document.getElementById("top-solutions-panel");

const XMIN = -10, XMAX = 10, YMIN = -10, YMAX = 10;

function resize() {
  canvas.width  = window.innerWidth;
  canvas.height = window.innerHeight;
  redraw();
}

window.addEventListener("resize", resize);

function mathToCanvas(mx, my) {
  const cx = (mx - XMIN) / (XMAX - XMIN) * canvas.width;
  const cy = (1 - (my - YMIN) / (YMAX - YMIN)) * canvas.height;
  return [cx, cy];
}

function canvasToMath(cx, cy) {
  const mx = XMIN + cx / canvas.width  * (XMAX - XMIN);
  const my = YMIN + (1 - cy / canvas.height) * (YMAX - YMIN);
  return [mx, my];
}

/* ==========================================================================
   Application State
   ========================================================================== */
let drawnPoints      = [];
let drawnCanvasPath  = [];
let fittedCurve      = null;
let expression       = null;
let computeStartTime = null;
let isDrawing        = false;

// Generation progress
let populationCurves = [];
let topSolData       = [];
let selectedTopIdx   = -1;

const runtimeDisplay = document.getElementById("runtime-display");
const genDisplay     = document.getElementById("gen-display");

/** Computation state machine: "idle" | "running" | "paused" */
let computeState = "idle";

/* ==========================================================================
   Settings Panel
   ========================================================================== */
const burgerBtn        = document.getElementById("burger-btn");
const settingsPanel    = document.getElementById("settings-panel");
const settingsBackdrop = document.getElementById("settings-backdrop");
const settingsClose    = settingsPanel.querySelector(".settings-close");

function openSettings() {
  settingsPanel.classList.add("open");
  settingsBackdrop.classList.add("open");
}

function closeSettings() {
  settingsPanel.classList.remove("open");
  settingsBackdrop.classList.remove("open");
}

burgerBtn.addEventListener("click", () => {
  settingsPanel.classList.contains("open") ? closeSettings() : openSettings();
});
settingsBackdrop.addEventListener("click", closeSettings);
settingsClose.addEventListener("click", closeSettings);

/* ==========================================================================
   Symbol Chips
   ========================================================================== */
const SYMBOL_GROUPS = [
  { label: "Arithmetic",        symbols: ["add", "sub", "mul", "div"] },
  { label: "Powers / Roots",    symbols: ["pow", "square", "sqrt", "cube", "cbrt", "abs"] },
  { label: "Trigonometric",     symbols: ["sin", "cos", "tan"] },
  { label: "Other",             symbols: ["tanh", "exp", "log"] },
];

const LOCKED_SYMBOLS = new Set(["constant", "variable"]);
const DEFAULT_ACTIVE = new Set(["add", "sub", "mul", "div", "sqrt", "log", "constant", "variable"]);
let activeSymbols    = new Set(DEFAULT_ACTIVE);

function buildSymbolChips() {
  const container = document.getElementById("symbol-chips-container");
  container.innerHTML = "";

  // Locked (required) chips
  const lockedGroup = document.createElement("div");
  lockedGroup.className = "symbol-group";
  lockedGroup.innerHTML = '<div class="symbol-group-label">Required</div>';

  const lockedChips = document.createElement("div");
  lockedChips.className = "symbol-chips";
  for (const s of LOCKED_SYMBOLS) {
    const chip = document.createElement("span");
    chip.className = "symbol-chip active locked";
    chip.textContent = s;
    lockedChips.appendChild(chip);
  }
  lockedGroup.appendChild(lockedChips);
  container.appendChild(lockedGroup);

  // Togglable groups
  for (const g of SYMBOL_GROUPS) {
    const group = document.createElement("div");
    group.className = "symbol-group";
    group.innerHTML = '<div class="symbol-group-label">' + g.label + '</div>';

    const chips = document.createElement("div");
    chips.className = "symbol-chips";

    for (const s of g.symbols) {
      const chip = document.createElement("span");
      chip.className = "symbol-chip" + (activeSymbols.has(s) ? " active" : "");
      chip.textContent = s;
      chip.addEventListener("click", () => {
        if (activeSymbols.has(s)) {
          activeSymbols.delete(s);
          chip.classList.remove("active");
        } else {
          activeSymbols.add(s);
          chip.classList.add("active");
        }
      });
      chips.appendChild(chip);
    }

    group.appendChild(chips);
    container.appendChild(group);
  }
}

buildSymbolChips();

/* ==========================================================================
   Slider value display
   ========================================================================== */
function initSlider(id, suffix) {
  const slider = document.getElementById(id);
  const valSpan = document.getElementById(id + "-val");
  if (!slider || !valSpan) return;
  slider.addEventListener("input", () => {
    valSpan.textContent = slider.value + (suffix || "");
  });
}

initSlider("p-step_delay_ms", " ms");
initSlider("p-max_displayed_curves", "");

/* ==========================================================================
   Gather Parameters from Settings Panel
   ========================================================================== */
function gatherParams() {
  const allSymbols = new Set([...LOCKED_SYMBOLS, ...activeSymbols]);

  const p = {
    allowed_symbols:                [...allSymbols].join(","),
    generations:                    parseInt(document.getElementById("p-generations").value) || 30,
    population_size:                parseInt(document.getElementById("p-population_size").value) || 200,
    tree_length:                    parseInt(document.getElementById("p-tree_length").value) || 40,
    tree_depth:                     parseInt(document.getElementById("p-tree_depth").value) || 20,
    mutation_rate:                  parseFloat(document.getElementById("p-mutation_rate").value) || 0.1,
    tournament_size:                parseInt(document.getElementById("p-tournament_size").value) || 4,
    elites:                         parseInt(document.getElementById("p-elites").value) || 1,
    parameter_optimization_iterations: parseInt(document.getElementById("p-parameter_optimization_iterations").value) || 5,
    use_linear_scaling:             document.getElementById("p-use_linear_scaling").checked,
    seed:                           parseInt(document.getElementById("p-seed").value),
    step_delay_ms:                  parseInt(document.getElementById("p-step_delay_ms").value) || 0,
    max_displayed_curves:           parseInt(document.getElementById("p-max_displayed_curves").value) || 50,
  };

  if (isNaN(p.seed)) p.seed = -1;

  return p;
}

/* ==========================================================================
   Control Buttons (Pause / Resume / Stop)
   ========================================================================== */
const controlsDiv = document.getElementById("controls");
const btnPause    = document.getElementById("btn-pause");
const btnResume   = document.getElementById("btn-resume");
const btnStop     = document.getElementById("btn-stop");

function updateControlButtons() {
  if (computeState === "idle") {
    controlsDiv.classList.add("hidden");
  } else if (computeState === "running") {
    controlsDiv.classList.remove("hidden");
    btnPause.style.display  = "flex";
    btnResume.style.display = "none";
    btnStop.style.display   = "flex";
  } else if (computeState === "paused") {
    controlsDiv.classList.remove("hidden");
    btnPause.style.display  = "none";
    btnResume.style.display = "flex";
    btnStop.style.display   = "flex";
  }
}

btnPause.addEventListener("click", () => {
  if (computeState !== "running" || !ws || ws.readyState !== WebSocket.OPEN) return;
  ws.send(JSON.stringify({ type: "pause" }));
});

btnResume.addEventListener("click", () => {
  if (computeState !== "paused" || !ws || ws.readyState !== WebSocket.OPEN) return;
  ws.send(JSON.stringify({ type: "resume", params: gatherParams() }));
});

btnStop.addEventListener("click", () => {
  if (computeState === "idle" || !ws || ws.readyState !== WebSocket.OPEN) return;
  ws.send(JSON.stringify({ type: "stop" }));

  drawnPoints       = [];
  drawnCanvasPath   = [];
  fittedCurve       = null;
  expression        = null;
  populationCurves  = [];
  topSolData        = [];
  selectedTopIdx    = -1;
  topSolPanel.style.display  = "none";
  genDisplay.style.display   = "none";
  computeState      = "idle";

  updateControlButtons();
  overlay.innerHTML = '<span class="hint">Draw a curve to fit</span>';
  redraw();
});

/* ==========================================================================
   Grid Drawing
   ========================================================================== */
function drawGrid() {
  const w = canvas.width, h = canvas.height;

  ctx.fillStyle = "#1e1e2e";
  ctx.fillRect(0, 0, w, h);

  // Minor gridlines
  ctx.strokeStyle = "#313244";
  ctx.lineWidth   = 0.5;
  for (let x = Math.ceil(XMIN); x <= XMAX; x++) {
    const [cx] = mathToCanvas(x, 0);
    ctx.beginPath(); ctx.moveTo(cx, 0); ctx.lineTo(cx, h); ctx.stroke();
  }
  for (let y = Math.ceil(YMIN); y <= YMAX; y++) {
    const [, cy] = mathToCanvas(0, y);
    ctx.beginPath(); ctx.moveTo(0, cy); ctx.lineTo(w, cy); ctx.stroke();
  }

  // Major gridlines (every 5 units)
  ctx.strokeStyle = "#45475a";
  ctx.lineWidth   = 1;
  for (let x = Math.ceil(XMIN / 5) * 5; x <= XMAX; x += 5) {
    const [cx] = mathToCanvas(x, 0);
    ctx.beginPath(); ctx.moveTo(cx, 0); ctx.lineTo(cx, h); ctx.stroke();
  }
  for (let y = Math.ceil(YMIN / 5) * 5; y <= YMAX; y += 5) {
    const [, cy] = mathToCanvas(0, y);
    ctx.beginPath(); ctx.moveTo(0, cy); ctx.lineTo(w, cy); ctx.stroke();
  }

  // Axes
  ctx.strokeStyle = "#585b70";
  ctx.lineWidth   = 1.5;
  const [ax]  = mathToCanvas(0, 0);
  const [, ay] = mathToCanvas(0, 0);
  ctx.beginPath(); ctx.moveTo(ax, 0); ctx.lineTo(ax, h); ctx.stroke();
  ctx.beginPath(); ctx.moveTo(0, ay); ctx.lineTo(w, ay); ctx.stroke();

  // Axis labels
  ctx.fillStyle    = "#7f849c";
  ctx.font         = "11px system-ui, sans-serif";
  ctx.textAlign    = "center";
  ctx.textBaseline = "top";
  for (let x = Math.ceil(XMIN); x <= XMAX; x++) {
    if (x === 0) continue;
    const [cx] = mathToCanvas(x, 0);
    ctx.fillText(x, cx, ay + 4);
  }
  ctx.textAlign    = "right";
  ctx.textBaseline = "middle";
  for (let y = Math.ceil(YMIN); y <= YMAX; y++) {
    if (y === 0) continue;
    const [, cy] = mathToCanvas(0, y);
    ctx.fillText(y, ax - 6, cy);
  }
}

/* ==========================================================================
   Curve Drawing (user stroke, population, & fitted result)
   ========================================================================== */
function drawUserCurve() {
  if (drawnCanvasPath.length < 2) return;

  ctx.strokeStyle = "#cdd6f4";
  ctx.lineWidth   = 2.5;
  ctx.lineJoin    = "round";
  ctx.lineCap     = "round";

  ctx.beginPath();
  ctx.moveTo(drawnCanvasPath[0][0], drawnCanvasPath[0][1]);
  for (let i = 1; i < drawnCanvasPath.length; i++) {
    ctx.lineTo(drawnCanvasPath[i][0], drawnCanvasPath[i][1]);
  }
  ctx.stroke();
}

function drawCurve(curve, color, width) {
  if (!curve || curve.length < 2) return;

  ctx.strokeStyle = color;
  ctx.lineWidth   = width;
  ctx.lineJoin    = "round";
  ctx.setLineDash([]);

  ctx.beginPath();
  let started = false;
  for (const [mx, my] of curve) {
    if (my < YMIN - 5 || my > YMAX + 5 || !isFinite(my)) { started = false; continue; }
    const [cx, cy] = mathToCanvas(mx, my);
    if (!started) { ctx.moveTo(cx, cy); started = true; }
    else          { ctx.lineTo(cx, cy); }
  }
  ctx.stroke();
}

function drawPopulationCurves() {
  if (!populationCurves || populationCurves.length === 0) return;
  for (const curve of populationCurves) {
    drawCurve(curve, "rgba(205, 214, 244, 0.08)", 3);
  }
}

function drawFittedCurve() {
  drawCurve(fittedCurve, "#f38ba8", 3);
}

function redraw() {
  drawGrid();
  drawUserCurve();
  drawPopulationCurves();
  drawFittedCurve();
}

/* ==========================================================================
   Pointer Events (drawing)
   ========================================================================== */
canvas.addEventListener("pointerdown", (e) => {
  isDrawing = true;
  canvas.setPointerCapture(e.pointerId);

  // Cancel any in-progress computation
  if (computeState !== "idle" && ws && ws.readyState === WebSocket.OPEN) {
    ws.send(JSON.stringify({ type: "cancel" }));
  }

  // Clear previous drawing & result
  drawnPoints       = [];
  drawnCanvasPath   = [];
  fittedCurve       = null;
  expression        = null;
  populationCurves  = [];
  topSolData        = [];
  selectedTopIdx    = -1;
  computeState      = "idle";
  updateControlButtons();
  topSolPanel.style.display    = "none";
  runtimeDisplay.style.display = "none";
  genDisplay.style.display     = "none";

  const [mx, my] = canvasToMath(e.offsetX, e.offsetY);
  drawnPoints.push([mx, my]);
  drawnCanvasPath.push([e.offsetX, e.offsetY]);

  redraw();
  overlay.innerHTML = '<span class="hint">Drawing...</span>';
});

canvas.addEventListener("pointermove", (e) => {
  if (!isDrawing) return;

  const [mx, my] = canvasToMath(e.offsetX, e.offsetY);
  drawnPoints.push([mx, my]);
  drawnCanvasPath.push([e.offsetX, e.offsetY]);

  // Draw the latest segment incrementally
  const len = drawnCanvasPath.length;
  if (len >= 2) {
    ctx.strokeStyle = "#cdd6f4";
    ctx.lineWidth   = 2.5;
    ctx.lineJoin    = "round";
    ctx.lineCap     = "round";
    ctx.beginPath();
    ctx.moveTo(drawnCanvasPath[len - 2][0], drawnCanvasPath[len - 2][1]);
    ctx.lineTo(drawnCanvasPath[len - 1][0], drawnCanvasPath[len - 1][1]);
    ctx.stroke();
  }
});

canvas.addEventListener("pointerup", (e) => {
  if (!isDrawing) return;
  isDrawing = false;

  if (drawnPoints.length < 5) {
    overlay.innerHTML = '<span class="hint">Draw a longer curve</span>';
    return;
  }

  if (ws && ws.readyState === WebSocket.OPEN) {
    ws.send(JSON.stringify({ type: "fit", points: drawnPoints, params: gatherParams() }));
    computeState     = "running";
    computeStartTime = performance.now();
    runtimeDisplay.style.display = "none";
    updateControlButtons();
    overlay.innerHTML = '<span class="status">Initializing HeuristicLib GP\u2026</span>';
  } else {
    overlay.innerHTML = '<span class="status" style="color:#f38ba8">Disconnected \u2013 reload page</span>';
  }
});

canvas.addEventListener("contextmenu", (e) => e.preventDefault());

/* ==========================================================================
   LaTeX Rendering & Top Solutions Table
   ========================================================================== */
function renderExpressionOverlay(latex, prefix) {
  const prefixHtml = prefix ? '<span class="gen-info">' + escapeHtml(prefix) + '</span><br>' : '';
  overlay.innerHTML = prefixHtml + '<span class="expr" id="katex-target"></span>';
  const el = document.getElementById("katex-target");

  function tryRender() {
    if (typeof katex !== "undefined") {
      try {
        katex.render(latex, el, { throwOnError: false, displayMode: false });
      } catch (e) {
        el.textContent = latex;
      }
    } else {
      setTimeout(tryRender, 100);
    }
  }
  tryRender();
}

function buildTopSolutionsTable() {
  let html = '<table><tr><th>R\u00b2</th><th>Len</th><th>Depth</th><th>Expression</th></tr>';
  for (let i = 0; i < topSolData.length; i++) {
    const s    = topSolData[i];
    const sel  = i === selectedTopIdx ? " selected" : "";
    const best = i === 0 ? " best-row" : "";
    html += '<tr class="top-row' + sel + best + '" data-idx="' + i + '">';
    html += '<td>' + s.r2 + '</td><td>' + s.length + '</td><td>' + s.depth + '</td>';
    html += '<td id="top-expr-' + i + '"></td></tr>';
  }
  html += '</table>';
  topSolPanel.innerHTML = html;

  // Render KaTeX in each row
  for (let i = 0; i < topSolData.length; i++) {
    const el = document.getElementById("top-expr-" + i);
    if (el && typeof katex !== "undefined") {
      try {
        katex.render(topSolData[i].latex || topSolData[i].expr, el, { throwOnError: false, displayMode: false });
      } catch (e) {
        el.textContent = topSolData[i].expr;
      }
    } else if (el) {
      el.textContent = topSolData[i].expr;
    }
  }

  // Row click handlers
  topSolPanel.querySelectorAll(".top-row").forEach((row) => {
    row.addEventListener("click", () => {
      selectTopSolution(parseInt(row.dataset.idx));
    });
  });
}

function selectTopSolution(idx) {
  if (idx < 0 || idx >= topSolData.length) return;

  selectedTopIdx = idx;
  const s = topSolData[idx];

  if (s.curve && s.curve.length > 0) fittedCurve = s.curve;
  renderExpressionOverlay(s.latex || s.expr, null);
  redraw();

  topSolPanel.querySelectorAll(".top-row").forEach((row, i) => {
    row.classList.toggle("selected", i === idx);
  });
}

/* ==========================================================================
   WebSocket Connection
   ========================================================================== */
let ws;

function connectWS() {
  const proto = location.protocol === "https:" ? "wss:" : "ws:";
  ws = new WebSocket(proto + "//" + location.host + "/ws");

  ws.onopen = () => {
    overlay.innerHTML = '<span class="hint">Draw a curve to fit</span>';
  };

  ws.onmessage = (evt) => {
    const msg = JSON.parse(evt.data);

    switch (msg.type) {
      case "generation": {
        // Live generation update
        const gen = msg.gen;
        const total = msg.totalGens;
        const r2 = msg.bestR2;

        populationCurves = msg.populationCurves || [];
        fittedCurve      = msg.bestCurve || null;

        // Update generation counter display
        genDisplay.textContent   = "Gen " + gen + "/" + total;
        genDisplay.style.display = "block";

        // Update overlay with best expression
        const prefix = "Gen " + gen + "/" + total + " \u2014 R\u00b2: " + r2.toFixed(4);
        renderExpressionOverlay(msg.bestLatex || msg.bestExpr, prefix);

        redraw();
        break;
      }

      case "result":
        computeState = "idle";
        updateControlButtons();
        populationCurves = [];

        if (computeStartTime !== null) {
          const elapsed = ((performance.now() - computeStartTime) / 1000).toFixed(3);
          runtimeDisplay.textContent   = elapsed + " s";
          runtimeDisplay.style.display = "block";
          computeStartTime = null;
        }

        expression        = msg.expression;
        fittedCurve       = msg.curve;
        topSolData        = msg.topSolutions || [];
        selectedTopIdx    = msg.bestIdx != null ? msg.bestIdx : 0;

        genDisplay.style.display = "none";
        renderExpressionOverlay(msg.latex || msg.expression, null);
        redraw();

        if (topSolData.length > 0) {
          buildTopSolutionsTable();
          topSolPanel.style.display = "block";
        }
        break;

      case "error":
        computeState = "idle";
        updateControlButtons();
        populationCurves = [];
        genDisplay.style.display = "none";
        overlay.innerHTML = '<span class="status" style="color:#f38ba8">' + escapeHtml(msg.message) + '</span>';
        break;

      case "paused":
        computeState = "paused";
        updateControlButtons();
        overlay.innerHTML = '<span class="status" style="color:#f9e2af">Paused \u2013 change settings or resume</span>';
        break;

      case "resumed":
        computeState = "running";
        updateControlButtons();
        overlay.innerHTML = '<span class="status">Computing symbolic regression\u2026</span>';
        break;

      case "stopped":
        computeState      = "idle";
        updateControlButtons();
        drawnPoints       = [];
        drawnCanvasPath   = [];
        fittedCurve       = null;
        expression        = null;
        populationCurves  = [];
        topSolData        = [];
        selectedTopIdx    = -1;
        topSolPanel.style.display  = "none";
        genDisplay.style.display   = "none";
        overlay.innerHTML = '<span class="hint">Draw a curve to fit</span>';
        redraw();
        break;
    }
  };

  ws.onclose = () => {
    computeState = "idle";
    updateControlButtons();
    overlay.innerHTML = '<span class="status" style="color:#f38ba8">Disconnected \u2013 reconnecting\u2026</span>';
    setTimeout(connectWS, 2000);
  };
}

/* ==========================================================================
   Utilities
   ========================================================================== */
function escapeHtml(s) {
  const d = document.createElement("div");
  d.textContent = s;
  return d.innerHTML;
}

/* ==========================================================================
   Initialise
   ========================================================================== */
connectWS();
resize();
