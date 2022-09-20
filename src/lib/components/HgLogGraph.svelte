<script lang="ts">
    import SvgRailroadGraph from './SvgRailroadGraph.svelte'
    import { buildParentData } from '$lib/log/hgLog'

    export let hglog: Record<string, any>[]
    export let tableBody: HTMLTableSectionElement

    $: parentData = buildParentData(hglog)
    $: console.log(parentData)
</script>

<SvgRailroadGraph {parentData} {tableBody} />
