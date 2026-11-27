<script setup lang="ts">
import { useId } from "vue";
import ArrowMarkers from "./ArrowMarkers.vue";
import HlFigure from "./HlFigure.vue";

// A problem, an algorithm and a random source combine into a run, which streams search states or returns the last.
const id = useId();
const arrow = `url(#${id}-arrow)`;
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-16 -16 712 332"
            role="img"
            aria-label="A problem, an algorithm configuration and a random source combine into a run. RunStreaming yields every search state. RunToCompletionAsync returns the final state."
        >
            <ArrowMarkers :id="id" />
            <g class="shadow">
                <rect class="node" x="0.5" y="0.5" width="210" height="88" rx="10" />
                <rect class="node" x="0.5" y="106.5" width="210" height="88" rx="10" />
                <rect class="node" x="0.5" y="212.5" width="210" height="87" rx="10" />
            </g>
            <text class="t" x="16" y="26">Problem</text>
            <text class="s" x="16" y="45">what counts as a good answer</text>
            <rect class="chip" x="16" y="56" width="150" height="20" rx="5" />
            <text class="m" x="24" y="70">TestFunctionProblem</text>
            <text class="t" x="16" y="132">Algorithm</text>
            <text class="s" x="16" y="151">operators and budgets</text>
            <rect class="chip" x="16" y="162" width="126" height="20" rx="5" />
            <text class="m" x="24" y="176">GeneticAlgorithm</text>
            <text class="t" x="16" y="238">Random source</text>
            <text class="s" x="16" y="257">a recorded seed</text>
            <rect class="chip" x="16" y="268" width="80" height="20" rx="5" />
            <text class="m" x="24" y="282">seed: 123</text>

            <path class="edge" d="M211 44 C 245 44, 245 128, 268 128" :marker-end="arrow" />
            <path class="edge" d="M211 150 L 268 150" :marker-end="arrow" />
            <path class="edge" d="M211 256 C 245 256, 245 172, 268 172" :marker-end="arrow" />

            <rect class="node primary" x="270.5" y="104.5" width="150" height="92" rx="10" />
            <text class="t" x="345" y="138" text-anchor="middle">Run</text>
            <text class="s" x="345" y="157" text-anchor="middle">fresh execution state</text>
            <text class="s" x="345" y="174" text-anchor="middle">for every call</text>

            <rect class="node soft" x="500.5" y="22.5" width="178" height="66" rx="10" />
            <rect class="node soft" x="492.5" y="31.5" width="180" height="66" rx="10" />
            <g class="shadow"><rect class="node" x="484.5" y="40.5" width="182" height="66" rx="10" /></g>
            <text class="t" x="500" y="64">Search states</text>
            <text class="s" x="500" y="83">one per generation, in order</text>
            <g class="shadow"><rect class="node" x="484.5" y="208.5" width="182" height="66" rx="10" /></g>
            <text class="t" x="500" y="232">Final state</text>
            <text class="s" x="500" y="251">the last state of the stream</text>

            <path class="edge" d="M421 132 C 452 132, 452 74, 481 74" :marker-end="arrow" />
            <path class="edge" d="M421 168 C 452 168, 452 241, 481 241" :marker-end="arrow" />
            <text class="el m" x="476" y="60" text-anchor="end">RunStreaming</text>
            <text class="el m" x="476" y="262" text-anchor="end">RunToCompletionAsync</text>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
