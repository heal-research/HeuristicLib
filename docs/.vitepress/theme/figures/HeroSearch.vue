<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, shallowRef } from "vue";

// The home hero: a genetic algorithm population on the two-dimensional Rastrigin function. Three generations stand
// side by side. Without reduced motion, a larger view also plays every generation and can be paused. The landscape
// is lighter where the objective value is lower. The data loads with the home page only.
interface HeroData {
    bound: number;
    generations: number[][];
    best: number[];
}

const snapshots = [1, 5, 15];
const data = shallowRef<HeroData>();
const reducedMotion = ref(true);
const playing = ref(true);
const generation = ref(1);
const stage = ref<HTMLCanvasElement>();
const panels = ref<HTMLCanvasElement[]>([]);

const formatBest = (value: number) => (value < 0.01 ? value.toExponential(1) : value.toFixed(2));
const bestOf = (index: number) => (data.value ? formatBest(data.value.best[index - 1]) : "");
const count = computed(() => data.value?.generations.length ?? 1);
const progress = computed(() => ((generation.value - 1) / Math.max(1, count.value - 1)) * 100);

const style = () => getComputedStyle(document.documentElement);

function rgb(name: string): [number, number, number] {
    const value = style().getPropertyValue(name).trim().replace("#", "");
    return [0, 2, 4].map((i) => parseInt(value.slice(i, i + 2), 16)) as [number, number, number];
}

// The landscape shade: mostly the bowl that leads to the optimum, with the local minima as a fainter ripple.
function shade(x: number, y: number, bound: number): number {
    const bowl = Math.sqrt((x * x + y * y) / (2 * bound * bound));
    const ripple = (2 - Math.cos(2 * Math.PI * x) - Math.cos(2 * Math.PI * y)) / 4;
    return Math.min(1, 0.85 * bowl + 0.15 * ripple);
}

// One landscape per pixel size and theme, drawn at the device resolution so nothing is scaled.
const landscapes = new Map<string, HTMLCanvasElement>();

function landscape(size: number, bound: number): HTMLCanvasElement {
    const low = rgb("--hl-fig-heat-lo");
    const high = rgb("--hl-fig-heat-hi");
    const key = `${size}-${low}-${high}`;
    const cached = landscapes.get(key);
    if (cached) return cached;
    const pixels = new ImageData(size, size);
    for (let j = 0; j < size; j++) {
        for (let i = 0; i < size; i++) {
            const t = shade(-bound + ((i + 0.5) / size) * 2 * bound, bound - ((j + 0.5) / size) * 2 * bound, bound);
            const k = (j * size + i) * 4;
            for (let c = 0; c < 3; c++) pixels.data[k + c] = low[c] + (high[c] - low[c]) * t;
            pixels.data[k + 3] = 255;
        }
    }
    const target = document.createElement("canvas");
    target.width = size;
    target.height = size;
    target.getContext("2d")?.putImageData(pixels, 0, 0);
    landscapes.set(key, target);
    return target;
}

function draw(target: HTMLCanvasElement, index: number, dotRadius: number) {
    const hero = data.value;
    const context = target.getContext("2d");
    if (!hero || !context) return;
    const ratio = window.devicePixelRatio || 1;
    const size = Math.round(target.clientWidth * ratio);
    if (!size) return;
    target.width = size;
    target.height = size;
    context.drawImage(landscape(size, hero.bound), 0, 0);

    // The global optimum as a faint cross.
    const center = size / 2;
    const arm = 5 * ratio;
    context.strokeStyle = style().getPropertyValue("--vp-c-text-3").trim();
    context.lineWidth = ratio;
    context.beginPath();
    context.moveTo(center - arm, center);
    context.lineTo(center + arm, center);
    context.moveTo(center, center - arm);
    context.lineTo(center, center + arm);
    context.stroke();

    const toPixel = (value: number, flip: boolean) =>
        ((flip ? hero.bound - value : value + hero.bound) / (2 * hero.bound)) * size;
    context.fillStyle = style().getPropertyValue("--hl-fig-accent").trim();
    context.strokeStyle = style().getPropertyValue("--hl-fig-dot-ring").trim();
    context.lineWidth = 1.25 * ratio;
    const points = hero.generations[index - 1];
    for (let p = 0; p < points.length; p += 2) {
        context.beginPath();
        context.arc(toPixel(points[p], false), toPixel(points[p + 1], true), dotRadius * ratio, 0, 2 * Math.PI);
        context.fill();
        context.stroke();
    }
}

function render() {
    panels.value.forEach((panel, i) => draw(panel, snapshots[i], 2.6));
    if (stage.value) draw(stage.value, generation.value, 3.6);
}

// Advance one generation every 450 ms and hold the last one for three seconds before starting over.
let frame = 0;
let last = 0;
function tick(time: number) {
    const hold = generation.value === count.value ? 3000 : 450;
    if (playing.value && time - last >= hold) {
        last = time;
        generation.value = generation.value === count.value ? 1 : generation.value + 1;
        if (stage.value) draw(stage.value, generation.value, 3.6);
    }
    frame = requestAnimationFrame(tick);
}

function show(index: number) {
    generation.value = index;
    playing.value = false;
    if (stage.value) draw(stage.value, index, 3.6);
}

let motionQuery: MediaQueryList | undefined;
let themeObserver: MutationObserver | undefined;
let sizeObserver: ResizeObserver | undefined;
let unmounted = false;

async function applyMotionPreference() {
    reducedMotion.value = motionQuery?.matches ?? true;
    cancelAnimationFrame(frame);
    await nextTick();
    sizeObserver?.disconnect();
    for (const canvas of [stage.value, ...panels.value]) if (canvas) sizeObserver?.observe(canvas);
    render();
    if (!reducedMotion.value) frame = requestAnimationFrame(tick);
}

onMounted(async () => {
    const loaded = (await import("../../figure-data/home-hero.json")).default as HeroData;
    if (unmounted) return;
    data.value = loaded;
    sizeObserver = new ResizeObserver(render);
    themeObserver = new MutationObserver(render);
    themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ["class"] });
    motionQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
    motionQuery.addEventListener("change", applyMotionPreference);
    await applyMotionPreference();
});

onBeforeUnmount(() => {
    unmounted = true;
    cancelAnimationFrame(frame);
    themeObserver?.disconnect();
    sizeObserver?.disconnect();
    motionQuery?.removeEventListener("change", applyMotionPreference);
});
</script>

<template>
    <figure class="hl-hero" :class="{ 'hl-hero--animated': !reducedMotion }">
        <div v-if="!reducedMotion" class="hl-hero__stage">
            <canvas
                ref="stage"
                role="img"
                aria-label="A population of 80 candidates moves over the Rastrigin landscape and gathers at the global optimum in the center."
            />
            <div class="hl-hero__bar">
                <button
                    class="hl-hero__toggle"
                    type="button"
                    :aria-label="playing ? 'Pause the animation' : 'Play the animation'"
                    @click="playing = !playing"
                >
                    <svg v-if="playing" viewBox="0 0 16 16" aria-hidden="true">
                        <rect x="4" y="3" width="3" height="10" rx="1" />
                        <rect x="9" y="3" width="3" height="10" rx="1" />
                    </svg>
                    <svg v-else viewBox="0 0 16 16" aria-hidden="true"><path d="M5 3 L13 8 L5 13 Z" /></svg>
                </button>
                <div class="hl-hero__track" aria-hidden="true"><i :style="{ width: `${progress}%` }" /></div>
                <span class="hl-hero__status">
                    generation <b>{{ generation }}</b> · best <b>{{ bestOf(generation) }}</b>
                </span>
            </div>
        </div>
        <div class="hl-hero__panels">
            <button
                v-for="snapshot in snapshots"
                :key="snapshot"
                class="hl-hero__panel"
                type="button"
                :disabled="reducedMotion"
                :aria-label="`Show generation ${snapshot}`"
                @click="show(snapshot)"
            >
                <canvas ref="panels" role="img" :aria-label="`Population at generation ${snapshot}`" />
                <span class="hl-hero__label">generation {{ snapshot }}</span>
                <span class="hl-hero__best">best {{ bestOf(snapshot) }}</span>
            </button>
        </div>
        <figcaption>A genetic algorithm searching the Rastrigin function</figcaption>
    </figure>
</template>

<style scoped>
.hl-hero {
    position: relative;
    z-index: 1;
    width: min(100%, 420px);
    margin: 0 auto;
    padding: 14px;
    border: 1px solid var(--vp-c-divider);
    border-radius: 18px;
    background: var(--vp-c-bg-soft);
    box-shadow: var(--hl-fig-shadow);
}

.hl-hero canvas {
    display: block;
    width: 100%;
    aspect-ratio: 1;
    border-radius: 10px;
}

.hl-hero--animated {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 92px;
    gap: 12px;
    width: min(100%, 470px);
}

.hl-hero--animated .hl-hero__panels {
    grid-template-columns: 1fr;
    align-content: start;
}

.hl-hero--animated figcaption {
    grid-column: 1 / -1;
    margin-top: 0;
}

.hl-hero--animated .hl-hero__label {
    font-size: 12px;
}

.hl-hero--animated .hl-hero__best {
    font-size: 11.5px;
}

.hl-hero__bar {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-top: 10px;
    font-size: 12px;
    color: var(--vp-c-text-2);
    font-variant-numeric: tabular-nums;
}

.hl-hero__status {
    flex: none;
    min-width: 168px;
    text-align: right;
}

.hl-hero__status b {
    color: var(--vp-c-text-1);
    font-weight: 600;
}

.hl-hero__track {
    flex: 1;
    height: 4px;
    border-radius: 2px;
    background: var(--vp-c-divider);
    overflow: hidden;
}

.hl-hero__track i {
    display: block;
    height: 100%;
    background: var(--hl-fig-accent);
}

.hl-hero__toggle {
    display: grid;
    flex: none;
    place-items: center;
    width: 28px;
    height: 28px;
    border: 1px solid var(--vp-c-divider);
    border-radius: 50%;
    background: var(--vp-c-bg);
    color: var(--vp-c-text-1);
    cursor: pointer;
}

.hl-hero__toggle svg {
    width: 12px;
    height: 12px;
    fill: currentColor;
}

.hl-hero__panels {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 10px;
}

.hl-hero__panel {
    display: block;
    padding: 0;
    border: none;
    background: none;
    font: inherit;
    text-align: center;
    color: inherit;
    cursor: pointer;
}

.hl-hero__panel:disabled {
    cursor: default;
}

.hl-hero__toggle:focus-visible,
.hl-hero__panel:focus-visible {
    outline: 2px solid var(--vp-c-brand-1);
    outline-offset: 2px;
}

.hl-hero__label {
    display: block;
    margin-top: 6px;
    font-size: 12.5px;
    font-weight: 600;
    color: var(--vp-c-text-1);
}

.hl-hero__best {
    display: block;
    font-size: 12px;
    color: var(--vp-c-text-2);
    font-variant-numeric: tabular-nums;
}

.hl-hero figcaption {
    margin-top: 12px;
    font-size: 12.5px;
    color: var(--vp-c-text-3);
    text-align: center;
}
</style>
