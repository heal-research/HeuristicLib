<script setup lang="ts">
import { computed, ref, useId } from "vue";
import HlFigure from "./HlFigure.vue";
import { linearScale } from "./scales";

// The final quality of every trial of a two-dimensional parameter grid: a heatmap of the median per cell and a dot
// plot of the single trials, in the same cell order.
interface Cell {
    populationSize: number;
    mutationRate: number;
    trials: number[];
}

const props = withDefaults(
    defineProps<{
        data: { cells: Cell[] };
        colorMax?: number;
    }>(),
    { colorMax: 2 },
);

const gradientId = useId();
const populations = computed(() => [...new Set(props.data.cells.map((cell) => cell.populationSize))]);
const rates = computed(() => [...new Set(props.data.cells.map((cell) => cell.mutationRate))]);
const median = (values: number[]) => (values[(values.length - 1) >> 1] + values[values.length >> 1]) / 2;
const format = (value: number) => value.toFixed(4);

// Heatmap geometry.
const cellWidth = 76;
const cellHeight = 86;
const ox = 72;
const oy = 44;
const heat = computed(() =>
    props.data.cells.map((cell) => {
        const i = populations.value.indexOf(cell.populationSize);
        const j = rates.value.indexOf(cell.mutationRate);
        const value = median(cell.trials);
        const t = Math.min(1, value / props.colorMax);
        return {
            cell,
            value,
            x: ox + j * (cellWidth + 2),
            y: oy + i * (cellHeight + 2),
            fill: `color-mix(in oklab, var(--hl-fig-seq-hi) ${(t * 100).toFixed(0)}%, var(--hl-fig-seq-lo))`,
            ink: t > 0.5 ? "var(--hl-fig-seq-on-hi)" : "var(--vp-c-text-1)",
        };
    }),
);
const legendY = computed(() => oy + populations.value.length * (cellHeight + 2) + 16);

// Dot plot geometry: one row per configuration, population and rate in two label columns.
const dotWidth = 420;
const dotHeight = 340;
const margin = { l: 108, r: 12, t: 30, b: 46 };
const populationColumn = 62;
const rateColumn = 94;
const xMax = computed(() => Math.ceil(Math.max(...props.data.cells.flatMap((cell) => cell.trials)) * 2) / 2);
const x = computed(() => linearScale(0, xMax.value + 0.2, margin.l, dotWidth - margin.r));
const xTicks = computed(() => Array.from({ length: xMax.value * 2 + 1 }, (_, i) => i / 2));
const band = computed(() => (dotHeight - margin.t - margin.b) / props.data.cells.length);
// Places equal or close values beside each other instead of on top: each dot takes the first vertical offset that
// keeps it clear of the dots already placed in its row.
function swarm(values: number[]): { value: number; dx: number; dy: number }[] {
    const placed: { value: number; dx: number; dy: number }[] = [];
    for (const value of values) {
        const dx = x.value(value);
        const dy =
            [0, -8, 8, -16, 16].find((offset) =>
                placed.every((dot) => (dot.dx - dx) ** 2 + (dot.dy - offset) ** 2 >= 8 ** 2),
            ) ?? 0;
        placed.push({ value, dx, dy });
    }
    return placed;
}

const rows = computed(() =>
    props.data.cells.map((cell, i) => ({
        cell,
        y: margin.t + band.value * i + band.value / 2,
        median: median(cell.trials),
        dots: swarm(cell.trials),
        firstOfPopulation: i === 0 || props.data.cells[i - 1].populationSize !== cell.populationSize,
    })),
);

const tableHeaders = ["population", "mutation rate", "trials, sorted", "median"];
const tableRows = computed(() =>
    props.data.cells.map((cell) => [
        cell.populationSize,
        cell.mutationRate.toFixed(1),
        cell.trials.map(format).join("  "),
        format(median(cell.trials)),
    ]),
);

// Tooltip for a heatmap cell or a trial dot.
const root = ref<HTMLElement>();
const tip = ref<{
    title: string;
    rows: { label: string; value: string; dot?: boolean }[];
    left: number;
    top: number;
}>();

function show(event: PointerEvent, title: string, rows: { label: string; value: string; dot?: boolean }[]) {
    const container = root.value;
    if (!container) return;
    const box = container.getBoundingClientRect();
    const left = event.clientX - box.left;
    const top = event.clientY - box.top;
    tip.value = { title, rows, left: left > box.width - 200 ? left - 196 : left + 16, top: Math.max(0, top - 80) };
}

const cellTitle = (cell: Cell) => `population ${cell.populationSize} · mutation ${cell.mutationRate.toFixed(1)}`;
</script>

<template>
    <HlFigure :table-headers="tableHeaders" :table-rows="tableRows">
        <div ref="root" style="position: relative">
            <div class="hl-legend">
                <span><i class="dot" style="background: var(--hl-fig-series-1)" />trial</span>
                <span><i class="tick" style="background: var(--hl-fig-accent)" />median</span>
            </div>
            <div class="hl-split">
                <svg
                    class="hl-chart"
                    viewBox="0 0 232 370"
                    role="img"
                    aria-label="Heatmap of the median final objective by population size and mutation rate."
                >
                    <text class="title" x="0" y="14">population</text>
                    <text class="title" :x="ox" y="14">mutation rate</text>
                    <text
                        v-for="(rate, j) in rates"
                        :key="rate"
                        :x="ox + j * (cellWidth + 2) + cellWidth / 2"
                        :y="oy - 8"
                        text-anchor="middle"
                    >
                        {{ rate.toFixed(1) }}
                    </text>
                    <text
                        v-for="(population, i) in populations"
                        :key="population"
                        :x="ox - 10"
                        :y="oy + i * (cellHeight + 2) + cellHeight / 2 + 4"
                        text-anchor="end"
                    >
                        {{ population }}
                    </text>
                    <g v-for="entry in heat" :key="`${entry.cell.populationSize}-${entry.cell.mutationRate}`">
                        <rect
                            :x="entry.x"
                            :y="entry.y"
                            :width="cellWidth"
                            :height="cellHeight"
                            rx="4"
                            :style="{ fill: entry.fill }"
                            @pointermove="
                                show($event, cellTitle(entry.cell), [
                                    { label: 'median of the trials', value: format(entry.value) },
                                    { label: 'best trial', value: format(entry.cell.trials[0]) },
                                ])
                            "
                            @pointerleave="tip = undefined"
                        />
                        <text
                            class="celltext"
                            :x="entry.x + cellWidth / 2"
                            :y="entry.y + cellHeight / 2 + 4.5"
                            text-anchor="middle"
                            :style="{ fill: entry.ink }"
                            pointer-events="none"
                        >
                            {{ entry.value < 0.01 ? entry.value.toFixed(4) : entry.value.toFixed(2) }}
                        </text>
                    </g>
                    <defs>
                        <linearGradient :id="gradientId" x1="0" x2="1" y1="0" y2="0">
                            <stop offset="0" style="stop-color: var(--hl-fig-seq-lo)" />
                            <stop offset="1" style="stop-color: var(--hl-fig-seq-hi)" />
                        </linearGradient>
                    </defs>
                    <rect
                        :x="ox"
                        :y="legendY"
                        :width="2 * cellWidth + 2"
                        height="8"
                        rx="4"
                        :fill="`url(#${gradientId})`"
                    />
                    <text :x="ox" :y="legendY + 22">0</text>
                    <text :x="ox + 2 * cellWidth + 2" :y="legendY + 22" text-anchor="end">
                        ≥ {{ colorMax.toFixed(1) }}
                    </text>
                    <text class="title" :x="ox" :y="legendY + 38">median, lower is better</text>
                </svg>
                <svg
                    class="hl-chart"
                    :viewBox="`0 0 ${dotWidth} ${dotHeight}`"
                    role="img"
                    aria-label="Final objective of every trial per configuration."
                >
                    <g v-for="tick in xTicks" :key="tick">
                        <line class="grid" :x1="x(tick)" :x2="x(tick)" :y1="margin.t" :y2="dotHeight - margin.b" />
                        <text :x="x(tick)" :y="dotHeight - margin.b + 17" text-anchor="middle">{{ tick }}</text>
                    </g>
                    <text class="title" :x="populationColumn" y="14" text-anchor="end">population</text>
                    <text class="title" :x="rateColumn" y="14" text-anchor="end">rate</text>
                    <g v-for="row in rows" :key="`${row.cell.populationSize}-${row.cell.mutationRate}`">
                        <line
                            v-if="row.firstOfPopulation"
                            class="axis"
                            x1="0"
                            :x2="dotWidth - margin.r"
                            :y1="row.y - band / 2"
                            :y2="row.y - band / 2"
                        />
                        <text v-if="row.firstOfPopulation" :x="populationColumn" :y="row.y + 4" text-anchor="end">
                            {{ row.cell.populationSize }}
                        </text>
                        <text :x="rateColumn" :y="row.y + 4" text-anchor="end">
                            {{ row.cell.mutationRate.toFixed(1) }}
                        </text>
                        <rect
                            :x="x(row.median) - 1"
                            :y="row.y - 19"
                            width="2"
                            height="38"
                            rx="1"
                            style="fill: var(--hl-fig-accent)"
                        />
                        <g v-for="(dot, k) in row.dots" :key="k">
                            <circle
                                :cx="dot.dx"
                                :cy="row.y + dot.dy"
                                r="3.6"
                                style="fill: var(--hl-fig-series-1); stroke: var(--vp-c-bg); stroke-width: 1.5"
                            />
                            <circle
                                :cx="dot.dx"
                                :cy="row.y + dot.dy"
                                r="7"
                                fill="transparent"
                                @pointermove="
                                    show($event, cellTitle(row.cell), [
                                        {
                                            label: `trial ${k + 1} of ${row.cell.trials.length}, sorted`,
                                            value: format(dot.value),
                                            dot: true,
                                        },
                                    ])
                                "
                                @pointerleave="tip = undefined"
                            />
                        </g>
                    </g>
                    <text
                        class="title"
                        :x="(margin.l + dotWidth - margin.r) / 2"
                        :y="dotHeight - 6"
                        text-anchor="middle"
                    >
                        best objective value at the end of the trial, lower is better
                    </text>
                    <line
                        class="axis"
                        :x1="margin.l"
                        :x2="dotWidth - margin.r"
                        :y1="dotHeight - margin.b"
                        :y2="dotHeight - margin.b"
                    />
                </svg>
            </div>
            <div v-if="tip" class="hl-tip" :style="{ left: `${tip.left}px`, top: `${tip.top}px` }">
                <div class="hd">{{ tip.title }}</div>
                <div v-for="row in tip.rows" :key="row.label" class="row">
                    <span class="key dot" :style="{ background: row.dot ? 'var(--hl-fig-series-1)' : 'transparent' }" />
                    <span class="val">{{ row.value }}</span>
                    <span class="lab">{{ row.label }}</span>
                </div>
            </div>
        </div>
        <template #caption><slot /></template>
    </HlFigure>
</template>
