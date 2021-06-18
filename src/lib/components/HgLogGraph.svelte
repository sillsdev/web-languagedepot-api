<script lang="ts">
    import SvgRailroadGraph from './SvgRailroadGraph.svelte'

    export let hglog: Record<string, any>[]
    export let tableBody: HTMLTableSectionElement

    // TODO: Define return type
    function buildParentData(hglog: Record<string, any>[]) : any {
        if (!hglog || hglog.length <= 0) {
            return []
        }

        const result = new Map<number, any>()
        // const availCols = new Set<number>()  // TODO: Implement and verify that graph still looks clean
        const cols = new Map<number, number>()
        let nextCol = 0

        for (const commit of hglog) {
            // console.log(commit)
            const { rev } = commit
            // Mercurial log returns empty parents array in the common case (one parent, and parent is previous revision)
            const parents: number[] = (commit.parents?.length === 0 && rev > 0) ? [rev - 1] : commit.parents
            let col: number
            if (cols.has(rev)) {
                col = cols.get(rev)
            } else {
                // First commit, or possibly new head not yet seen
                // col = nextCol++ would be too error-prone
                col = nextCol
                nextCol += 1
                cols.set(rev, col)
            }
            if (parents.length === 0) {
                // Either revision 0, or parent is off the bottom of the chart; either way, SVG should show no parent
                result.set(rev, [col, -1, col])
            } else if (parents.length === 1) {
                const parent = parents[0]
                let pcol
                if (cols.has(parent)) {
                    const seenPcol = cols.get(parent)
                    if (seenPcol > col) {
                        // Parent had multiple children, and an earlier child was further right. Parent should be as far left as possible
                        pcol = col
                        cols.set(parent, pcol)
                        // If this was the rightmost column, it's safe to shift left again
                        if (seenPcol === nextCol - 1) {
                            nextCol = seenPcol
                            // TODO: A cleverer algorithm could keep track of which columns are completed individually, with a Set<number, boolean>, and then mark completed columns for reuse
                        }
                    } else {
                        pcol = seenPcol
                    }
                } else {
                    pcol = col
                    cols.set(parent, pcol)
                }
                result.set(rev, [col, parent, pcol])
            } else {
                // Mercurial never has more than two parents
                const parent0 = parents[0]
                const parent1 = parents[1]
                let pcol0: number
                let pcol1: number
                if (cols.has(parent0) && cols.has(parent1)) {
                    pcol0 = cols.get(parent0)
                    pcol1 = cols.get(parent1)
                } else if (cols.has(parent0)) {
                    // Unseen parent gets our column
                    pcol0 = cols.get(parent0)
                    pcol1 = col
                    cols.set(parent1, pcol1)
                } else if (cols.has(parent1)) {
                    // Unseen parent gets our column
                    pcol0 = col
                    pcol1 = cols.get(parent1)
                    cols.set(parent0, pcol0)
                } else {
                    // Must put one parent in new column
                    // TODO: Once we have a Set of unused columns, pick smallest one from the set and remove it, and only increment nextCol if set is empty
                    pcol0 = col
                    pcol1 = nextCol
                    nextCol += 1
                    cols.set(parent0, pcol0)
                    cols.set(parent1, pcol1)
                }
                result.set(rev, [col, parent0, pcol0, parent1, pcol1])
            }
        }

        return result
    }

    $: parentData = buildParentData(hglog)
    $: console.log(parentData)
</script>

<SvgRailroadGraph {parentData} {tableBody} />
