<script setup lang="ts">
// Arrowheads for one diagram. Marker ids are global in a page, so each diagram passes its own prefix.
defineProps<{ id: string }>();
</script>

<template>
    <defs>
        <marker
            :id="`${id}-arrow`"
            viewBox="0 0 10 10"
            refX="8.5"
            refY="5"
            markerWidth="8"
            markerHeight="8"
            orient="auto-start-reverse"
            markerUnits="userSpaceOnUse"
        >
            <path class="arrowhead" d="M2 1.5 L8.5 5 L2 8.5" />
        </marker>
        <marker
            :id="`${id}-accent`"
            viewBox="0 0 10 10"
            refX="8.5"
            refY="5"
            markerWidth="8"
            markerHeight="8"
            orient="auto-start-reverse"
            markerUnits="userSpaceOnUse"
        >
            <path class="arrowhead accent" d="M2 1.5 L8.5 5 L2 8.5" />
        </marker>
    </defs>
</template>
