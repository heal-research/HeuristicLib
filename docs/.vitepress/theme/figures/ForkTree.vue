<script setup lang="ts">
import { useId } from "vue";
import ArrowMarkers from "./ArrowMarkers.vue";
import HlFigure from "./HlFigure.vue";

// How one root generator fans out: a fork per run, a fork per iteration inside a run, and the stream that the
// iteration hands to the step's operators. A terminator added with TerminatedBy draws from a stream of its own.
const id = useId();
const arrow = `url(#${id}-arrow)`;

const root = { x: 0.5, y: 84.5, w: 120, h: 56 };
const runs = [
    { y: 20.5, label: "run 0", key: "Fork(0)", primary: false },
    { y: 92.5, label: "run 1", key: "Fork(1)", primary: true },
    { y: 180.5, label: "run 19", key: "Fork(19)", primary: false },
];
const runX = 160.5;
const runW = 130;
const iterations = [
    { y: 0.5, label: "iteration 0", key: "Fork(0)", primary: false },
    { y: 52.5, label: "iteration 1", key: "Fork(1)", primary: true },
    { y: 104.5, label: "iteration 2", key: "Fork(2)", primary: false },
];
const iterationX = 330.5;
const iterationW = 160;
const step = { x: 530.5, y: 18.5, w: 170, h: 108 };
const terminator = { y: 172.5, h: 64 };

// A horizontal connector that bends once between two points.
const connect = (x1: number, y1: number, x2: number, y2: number) => {
    const mid = (x1 + x2) / 2;
    return `M${x1} ${y1} C ${mid} ${y1}, ${mid} ${y2}, ${x2} ${y2}`;
};
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-14 -14 728 266"
            role="img"
            aria-label="The root generator with seed 123 forks one stream per run. Inside a run, the algorithm forks one stream per iteration, keyed by the iteration index, and passes it to the step's operators, the interceptor and the algorithm's terminator. A terminator added with TerminatedBy receives a separate stream under a dedicated key, forked once per yielded state."
        >
            <ArrowMarkers :id="id" />
            <rect class="node primary" :x="root.x" :y="root.y" :width="root.w" :height="root.h" rx="10" />
            <text class="t" x="14" :y="root.y + 23">Root</text>
            <text class="m" x="14" :y="root.y + 41">seed: 123</text>

            <g class="shadow">
                <rect
                    v-for="run in runs"
                    :key="run.label"
                    class="node"
                    :x="runX"
                    :y="run.y"
                    :width="runW"
                    height="40"
                    rx="9"
                />
                <rect
                    v-for="iteration in iterations"
                    :key="iteration.label"
                    class="node"
                    :x="iterationX"
                    :y="iteration.y"
                    :width="iterationW"
                    height="40"
                    rx="9"
                />
                <rect
                    class="node"
                    :x="iterationX"
                    :y="terminator.y"
                    :width="iterationW"
                    :height="terminator.h"
                    rx="9"
                />
                <rect class="node" :x="step.x" :y="step.y" :width="step.w" :height="step.h" rx="10" />
            </g>

            <g v-for="run in runs" :key="`${run.label}-t`">
                <rect v-if="run.primary" class="node primary" :x="runX" :y="run.y" :width="runW" height="40" rx="9" />
                <text class="t" :x="runX + 12" :y="run.y + 25">{{ run.label }}</text>
                <text class="mm" :x="runX + runW - 12" :y="run.y + 25" text-anchor="end">{{ run.key }}</text>
                <path
                    class="edge"
                    :d="connect(root.x + root.w, root.y + root.h / 2, runX - 2, run.y + 20)"
                    :marker-end="arrow"
                />
            </g>
            <text class="s" :x="runX + runW / 2" y="160" text-anchor="middle">⋮</text>

            <g v-for="iteration in iterations" :key="`${iteration.label}-t`">
                <rect
                    v-if="iteration.primary"
                    class="node primary"
                    :x="iterationX"
                    :y="iteration.y"
                    :width="iterationW"
                    height="40"
                    rx="9"
                />
                <text class="t" :x="iterationX + 12" :y="iteration.y + 25">{{ iteration.label }}</text>
                <text class="mm" :x="iterationX + iterationW - 12" :y="iteration.y + 25" text-anchor="end">
                    {{ iteration.key }}
                </text>
                <path
                    class="edge"
                    :d="connect(runX + runW, runs[1].y + 20, iterationX - 2, iteration.y + 20)"
                    :marker-end="arrow"
                />
            </g>
            <text class="s" :x="iterationX + iterationW / 2" y="160" text-anchor="middle">⋮</text>

            <text class="m" :x="iterationX + 12" :y="terminator.y + 23">TerminatedBy</text>
            <text class="s" :x="iterationX + 12" :y="terminator.y + 41">dedicated key, then</text>
            <text class="s" :x="iterationX + 12" :y="terminator.y + 55">one fork per state</text>
            <path
                class="edge"
                :d="connect(runX + runW, runs[1].y + 20, iterationX - 2, terminator.y + terminator.h / 2)"
                :marker-end="arrow"
            />

            <text class="t" :x="step.x + 14" :y="step.y + 24">The same stream for</text>
            <text class="s" :x="step.x + 14" :y="step.y + 44">the step's operators</text>
            <text class="s" :x="step.x + 14" :y="step.y + 61">the interceptor</text>
            <text class="s" :x="step.x + 14" :y="step.y + 78">the algorithm's</text>
            <text class="s" :x="step.x + 14" :y="step.y + 93">terminator</text>
            <path
                class="edge"
                :d="connect(iterationX + iterationW, iterations[1].y + 20, step.x - 2, iterations[1].y + 20)"
                :marker-end="arrow"
            />
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
