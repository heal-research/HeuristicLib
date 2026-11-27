<script setup lang="ts">
import { useId } from "vue";
import ArrowMarkers from "./ArrowMarkers.vue";
import HlFigure from "./HlFigure.vue";

// Which tool shows what a run does: three questions in order, and an analyzer when none of them applies.
const id = useId();
const arrow = `url(#${id}-arrow)`;

const width = 310;
const height = 58;
const pitch = 92;
const answerX = 380.5;
const steps = [
    {
        question: "Should it stop or limit the run?",
        detail: "a count or duration that budgets read",
        answer: "Inspection",
        code: "CountingCandidates, LimitedToCandidates",
    },
    {
        question: "Is it behavior, not analysis?",
        detail: "such as logging or bridging to a runtime",
        answer: "A wrapper",
        code: "placed with Substitute",
    },
    {
        question: "Do you need the search states?",
        detail: "every yielded state or only the final one",
        answer: "The run methods",
        code: "RunStreaming, RunToCompletionAsync",
    },
].map((step, index) => ({ ...step, y: 0.5 + index * pitch }));
const last = { y: 0.5 + steps.length * pitch, h: 70 };
</script>

<template>
    <HlFigure>
        <svg
            class="hl-dg"
            viewBox="-14 -14 718 378"
            role="img"
            aria-label="Choose a tool by asking three questions in order. If it should stop or limit the run, use inspection such as CountingCandidates or LimitedToCandidates. If it is behavior rather than analysis, use a wrapper placed with Substitute. If you need the search states, use RunStreaming or RunToCompletionAsync. Otherwise use an analyzer passed to CreateRun, or to experiment run creation for every trial."
        >
            <ArrowMarkers :id="id" />
            <g class="shadow">
                <rect
                    v-for="step in steps"
                    :key="step.question"
                    class="node"
                    x="0.5"
                    :y="step.y"
                    :width="width"
                    :height="height"
                    rx="10"
                />
                <rect
                    v-for="step in steps"
                    :key="step.answer"
                    class="node"
                    :x="answerX"
                    :y="step.y"
                    :width="width"
                    :height="height"
                    rx="10"
                />
            </g>
            <g v-for="step in steps" :key="`${step.question}-t`">
                <text class="t" x="14" :y="step.y + 24">{{ step.question }}</text>
                <text class="s" x="14" :y="step.y + 42">{{ step.detail }}</text>
                <text class="t" :x="answerX + 14" :y="step.y + 24">{{ step.answer }}</text>
                <text class="mm" :x="answerX + 14" :y="step.y + 42">{{ step.code }}</text>
                <path
                    class="edge"
                    :d="`M${width + 1} ${step.y + height / 2} L ${answerX - 2} ${step.y + height / 2}`"
                    :marker-end="arrow"
                />
                <text class="el" :x="(width + answerX) / 2" :y="step.y + height / 2 - 7" text-anchor="middle">yes</text>
                <path
                    class="edge"
                    :d="`M${width / 2} ${step.y + height} L ${width / 2} ${step.y + pitch - 2}`"
                    :marker-end="arrow"
                />
                <text class="el" :x="width / 2 + 10" :y="step.y + height + 21">no</text>
            </g>

            <rect class="node primary" x="0.5" :y="last.y" :width="width" :height="last.h" rx="10" />
            <text class="t" x="14" :y="last.y + 24">An analyzer</text>
            <text class="s" x="14" :y="last.y + 42">passed to CreateRun, or to experiment</text>
            <text class="s" x="14" :y="last.y + 57">run creation for every trial</text>
        </svg>
        <template #caption><slot /></template>
    </HlFigure>
</template>
