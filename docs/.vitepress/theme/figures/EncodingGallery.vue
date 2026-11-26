<script setup lang="ts">
import HlFigure from "./HlFigure.vue";

// One small candidate per built-in search space, with the rule its members satisfy.
const cell = 42;
const gap = 4;
const left = 262;
const rowHeight = 62;
const cellX = (index: number) => left + index * (cell + gap) + 0.5;

const real = [0.73, -2.41, 4.05, 1.18, -4.6];
const bound = 5.12;
const rows = [
    {
        type: "RealVector",
        space: "BoundedRealVectorSearchSpace",
        rule: ["each value within", "its bounds"],
        cells: real.map((value) => ({ text: value.toFixed(1), on: false })),
    },
    {
        type: "IntegerVector",
        space: "IntegerVectorSearchSpace",
        rule: ["whole numbers", "within bounds"],
        cells: [3, 0, 7, 2, 5, 1].map((value) => ({ text: String(value), on: false })),
    },
    {
        type: "BoolVector",
        space: "BoolVectorSearchSpace",
        rule: ["each position", "on or off"],
        cells: [1, 0, 1, 1, 0, 0, 1].map((value) => ({ text: String(value), on: value === 1 })),
    },
    {
        type: "BoolVector",
        space: "FixedCardinalityBoolVectorSearchSpace",
        rule: ["exactly k = 3", "positions on"],
        cells: [0, 1, 0, 0, 1, 0, 1].map((value) => ({ text: String(value), on: value === 1 })),
    },
    {
        type: "Permutation",
        space: "PermutationSearchSpace",
        rule: ["each index", "exactly once"],
        cells: [3, 0, 4, 1, 6, 2, 5].map((value) => ({ text: String(value), on: false })),
    },
].map((row, index) => ({ ...row, y: index * rowHeight }));

const ruleX = cellX(7) + 10;
const trackX = (value: number) => (cell * (value + bound)) / (2 * bound);
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-14 -14 716 324"
            role="img"
            aria-label="One example candidate per search space. A real vector holds values within bounds. An integer vector holds whole numbers within bounds. A bool vector sets each position on or off. A fixed cardinality bool vector has exactly three positions on. A permutation contains each index exactly once."
        >
            <g v-for="(row, r) in rows" :key="row.space">
                <line v-if="r > 0" class="divider" x1="0" x2="702" :y1="row.y - 13" :y2="row.y - 13" />
                <text class="mt" x="0" :y="row.y + 14">{{ row.type }}</text>
                <text class="mm" x="0" :y="row.y + 32">{{ row.space }}</text>
                <g v-for="(c, i) in row.cells" :key="i">
                    <rect
                        class="cell"
                        :class="{ on: c.on }"
                        :x="cellX(i)"
                        :y="row.y + 0.5"
                        :width="cell"
                        height="30"
                        rx="5"
                    />
                    <text
                        class="cell-t"
                        :class="{ on: c.on }"
                        :x="cellX(i) + cell / 2"
                        :y="row.y + 20"
                        text-anchor="middle"
                    >
                        {{ c.text }}
                    </text>
                </g>
                <g v-if="r === 0">
                    <g v-for="(value, i) in real" :key="`track${i}`">
                        <line
                            class="track"
                            :x1="cellX(i) + 2"
                            :x2="cellX(i) + cell - 2"
                            :y1="row.y + 40"
                            :y2="row.y + 40"
                        />
                        <circle class="tick-mark" :cx="cellX(i) + trackX(value)" :cy="row.y + 40" r="3" />
                    </g>
                </g>
                <text class="s" :x="ruleX" :y="row.y + 13">{{ row.rule[0] }}</text>
                <text class="s" :x="ruleX" :y="row.y + 28">{{ row.rule[1] }}</text>
            </g>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
