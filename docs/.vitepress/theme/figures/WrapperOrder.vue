<script setup lang="ts">
import { useId } from "vue";
import ArrowMarkers from "./ArrowMarkers.vue";
import HlFigure from "./HlFigure.vue";

// Where one Substitute call places three wrappers that name the same evaluator.
const id = useId();
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-14 -14 708 304"
            role="img"
            aria-label="Nested wrappers around an evaluator. Observation is listed first and sits outermost, timing sits inside it, and counting is listed last and sits innermost, next to the evaluator."
        >
            <ArrowMarkers :id="id" />
            <rect class="layer0" x="0.5" y="0.5" width="400" height="230" rx="16" />
            <rect class="layer1" x="26.5" y="34.5" width="348" height="172" rx="14" />
            <rect class="layer2" x="52.5" y="68.5" width="296" height="114" rx="12" />
            <rect class="node primary" x="84.5" y="102.5" width="232" height="56" rx="10" />
            <text class="t" x="16" y="22">observe</text>
            <text class="s" x="80" y="22">listed first, outermost</text>
            <text class="t" x="42" y="56">time</text>
            <text class="s" x="80" y="56">listed second</text>
            <text class="t" x="68" y="90">count</text>
            <text class="s" x="112" y="90">listed last, innermost</text>
            <text class="t" x="200.5" y="127" text-anchor="middle">evaluator</text>
            <text class="s" x="200.5" y="145" text-anchor="middle">the configured operator</text>

            <path class="edge accent" d="M430 116 L 320 116" :marker-end="`url(#${id}-accent)`" />
            <path class="edge" d="M320 146 L 430 146" :marker-end="`url(#${id}-arrow)`" />
            <text class="el" x="440" y="120">call enters from outside</text>
            <text class="el" x="440" y="150">result returns outward</text>

            <text class="t" x="0" y="262">One Substitute call with</text>
            <text class="m" x="168" y="262">[observe, time, count]</text>
            <text class="t" x="0" y="282">builds</text>
            <text class="m" x="50" y="282">Observe(Time(Count(evaluator)))</text>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
