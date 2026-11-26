<script setup lang="ts">
import { computed, ref } from "vue";
import HlFigure from "./HlFigure.vue";
import { formatPower, formatValue, linearScale, logScale } from "./scales";

// A line chart over one shared x axis. The data comes from a figure-data file that a documentation spec writes;
// the page states how to draw it. Series are drawn in the ordinal palette in the order given. A single series needs
// no legend because the y title names it.
interface Series {
    key: string;
    label: string;
}

interface Note {
    x: number;
    series: string;
    dx: number;
    dy: number;
    lines: string[];
}

const props = withDefaults(
    defineProps<{
        data: { x: number[]; series: Record<string, number[]> };
        series: Series[];
        label: string;
        xTitle: string;
        yTitle: string;
        xTicks: number[];
        yTicks: number[];
        yDomain: [number, number];
        yScale?: "linear" | "log";
        note?: Note;
        tableEvery?: number;
        height?: number;
        unit?: string;
        digits?: number;
    }>(),
    { yScale: "linear", tableEvery: 1, height: 250, unit: "" },
);

const width = 652;
const margin = { t: 22, r: 88, b: 30, l: 46 };
const colors = ["var(--hl-fig-ord-1)", "var(--hl-fig-ord-2)", "var(--hl-fig-ord-3)"];

const xs = computed(() => props.data.x);
const x = computed(() => linearScale(xs.value[0], xs.value[xs.value.length - 1], margin.l, width - margin.r));
const y = computed(() => {
    const [d0, d1] = props.yDomain;
    const r0 = props.height - margin.b;
    return props.yScale === "log" ? logScale(d0, d1, r0, margin.t) : linearScale(d0, d1, r0, margin.t);
});

const lines = computed(() =>
    props.series.map((series, index) => {
        const values = props.data.series[series.key];
        const d = values
            .map((value, i) => `${i ? "L" : "M"}${x.value(xs.value[i]).toFixed(1)} ${y.value(value).toFixed(1)}`)
            .join("");
        return { ...series, values, d, color: colors[index % colors.length] };
    }),
);

// End labels from top to bottom, skipping one that would overlap the label above it.
const endLabels = computed(() => {
    const last = xs.value.length - 1;
    const sorted = lines.value
        .map((line) => ({ label: line.label, y: y.value(line.values[last]) }))
        .sort((a, b) => a.y - b.y);
    let previous = -Infinity;
    return sorted.filter((label) => {
        if (label.y - previous < 14) return false;
        previous = label.y;
        return true;
    });
});

const note = computed(() => {
    const n = props.note;
    if (!n) return undefined;
    const index = xs.value.indexOf(n.x);
    const px = x.value(n.x);
    const py = y.value(props.data.series[n.series][index]);
    const lx = px + n.dx;
    const ly = py + n.dy;
    const anchor = n.dx < 0 ? "end" : "start";
    const tx = lx + (n.dx < 0 ? -6 : 6);
    const texts = n.lines.map((text, i) => ({ text, y: ly + 4 + i * 15 - ((n.lines.length - 1) * 15) / 2 }));
    return { px, py, lx, ly, anchor, tx, texts };
});

// A readout or table value: fixed decimals when the settings give them, otherwise the shared format.
const format = (value: number) =>
    (props.digits === undefined ? formatValue(value) : value.toFixed(props.digits)) + props.unit;

const tableHeaders = computed(() => [props.xTitle, ...props.series.map((series) => series.label)]);
const tableRows = computed(() =>
    xs.value.flatMap((value, i) =>
        i === 0 || value % props.tableEvery === 0
            ? [[value, ...lines.value.map((line) => format(line.values[i]))]]
            : [],
    ),
);

const tickLabel = (value: number) => (props.yScale === "log" ? formatPower(value) : String(value)) + props.unit;

// Hover readout: the nearest x value, its point on every line and a tooltip with the values.
const svg = ref<SVGSVGElement>();
const root = ref<HTMLElement>();
const hover = ref<{ index: number; left: number; top: number }>();

function onMove(event: PointerEvent) {
    const element = svg.value;
    const container = root.value;
    const matrix = element?.getScreenCTM();
    if (!element || !container || !matrix) return;
    const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    const target = x.value.invert(point.x);
    let lo = 0;
    let hi = xs.value.length - 1;
    while (hi - lo > 1) {
        const mid = (lo + hi) >> 1;
        if (xs.value[mid] < target) lo = mid;
        else hi = mid;
    }
    const index = Math.abs(xs.value[lo] - target) <= Math.abs(xs.value[hi] - target) ? lo : hi;
    const box = container.getBoundingClientRect();
    const left = event.clientX - box.left;
    const top = event.clientY - box.top;
    hover.value = {
        index,
        left: left > box.width - 180 ? left - 176 : left + 16,
        top: Math.max(0, top - 96),
    };
}
</script>

<template>
    <HlFigure :table-headers="tableHeaders" :table-rows="tableRows">
        <div ref="root" style="position: relative">
            <div v-if="lines.length > 1" class="hl-legend">
                <span v-for="line in lines" :key="line.key"
                    ><i :style="{ background: line.color }" />{{ line.label }}</span
                >
            </div>
            <svg ref="svg" class="hl-chart" :viewBox="`0 0 ${width} ${height}`" role="img" :aria-label="label">
                <g v-for="tick in yTicks" :key="`y${tick}`">
                    <line class="grid" :x1="margin.l" :x2="width - margin.r" :y1="y(tick)" :y2="y(tick)" />
                    <text :x="margin.l - 8" :y="y(tick) + 3.5" text-anchor="end">{{ tickLabel(tick) }}</text>
                </g>
                <line
                    class="axis"
                    :x1="margin.l"
                    :x2="width - margin.r"
                    :y1="height - margin.b"
                    :y2="height - margin.b"
                />
                <g v-for="tick in xTicks" :key="`x${tick}`">
                    <line
                        class="axis"
                        :x1="x(tick)"
                        :x2="x(tick)"
                        :y1="height - margin.b"
                        :y2="height - margin.b + 4"
                    />
                    <text :x="x(tick)" :y="height - margin.b + 17" text-anchor="middle">{{ tick }}</text>
                </g>
                <text class="title" :x="margin.l - 8" y="12">{{ yTitle }}</text>
                <text class="title" :x="width - margin.r + 26" :y="height - margin.b + 17">{{ xTitle }}</text>
                <path v-for="line in lines" :key="line.key" class="line" :d="line.d" :style="{ stroke: line.color }" />
                <text
                    v-for="end in endLabels"
                    :key="end.label"
                    class="endlabel"
                    :x="width - margin.r + 8"
                    :y="end.y + 4"
                >
                    {{ end.label }}
                </text>
                <g v-if="note">
                    <path class="note-line" :d="`M${note.px} ${note.py} L${note.lx} ${note.ly}`" />
                    <circle class="note-dot" :cx="note.px" :cy="note.py" r="4" />
                    <text
                        v-for="t in note.texts"
                        :key="t.text"
                        class="note"
                        :x="note.tx"
                        :y="t.y"
                        :text-anchor="note.anchor"
                    >
                        {{ t.text }}
                    </text>
                </g>
                <g v-if="hover">
                    <line
                        class="cross"
                        :x1="x(xs[hover.index])"
                        :x2="x(xs[hover.index])"
                        :y1="margin.t"
                        :y2="height - margin.b"
                    />
                    <circle
                        v-for="line in lines"
                        :key="line.key"
                        class="hoverdot"
                        r="4"
                        :cx="x(xs[hover.index])"
                        :cy="y(line.values[hover.index])"
                        :style="{ fill: line.color }"
                    />
                </g>
                <rect
                    :x="margin.l"
                    :y="margin.t"
                    :width="width - margin.l - margin.r"
                    :height="height - margin.t - margin.b"
                    fill="transparent"
                    @pointermove="onMove"
                    @pointerleave="hover = undefined"
                />
            </svg>
            <div v-if="hover" class="hl-tip" :style="{ left: `${hover.left}px`, top: `${hover.top}px` }">
                <div class="hd">{{ xTitle }} {{ xs[hover.index] }}</div>
                <div v-for="line in lines" :key="line.key" class="row">
                    <span class="key" :style="{ background: line.color }" />
                    <span class="val">{{ format(line.values[hover.index]) }}</span>
                    <span class="lab">{{ line.label }}</span>
                </div>
            </div>
        </div>
        <template #caption><slot /></template>
    </HlFigure>
</template>
