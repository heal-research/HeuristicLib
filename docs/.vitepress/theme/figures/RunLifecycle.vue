<script setup lang="ts">
import { useId } from "vue";
import ArrowMarkers from "./ArrowMarkers.vue";
import HlFigure from "./HlFigure.vue";

// The lifecycle states of an AlgorithmRun, from Ready to the three terminal states.
const id = useId();
const arrow = `url(#${id}-arrow)`;
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-24 -8 712 256"
            role="img"
            aria-label="AlgorithmRun states. Ready moves to Running when Stream is called. Running moves to Paused when the stream is disposed or canceled, and Stream resumes it. Running ends in Completed when the iterator ends, in Failed on an exception, and in Canceled when setup is canceled."
        >
            <ArrowMarkers :id="id" />
            <g class="shadow">
                <rect class="node" x="0.5" y="96.5" width="124" height="38" rx="19" />
                <rect class="node primary" x="218.5" y="96.5" width="124" height="38" rx="19" />
                <rect class="node" x="218.5" y="190.5" width="124" height="38" rx="19" />
            </g>
            <rect class="node terminal" x="508.5" y="16.5" width="150" height="38" rx="19" />
            <rect class="node terminal" x="508.5" y="96.5" width="150" height="38" rx="19" />
            <rect class="node terminal" x="508.5" y="176.5" width="150" height="38" rx="19" />
            <text class="t" x="62.5" y="120" text-anchor="middle">Ready</text>
            <text class="t" x="280.5" y="120" text-anchor="middle">Running</text>
            <text class="t" x="280.5" y="214" text-anchor="middle">Paused</text>
            <text class="t" x="583.5" y="40" text-anchor="middle">Completed</text>
            <text class="t" x="583.5" y="120" text-anchor="middle">Failed</text>
            <text class="t" x="583.5" y="200" text-anchor="middle">Canceled</text>
            <text class="s" x="62.5" y="156" text-anchor="middle">analyzers fixed</text>
            <text class="s" x="62.5" y="172" text-anchor="middle">when CreateRun returns</text>

            <path class="edge" d="M126 115.5 L 215 115.5" :marker-end="arrow" />
            <text class="el m" x="170" y="106" text-anchor="middle">Stream()</text>
            <path class="edge" d="M262 136 L 262 187" :marker-end="arrow" />
            <path class="edge" d="M300 188 L 300 137" :marker-end="arrow" />
            <text class="el" x="254" y="166" text-anchor="end">stream disposed</text>
            <text class="el" x="254" y="180" text-anchor="end">or canceled</text>
            <text class="el m" x="308" y="166">Stream()</text>
            <text class="el" x="308" y="180">resumes</text>

            <path class="edge" d="M344 108 C 420 108, 430 35.5, 505 35.5" :marker-end="arrow" />
            <path class="edge" d="M344 115.5 L 505 115.5" :marker-end="arrow" />
            <path class="edge" d="M344 123 C 420 123, 430 195.5, 505 195.5" :marker-end="arrow" />
            <text class="el" x="456" y="50" text-anchor="middle">iterator ends</text>
            <text class="el" x="452" y="108" text-anchor="middle">exception</text>
            <text class="el" x="456" y="184" text-anchor="middle">canceled in setup</text>
            <text class="s" x="583.5" y="236" text-anchor="middle">terminal: cannot resume</text>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
