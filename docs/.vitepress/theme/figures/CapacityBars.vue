<script setup lang="ts">
import HlFigure from "./HlFigure.vue";
import { linearScale } from "./scales";

// Machine hours of the plan the custom problem example finds: 23 tables and 25 shelves. The numbers come from the
// page's product table and its quoted output.
const products = [
    { name: "table", units: 23, cutting: 4, assembly: 5, color: "var(--hl-fig-ord-1)" },
    { name: "shelf", units: 25, cutting: 2, assembly: 1, color: "var(--hl-fig-ord-3)" },
];
const machines = [
    { name: "cutting", key: "cutting" as const, capacity: 160 },
    { name: "assembly", key: "assembly" as const, capacity: 140 },
];

const width = 652;
const margin = { l: 76, r: 150 };
const x = linearScale(0, 170, margin.l, width - margin.r);
const barHeight = 34;
const rowY = (index: number) => 14 + index * 64;

const rows = machines.map((machine, index) => {
    let start = 0;
    const segments = products.map((product) => {
        const hours = product.units * product[machine.key];
        const segment = { ...product, hours, x0: x(start), x1: x(start + hours) };
        start += hours;
        return segment;
    });
    const spare = machine.capacity - start;
    return { ...machine, y: rowY(index), segments, used: start, spare };
});

const tableHeaders = ["machine", ...products.map((product) => `${product.name} hours`), "used", "capacity"];
const tableRows = rows.map((row) => [
    row.name,
    ...row.segments.map((segment) => segment.hours),
    row.used,
    row.capacity,
]);
</script>

<template>
    <HlFigure :table-headers="tableHeaders" :table-rows="tableRows">
        <div class="hl-legend">
            <span v-for="product in products" :key="product.name"
                ><i class="box" :style="{ background: product.color }" />{{ product.name }}</span
            >
        </div>
        <svg
            class="hl-chart"
            :viewBox="`0 0 ${width} 158`"
            role="img"
            aria-label="Cutting uses 142 of 160 hours: 92 for tables and 50 for shelves, with 18 hours spare. Assembly uses 140 of 140 hours: 115 for tables and 25 for shelves, so assembly is the bottleneck."
        >
            <g v-for="row in rows" :key="row.name">
                <text class="title" :x="margin.l - 10" :y="row.y + barHeight / 2 + 4" text-anchor="end">
                    {{ row.name }}
                </text>
                <rect
                    :x="x(0)"
                    :y="row.y"
                    :width="x(row.capacity) - x(0)"
                    :height="barHeight"
                    rx="4"
                    :style="{ fill: 'none', stroke: 'var(--hl-fig-axis)', strokeDasharray: '3 3' }"
                />
                <rect
                    v-for="(segment, index) in row.segments"
                    :key="segment.name"
                    :x="segment.x0 + (index ? 1 : 0)"
                    :y="row.y"
                    :width="segment.x1 - segment.x0 - (index ? 1 : 0)"
                    :height="barHeight"
                    :rx="index === row.segments.length - 1 ? 4 : 0"
                    :style="{ fill: segment.color }"
                />
                <text
                    v-for="(segment, index) in row.segments"
                    :key="`${segment.name}-label`"
                    :x="segment.x0 + 8"
                    :y="row.y + barHeight / 2 + 4"
                    :style="{
                        fill: index === 0 ? 'var(--hl-fig-seq-on-hi)' : 'var(--vp-c-text-1)',
                        fontSize: '11.5px',
                    }"
                >
                    {{ segment.x1 - segment.x0 > 52 ? `${segment.hours} h` : segment.hours }}
                </text>
                <line
                    class="ref"
                    :x1="x(row.capacity)"
                    :x2="x(row.capacity)"
                    :y1="row.y - 6"
                    :y2="row.y + barHeight + 6"
                />
                <text class="note" :x="x(row.capacity) + 10" :y="row.y + 13">
                    {{ row.used }} of {{ row.capacity }} h
                </text>
                <text :x="x(row.capacity) + 10" :y="row.y + 29">
                    {{ row.spare > 0 ? `${row.spare} h spare` : "no hours spare" }}
                </text>
            </g>
            <line class="axis" :x1="x(0)" :x2="x(170)" y1="132" y2="132" />
            <g v-for="tick in [0, 40, 80, 120, 160]" :key="tick">
                <line class="axis" :x1="x(tick)" :x2="x(tick)" y1="132" y2="136" />
                <text :x="x(tick)" y="148" text-anchor="middle">{{ tick }}</text>
            </g>
            <text class="title" :x="x(170) + 8" y="148">hours</text>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
