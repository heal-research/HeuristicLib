<script setup lang="ts">
import { computed, ref } from "vue";
import HlFigure from "./HlFigure.vue";
import { linearScale } from "./scales";
import data from "../../figure-data/symbolic-regression.json";

// The best expression of a symbolic regression run as a tree, and its predictions against the training data.
interface TreeNode {
    label: string;
    kind: string;
    children: TreeNode[];
}

// Tree layout: leaves sit side by side in order, and a parent sits centered above its children.
const tree = { l: 0, r: 300, t: 18, level: 56 };
const placed: { label: string; kind: string; x: number; y: number; parent?: { x: number; y: number } }[] = [];
const leafCount = (node: TreeNode): number =>
    node.children.length ? node.children.reduce((sum, child) => sum + leafCount(child), 0) : 1;
const pitch = (tree.r - tree.l) / leafCount(data.tree as TreeNode);
const symbols: Record<string, string> = { "*": "×", "-": "−", "/": "÷" };
let nextLeaf = 0;
function place(node: TreeNode, depth: number): number {
    const children = node.children.map((child) => place(child, depth + 1));
    const x = children.length
        ? (placed[children[0]].x + placed[children[children.length - 1]].x) / 2
        : tree.l + pitch * (nextLeaf++ + 0.5);
    const point = { x, y: tree.t + depth * tree.level };
    for (const child of children) placed[child].parent = point;
    placed.push({ label: symbols[node.label] ?? node.label, kind: node.kind, ...point });
    return placed.length - 1;
}
place(data.tree as TreeNode, 0);
const nodes = placed;
const pillWidth = (label: string) => Math.max(32, label.length * 7.4 + 14);

// The chart of the data and the model.
const chart = { l: 382, r: 694, t: 22, b: 242 };
const values = [...data.target, ...data.prediction];
const yMin = Math.floor(Math.min(...values));
const yMax = Math.ceil(Math.max(...values));
const x = linearScale(-3, 3, chart.l, chart.r);
const y = linearScale(yMin, yMax, chart.b, chart.t);
const xTicks = [-3, -2, -1, 0, 1, 2, 3];
const yTicks = Array.from({ length: yMax - yMin + 1 }, (_, i) => yMin + i).filter((tick) =>
    yMax - yMin <= 8 ? true : tick % 2 === 0,
);
const model = data.x
    .map((value, i) => `${i ? "L" : "M"}${x(value).toFixed(1)} ${y(data.prediction[i]).toFixed(1)}`)
    .join("");

const tableHeaders = ["x", "data", "model"];
const tableRows = data.x.map((value, i) => [
    value.toFixed(1),
    data.target[i].toFixed(2),
    data.prediction[i].toFixed(2),
]);

const svg = ref<SVGSVGElement>();
const hover = ref<number>();
function onMove(event: PointerEvent) {
    const matrix = svg.value?.getScreenCTM();
    if (!matrix) return;
    const cursor = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    const target = x.invert(cursor.x);
    let nearest = 0;
    data.x.forEach((value, i) => {
        if (Math.abs(value - target) < Math.abs(data.x[nearest] - target)) nearest = i;
    });
    hover.value = nearest;
}
const readout = computed(() =>
    hover.value === undefined
        ? undefined
        : `x ${data.x[hover.value].toFixed(1)} · data ${data.target[hover.value].toFixed(2)} · model ${data.prediction[hover.value].toFixed(2)}`,
);
</script>

<template>
    <HlFigure :table-headers="tableHeaders" :table-rows="tableRows">
        <svg
            ref="svg"
            class="hl-dg"
            viewBox="-14 -14 722 276"
            role="img"
            :aria-label="`The best expression as a tree, equal to ${data.infix}, and its predictions as a line through the 31 noisy training samples. The root mean squared error is ${data.rmse}.`"
        >
            <path
                v-for="(node, i) in nodes.filter((n) => n.parent)"
                :key="`e${i}`"
                class="edge"
                :d="`M${node.parent!.x} ${node.parent!.y} L ${node.x} ${node.y}`"
            />
            <g v-for="(node, i) in nodes" :key="`n${i}`">
                <rect
                    class="node"
                    :x="node.x - pillWidth(node.label) / 2"
                    :y="node.y - 14"
                    :width="pillWidth(node.label)"
                    height="28"
                    rx="14"
                />
                <rect
                    class="node"
                    :class="{ primary: node.kind === 'operation' }"
                    :x="node.x - pillWidth(node.label) / 2"
                    :y="node.y - 14"
                    :width="pillWidth(node.label)"
                    height="28"
                    rx="14"
                />
                <text
                    :class="node.kind === 'operation' ? 't' : 'cell-t'"
                    :x="node.x"
                    :y="node.y + (node.kind === 'operation' ? 5.5 : 4)"
                    text-anchor="middle"
                    :style="node.kind === 'operation' ? { fontSize: '16px' } : undefined"
                >
                    {{ node.label }}
                </text>
            </g>

            <g class="hl-chart">
                <g v-for="tick in yTicks" :key="`y${tick}`">
                    <line class="grid" :x1="chart.l" :x2="chart.r" :y1="y(tick)" :y2="y(tick)" />
                    <text :x="chart.l - 8" :y="y(tick) + 3.5" text-anchor="end">{{ tick }}</text>
                </g>
                <line class="axis" :x1="chart.l" :x2="chart.r" :y1="chart.b" :y2="chart.b" />
                <g v-for="tick in xTicks" :key="`x${tick}`">
                    <line class="axis" :x1="x(tick)" :x2="x(tick)" :y1="chart.b" :y2="chart.b + 4" />
                    <text :x="x(tick)" :y="chart.b + 17" text-anchor="middle">{{ tick }}</text>
                </g>
                <text class="title" :x="chart.l - 8" :y="chart.t - 10">y</text>
                <text class="title" :x="chart.r" :y="chart.b - 8" text-anchor="end">x</text>
                <circle
                    v-for="(value, i) in data.x"
                    :key="`d${i}`"
                    :cx="x(value)"
                    :cy="y(data.target[i])"
                    r="3.5"
                    :style="{ fill: 'var(--hl-fig-context)', stroke: 'var(--vp-c-bg)', strokeWidth: 1.5 }"
                />
                <path class="line" :d="model" :style="{ stroke: 'var(--hl-fig-series-1)' }" />
                <g v-if="hover !== undefined">
                    <line class="ref" :x1="x(data.x[hover])" :x2="x(data.x[hover])" :y1="chart.t" :y2="chart.b" />
                    <circle
                        :cx="x(data.x[hover])"
                        :cy="y(data.target[hover])"
                        r="6"
                        fill="none"
                        :style="{ stroke: 'var(--vp-c-text-1)', strokeWidth: 1.25 }"
                    />
                    <text class="note" :x="chart.r" :y="chart.t - 10" text-anchor="end">{{ readout }}</text>
                </g>
                <g v-else>
                    <circle :cx="chart.r - 196" :cy="chart.t - 14" r="3.5" :style="{ fill: 'var(--hl-fig-context)' }" />
                    <text class="title" :x="chart.r - 188" :y="chart.t - 10">data</text>
                    <line
                        class="line"
                        :x1="chart.r - 150"
                        :x2="chart.r - 134"
                        :y1="chart.t - 14"
                        :y2="chart.t - 14"
                        :style="{ stroke: 'var(--hl-fig-series-1)' }"
                    />
                    <text class="title" :x="chart.r - 128" :y="chart.t - 10">model, RMSE {{ data.rmse }}</text>
                </g>
                <rect
                    :x="chart.l"
                    :y="chart.t"
                    :width="chart.r - chart.l"
                    :height="chart.b - chart.t"
                    fill="transparent"
                    @pointermove="onMove"
                    @pointerleave="hover = undefined"
                />
            </g>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
