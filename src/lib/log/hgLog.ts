
export function buildParentData(hglog: Record<string, any>[]): Map<number, number[]> {
    if (!hglog || hglog.length <= 0) {
        return new Map()
    }

    const result = new Map<number, any>()
    const availCols = new Set<number>()
    let nextCol = 0
    function nextAvailableCol() {
        if (availCols.size) {
            const cols = Array.from(availCols)
            cols.sort()
            const col = cols[0]
            availCols.delete(col)
            return col
        } else {
            // col = nextCol++ would be too error-prone
            const col = nextCol
            nextCol += 1
            return col
        }
    }
    const cols = new Map<number, number>()

    for (const commit of hglog) {
        const { rev } = commit
        // Mercurial log returns empty parents array in the common case (one parent, and parent is previous revision)
        const parents: number[] = (commit.parents?.length === 0 && rev > 0) ? [rev - 1] : commit.parents
        let col: number
        if (cols.has(rev)) {
            col = cols.get(rev)
        } else {
            // First commit, or possibly new head not yet seen
            col = nextAvailableCol()
            cols.set(rev, col)
        }
        if (parents.length === 0) {
            // Either revision 0, or parent is off the bottom of the chart; either way, SVG should show no parent
            result.set(rev, [col, -1])
        } else if (parents.length === 1) {
            const parent = parents[0]
            if (cols.has(parent)) {
                const seenPcol = cols.get(parent)
                if (seenPcol > col) {
                    // Parent had multiple children, and an earlier child was further right. Parent should be as far left as possible
                    cols.set(parent, col)
                    // Parent's "old" column is now available for reuse in the graph
                    availCols.add(seenPcol)
                } else if (col > seenPcol) {
                    // Parent had multiple children, and an earlier child was further left. This column is now available for reuse
                    availCols.add(col)
                } // else do nothing
            } else {
                cols.set(parent, col)
            }
            result.set(rev, [col, parent])
        } else {
            // Mercurial never has more than two parents
            const parent0 = parents[0]
            const parent1 = parents[1]
            if (cols.has(parent0) && cols.has(parent1)) {
                const pcol0 = cols.get(parent0)
                const pcol1 = cols.get(parent1)
                if (pcol0 !== col && pcol1 !== col) {
                    // This column is ending and is available for reuse
                    availCols.add(col)
                }
            } else if (cols.has(parent0)) {
                // Unseen parent gets our column
                cols.set(parent1, col)
            } else if (cols.has(parent1)) {
                // Unseen parent gets our column
                cols.set(parent0, col)
            } else {
                // Must put one parent in new column. Reuse a previous column if possible so the graph doesn't get too wide
                cols.set(parent0, col)
                cols.set(parent1, nextAvailableCol())
            }
            result.set(rev, [col, parent0, parent1])
        }
    }

    return result
}
