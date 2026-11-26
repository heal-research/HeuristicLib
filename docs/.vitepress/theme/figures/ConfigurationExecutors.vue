<script setup lang="ts">
import { useId } from "vue";
import ArrowMarkers from "./ArrowMarkers.vue";
import HlFigure from "./HlFigure.vue";

// One configuration graph and the executor graphs that two runs build from it. Each executor graph mirrors the
// configuration and holds its own mutable fields.
const id = useId();
const arrow = `url(#${id}-arrow)`;

const top = 52.5;
const childY = (index: number) => top + 44 + index * 40;
const columns = [
    {
        x: 0.5,
        title: "Configuration",
        detail: "immutable, serves every run",
        root: ["algorithm", "200 generations"],
        children: [
            ["creator", ""],
            ["mutator", "rate 0.2"],
            ["evaluator", "cached"],
        ],
        primary: true,
    },
    {
        x: 262.5,
        title: "Executors of run 1",
        detail: "built by the run's scope",
        root: ["algorithm", "generation 40"],
        children: [
            ["creator", ""],
            ["mutator", ""],
            ["evaluator", "412 cached"],
        ],
        primary: false,
    },
    {
        x: 482.5,
        title: "Executors of run 2",
        detail: "fresh fields, nothing shared",
        root: ["algorithm", "generation 7"],
        children: [
            ["creator", ""],
            ["mutator", ""],
            ["evaluator", "98 cached"],
        ],
        primary: false,
    },
];
const rootWidth = 198;
const childWidth = 180;
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-14 -14 708 248"
            role="img"
            aria-label="One immutable configuration of an algorithm with a creator, a mutator and a cached evaluator. Two runs each build their own executors from it. The executor graph mirrors the configuration and holds mutable fields such as the current generation and the cache entries, which differ between the runs."
        >
            <ArrowMarkers :id="id" />
            <g v-for="column in columns" :key="column.title">
                <text class="t" :x="column.x" y="12">{{ column.title }}</text>
                <text class="s" :x="column.x" y="30">{{ column.detail }}</text>
                <g class="shadow">
                    <rect
                        class="node"
                        :class="{ primary: column.primary }"
                        :x="column.x"
                        :y="top"
                        :width="rootWidth"
                        height="30"
                        rx="8"
                    />
                    <rect
                        v-for="(child, index) in column.children"
                        :key="child[0]"
                        class="node"
                        :class="{ primary: column.primary }"
                        :x="column.x + 18"
                        :y="childY(index)"
                        :width="childWidth"
                        height="30"
                        rx="8"
                    />
                </g>
                <text class="t" :x="column.x + 12" :y="top + 20">{{ column.root[0] }}</text>
                <text class="mm" :x="column.x + rootWidth - 10" :y="top + 20" text-anchor="end">
                    {{ column.root[1] }}
                </text>
                <g v-for="(child, index) in column.children" :key="`${child[0]}-t`">
                    <path
                        class="edge"
                        :d="`M${column.x + 9} ${top + 30} L ${column.x + 9} ${childY(index) + 15} L ${column.x + 17} ${childY(index) + 15}`"
                    />
                    <text class="t" :x="column.x + 30" :y="childY(index) + 20">{{ child[0] }}</text>
                    <text class="mm" :x="column.x + 18 + childWidth - 10" :y="childY(index) + 20" text-anchor="end">
                        {{ child[1] }}
                    </text>
                </g>
            </g>
            <path
                class="edge"
                :d="`M${rootWidth + 4} ${top + 15} L ${columns[1].x - 4} ${top + 15}`"
                :marker-end="arrow"
            />
            <text class="el" :x="(rootWidth + columns[1].x) / 2" :y="top + 7" text-anchor="middle">resolve</text>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
