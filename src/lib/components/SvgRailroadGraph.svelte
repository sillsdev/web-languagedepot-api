<script lang="ts">
    import { columnColor } from './columnColor'

    export let parentData: Map<number, number[]>
    export let tableBody: HTMLTableSectionElement
    export let columnWidth = 20
    $: halfColumnWidth = columnWidth / 2
    $: linkRadius = halfColumnWidth
    export let dotRadius = 3
    $: trackWidth = dotRadius / 2
    // TODO: Allow configurable colors

    let svgElem: SVGSVGElement

    let ready = false
    let svgHeight = 0
    let svgWidth = 0
    let totalRowHeights = []
    let centersByRow = new Map<number, [number, number]>()
    let parentsByRow = new Map<number, [number, number][]>()
    let colorsByRow = new Map<number, string>()

    function calculateCenters(parentData: Map<number, number[]>, tableBody: HTMLTableSectionElement) {
        if (!tableBody) {
            // Component is created before the parent table element can finish mounting, so this will be called at least once with tableBody being null
            return
        }
        let maxRev = undefined
        let maxCol = 0
        // Guess header height. TODO: Consider passing in tableElem instead, or at least pass in header height
        let firstRowHeight = tableBody.rows?.length > 0 ? tableBody.rows[0].clientHeight : 0
        for (const [rev, colData] of parentData.entries()) {
            let col, p0rev, p0col, p1rev, p1col, parentCount
            if (maxRev === undefined) {
                // Parent data has highest rev first
                maxRev = rev
            }
            if (colData.length === 5) {
                [col, p0rev, p0col, p1rev, p1col] = colData
                parentCount = 2
                // Deal with having either (or both) parents off the chart
                if (p1rev === -1) {
                    // Second parent off the chart
                    parentCount -= 1
                }
                if (p0rev === -1) {
                    p0rev = p1rev  // If this is also -1, that's okay because parentCount will become 0
                    p0col = p1col
                    parentCount -= 1
                }
            } else {
                [col, p0rev, p0col] = colData
                parentCount = 1
                if (p0rev === -1) {
                    p0rev = p1rev  // If this is also -1, that's okay because parentCount will become 0
                    p0col = p1col
                    parentCount -= 1
                }
            }
            const row = maxRev - rev
            const rowElem = tableBody.rows[row]
            const cx = col * columnWidth + halfColumnWidth
            const cy = rowElem.offsetTop - firstRowHeight + (rowElem.clientHeight / 2)
            centersByRow[row] = [cx,cy]
            colorsByRow[row] = columnColor(col)
            if (parentCount > 0) {
                const p0row = maxRev - p0rev
                const p0RowElem = tableBody.rows[p0row]
                const p0x = p0col * columnWidth + halfColumnWidth
                const p0y = p0RowElem.offsetTop - firstRowHeight + (p0RowElem.clientHeight / 2)
                if (parentCount > 1) {
                    const p1row = maxRev - p1rev
                    const p1RowElem = tableBody.rows[p1row]
                    const p1x = p1col * columnWidth + halfColumnWidth
                    const p1y = p1RowElem.offsetTop - firstRowHeight + (p1RowElem.clientHeight / 2)
                    parentsByRow[row] = [[p0x,p0y],[p1x,p1y]]
                } else {
                    parentsByRow[row] = [[p0x,p0y]]
                }
            } else {
                parentsByRow[row] = []
            }
            maxCol = maxCol < col ? col : maxCol
        }
        svgHeight = tableBody.clientHeight
        svgWidth = (maxCol + 1) * columnWidth
        ready = true
    }

    function curve([cx,cy],[px,py]) {
        const hx = (cx + px) / 2, hy = (cy + py) / 2
        return `M ${cx} ${cy} C ${cx} ${cy}, ${cx} ${hy}, ${hx} ${hy} C ${hx} ${hy}, ${px} ${hy}, ${px} ${py}`
        // Could also try:
        // return `M ${cx} ${cy} S ${cx} ${hy}, ${hx} ${hy} S ${px} ${hy}, ${px} ${py}`
        // Although I find I prefer the version with C in it as the S version isn't quite equivalent
    }

    $: { calculateCenters(parentData, tableBody); if (ready) { console.log('Parent data:', parentData); console.log('parentsByRow:', parentsByRow) } }
    // TODO: Also bind to clientHeight and recalculate when it changes

</script>

{#if ready}
<svg bind:this={svgElem} height={svgHeight} width={svgWidth}>
{#each tableBody.rows as row, i}
    <circle cx={centersByRow[i][0]} cy={centersByRow[i][1]} r={dotRadius} fill={colorsByRow[i]} stroke="none"></circle>
    {#each parentsByRow[i] as [px,py]}
    <path d={curve(centersByRow[i], [px,py])} fill="none" stroke={colorsByRow[i]} stroke-width={trackWidth}></path>
    {/each}
{/each}
</svg>
{/if}
