<script context="module">
    export async function load({ page, fetch }) {
        const url = `/api/v2/projects/${page.params.code}/log`
        const result = await fetch(url)
        if (result.ok) {
            const log = await result.json()
            console.log(log)
            return { props: { log, code: page.params.code }}
        }
        // No return if result not OK, which will result in a 404
    }

    function stripFlexBridgeInfo(desc) {
        const re = /\[(FLEx Bridge:|LfMergeBridge:)[^\]]+\]/
        return desc.replace(re, '').trim()
    }
</script>

<script>
    import HgLogGraph from '$lib/components/HgLogGraph.svelte'

    let tableBody

    export let log
    export let code
</script>

<h2>Log for {code}</h2>

{#if log}
<table>
    <thead>
        <tr>
            <th><!-- Empty header: SVG goes here --></th>
            <th>#</th>
            <th><!-- Empty header: radio buttons for selecting diffs, column 1 --></th>
            <th><!-- Empty header: radio buttons for selecting diffs, column 2 --></th>
            <th>Date</th>
            <th>Author</th>
            <th>Comment</th>
        </tr>
    </thead>
    <tbody bind:this={tableBody}>
    {#each log as row, i (row.hash)}
    <tr class:top={ i === 0 } class:bottom={i === log.length - 1} class:odd={i % 2} class:even={i % 2 === 0}>
        {#if i === 0}
        <td id="log-graph" rowspan=0><HgLogGraph hglog={log} {tableBody} /></td>
        {/if}
        <td>{row.rev}:{row.shorthash}</td>
        <td>x</td> <!-- Placeholder for radio buttons (add them once we're ready to render individual commit diffs) -->
        <td>y</td> <!-- Placeholder for radio buttons second column -->
        <td>{row.date}</td>
        <td>{row.author}</td>
        <td>{stripFlexBridgeInfo(row.desc)}</td>
    </tr>
    {/each}
    </tbody>
</table>
{/if}

<style>
    table {
        border-collapse: collapse;
        border: 1px solid #ddd;
        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        font-size: 12px;
        padding: 2px;
    }
    th {
        background-color: #eee;
    }
    tr.even {
        background-color: #fff;
    }
    tr.odd {
        background-color: #f8f8f8;
    }
    td { padding-right: 10px; }
</style>
