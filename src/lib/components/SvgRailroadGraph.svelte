<script lang="ts">
    import { columnColor } from './columnColor'

    export let parentData: Map<number, number[]>
    export let tableBody: HTMLTableSectionElement
    export let columnWidth = 20
    $: halfColumnWidth = columnWidth / 2
    $: linkRadius = halfColumnWidth
    // TODO: Add <a href="..."> links to individual commits (once we're ready to render individual commit diffs)
    export let dotRadius = 3
    $: trackWidth = dotRadius / 2
    // TODO: Allow configurable colors

    let svgHeight = 0
    let svgWidth = 0
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
            let col, p0rev = -1, p1rev = -1
            if (maxRev === undefined) {
                // Parent data has highest rev first
                maxRev = rev
            }
            if (colData.length === 3) {
                [col, p0rev, p1rev] = colData
            } else {
                [col, p0rev] = colData
            }
            const row = maxRev - rev
            const rowElem = tableBody.rows[row]
            const cx = col * columnWidth + halfColumnWidth
            const cy = rowElem.offsetTop - firstRowHeight + (rowElem.clientHeight / 2)
            centersByRow[row] = [cx,cy]
            colorsByRow[row] = columnColor(col)
            const parents = []
            if (p0rev >= 0 && parentData.has(p0rev)) {
                const p0col = parentData.get(p0rev)[0]
                const p0row = maxRev - p0rev
                const p0RowElem = tableBody.rows[p0row]
                const p0x = p0col * columnWidth + halfColumnWidth
                const p0y = p0RowElem.offsetTop - firstRowHeight + (p0RowElem.clientHeight / 2)
                parents.push([p0x,p0y])
            }
            if (p1rev >= 0 && parentData.has(p1rev)) {
                const p1col = parentData.get(p1rev)[0]
                const p1row = maxRev - p1rev
                const p1RowElem = tableBody.rows[p1row]
                const p1x = p1col * columnWidth + halfColumnWidth
                const p1y = p1RowElem.offsetTop - firstRowHeight + (p1RowElem.clientHeight / 2)
                parents.push([p1x,p1y])
            }
            parentsByRow[row] = parents
            maxCol = maxCol < col ? col : maxCol
        }
        svgHeight = tableBody.clientHeight
        svgWidth = (maxCol + 1) * columnWidth
    }

    function curve([cx,cy],[px,py]) {
        const hx = (cx + px) / 2, hy = (cy + py) / 2
        return `M ${cx} ${cy} C ${cx} ${cy}, ${cx} ${hy}, ${hx} ${hy} C ${hx} ${hy}, ${px} ${hy}, ${px} ${py}`
        // Could also try:
        // return `M ${cx} ${cy} S ${cx} ${hy}, ${hx} ${hy} S ${px} ${hy}, ${px} ${py}`
        // Although I find I prefer the version with C in it as the S version isn't quite equivalent
    }

    $: { calculateCenters(parentData, tableBody) }
    // TODO: Also bind to clientHeight and recalculate when it changes... but debounce that so that changes of a single pixel in devtools don't go into an infinite re-render loop
</script>

{#if tableBody?.rows}
<svg height={svgHeight} width={svgWidth}>
{#each tableBody.rows as row, i}
    <circle cx={centersByRow[i][0]} cy={centersByRow[i][1]} r={dotRadius} fill={colorsByRow[i]} stroke="none"></circle>
    {#each parentsByRow[i] as [px,py]}
    <path d={curve(centersByRow[i], [px,py])} fill="none" stroke={colorsByRow[i]} stroke-width={trackWidth}></path>
    {/each}
{/each}
</svg>
{/if}
