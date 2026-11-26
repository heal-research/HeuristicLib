<script setup lang="ts">
import { ref, useId } from "vue";

// A figure card: the body slot holds the picture, the caption slot its explanation. A figure with table data
// offers it as a table, so every chart can be read without seeing it.
const props = defineProps<{
    tableHeaders?: string[];
    tableRows?: (string | number)[][];
}>();

const open = ref(false);
const tableId = useId();
</script>

<template>
    <figure class="hl-fig">
        <div class="hl-fig__body">
            <slot />
        </div>
        <figcaption>
            <span class="hl-fig__num">Figure</span>
            <span class="hl-fig__cap"><slot name="caption" /></span>
            <button
                v-if="props.tableRows"
                class="hl-fig__data"
                type="button"
                :aria-expanded="open"
                :aria-controls="tableId"
                @click="open = !open"
            >
                {{ open ? "Hide data" : "Data" }}
            </button>
        </figcaption>
        <div v-if="props.tableRows" v-show="open" :id="tableId" class="hl-fig__table">
            <table>
                <thead>
                    <tr>
                        <th v-for="header in props.tableHeaders" :key="header">{{ header }}</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="(row, index) in props.tableRows" :key="index">
                        <td v-for="(cell, column) in row" :key="column">{{ cell }}</td>
                    </tr>
                </tbody>
            </table>
        </div>
    </figure>
</template>
