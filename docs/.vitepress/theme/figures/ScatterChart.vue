<script setup lang="ts">
import { computed, ref } from "vue";
import HlFigure from "./HlFigure.vue";
import { linearScale } from "./scales";

// A scatter chart of point groups drawn in order, so a later group lies on top. The settings file builds the groups
// from its figure data and states the table and the tooltip rows.
export interface Point {
    x: number;
    y: number;
    [key: string]: number;
}

interface Group {
    label: string;
    color: string;
    points: Point[];
    radius?: number;
    ring?: boolean;
}

interface Reference {
    y: number;
    label: string;
    labelX: number;
}

interface Note {
    x: number;
    y: number;
    dx: number;
    dy: number;
    lines: string[];
}

const props = withDefaults(
    defineProps<{
        groups: Group[];
        label: string;
        xTitle: string;
        yTitle: string;
        xTicks: number[];
        yTicks: number[];
        xDomain: [number, number];
        yDomain: [number, number];
        tickFormat?: (value: number) => string;
        yTickFormat?: (value: number) => string;
        reference?: Reference;
        note?: Note;
        tip: (point: Point) => { label: string; value: string }[];
        tableHeaders: string[];
        tableRows: (string | number)[][];
        height?: number;
    }>(),
    { height: 300, tickFormat: (value: number) => String(value) },
);

const width = 652;
const margin = { t: 22, r: 24, b: 30, l: 46 };

const x = computed(() => linearScale(props.xDomain[0], props.xDomain[1], margin.l, width - margin.r));
const y = computed(() => linearScale(props.yDomain[0], props.yDomain[1], props.height - margin.b, margin.t));
const yTick = (value: number) => (props.yTickFormat ?? props.tickFormat)(value);

const marks = computed(() =>
    props.groups.flatMap((group) =>
        group.points.map((point) => ({
            group,
            point,
            px: x.value(point.x),
            py: y.value(point.y),
        })),
    ),
);

const note = computed(() => {
    const n = props.note;
    if (!n) return undefined;
    const px = x.value(n.x);
    const py = y.value(n.y);
    const lx = px + n.dx;
    const ly = py + n.dy;
    const anchor = n.dx < 0 ? "end" : "start";
    const tx = lx + (n.dx < 0 ? -6 : 6);
    const texts = n.lines.map((text, i) => ({ text, y: ly + 4 + i * 15 - ((n.lines.length - 1) * 15) / 2 }));
    return { px, py, lx, ly, anchor, tx, texts };
});

// Hover: the nearest mark within 24 units, preferring a later group on a tie.
const svg = ref<SVGSVGElement>();
const root = ref<HTMLElement>();
const hover = ref<{ mark: (typeof marks.value)[number]; left: number; top: number }>();

function onMove(event: PointerEvent) {
    const element = svg.value;
    const container = root.value;
    const matrix = element?.getScreenCTM();
    if (!element || !container || !matrix) return;
    const cursor = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    let nearest: (typeof marks.value)[number] | undefined;
    let distance = 24 * 24;
    for (const mark of marks.value) {
        const d = (mark.px - cursor.x) ** 2 + (mark.py - cursor.y) ** 2;
        if (d <= distance) {
            distance = d;
            nearest = mark;
        }
    }
    if (!nearest) {
        hover.value = undefined;
        return;
    }
    const box = container.getBoundingClientRect();
    const left = event.clientX - box.left;
    const top = event.clientY - box.top;
    hover.value = {
        mark: nearest,
        left: left > box.width - 180 ? left - 176 : left + 16,
        top: Math.max(0, top - 110),
    };
}
</script>

<template>
    <HlFigure :table-headers="tableHeaders" :table-rows="tableRows">
        <div ref="root" style="position: relative">
            <div class="hl-legend">
                <span v-for="group in groups" :key="group.label"
                    ><i class="dot" :style="{ background: group.color }" />{{ group.label }}</span
                >
            </div>
            <svg ref="svg" class="hl-chart" :viewBox="`0 0 ${width} ${height}`" role="img" :aria-label="label">
                <g v-for="tick in yTicks" :key="`y${tick}`">
                    <line class="grid" :x1="margin.l" :x2="width - margin.r" :y1="y(tick)" :y2="y(tick)" />
                    <text :x="margin.l - 8" :y="y(tick) + 3.5" text-anchor="end">{{ yTick(tick) }}</text>
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
                    <text :x="x(tick)" :y="height - margin.b + 17" text-anchor="middle">{{ tickFormat(tick) }}</text>
                </g>
                <text class="title" :x="margin.l - 8" y="12">{{ yTitle }}</text>
                <text class="title" :x="width - margin.r" :y="height - margin.b - 8" text-anchor="end">
                    {{ xTitle }}
                </text>
                <g v-if="reference">
                    <line class="ref" :x1="margin.l" :x2="width - margin.r" :y1="y(reference.y)" :y2="y(reference.y)" />
                    <text class="reflabel" :x="x(reference.labelX)" :y="y(reference.y) - 6">
                        {{ reference.label }}
                    </text>
                </g>
                <circle
                    v-for="(mark, index) in marks"
                    :key="index"
                    :cx="mark.px"
                    :cy="mark.py"
                    :r="mark.group.radius ?? 3.5"
                    :style="{
                        fill: mark.group.color,
                        stroke: mark.group.ring === false ? 'none' : 'var(--vp-c-bg)',
                        strokeWidth: 1.5,
                    }"
                />
                <g v-if="note">
                    <path class="note-line" :d="`M${note.px} ${note.py} L${note.lx} ${note.ly}`" />
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
                <circle
                    v-if="hover"
                    :cx="hover.mark.px"
                    :cy="hover.mark.py"
                    r="7"
                    fill="none"
                    :style="{ stroke: 'var(--vp-c-text-1)', strokeWidth: 1.25 }"
                />
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
                <div class="hd">{{ hover.mark.group.label }}</div>
                <div v-for="(row, index) in tip(hover.mark.point)" :key="row.label" class="row">
                    <span
                        class="key dot"
                        :style="{ background: index === 0 ? hover.mark.group.color : 'transparent' }"
                    />
                    <span class="val">{{ row.value }}</span>
                    <span class="lab">{{ row.label }}</span>
                </div>
            </div>
        </div>
        <template #caption><slot /></template>
    </HlFigure>
</template>
