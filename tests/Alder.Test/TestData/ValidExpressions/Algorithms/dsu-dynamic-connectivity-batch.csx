// Dynamic connectivity with DSU over batched commands
// Exercises: switch with command opcodes, path compression, union by size, component counting

// Commands are [op, a, b]:
// op=1 => union(a,b)
// op=2 => connected(a,b)
var cmds = new[] {
    1,0,1,  1,1,2,  2,0,2,  2,0,3,  1,3,4,  1,2,4,
    2,0,4,  1,5,6,  2,0,6,  1,6,7,  1,4,7,  2,0,7
};

var n = 8;
var parent = new int[n];
var size = new int[n];
for (var i = 0; i < n; i++)
{
    parent[i] = i;
    size[i] = 1;
}

var queryResults = "";
var queryCount = 0;

for (var i = 0; i < cmds.Length / 3; i++)
{
    var op = cmds[i * 3];
    var a = cmds[i * 3 + 1];
    var b = cmds[i * 3 + 2];

    // Find(a) with compression
    var ra = a;
    while (parent[ra] != ra) ra = parent[ra];
    var x = a;
    while (parent[x] != x)
    {
        var nx = parent[x];
        parent[x] = ra;
        x = nx;
    }

    // Find(b) with compression
    var rb = b;
    while (parent[rb] != rb) rb = parent[rb];
    x = b;
    while (parent[x] != x)
    {
        var nx = parent[x];
        parent[x] = rb;
        x = nx;
    }

    switch (op)
    {
        case 1:
        {
            if (ra != rb)
            {
                if (size[ra] < size[rb])
                {
                    parent[ra] = rb;
                    size[rb] += size[ra];
                }
                else
                {
                    parent[rb] = ra;
                    size[ra] += size[rb];
                }
            }
            break;
        }

        case 2:
        {
            var conn = ra == rb;
            queryResults += conn ? "1" : "0";
            queryCount++;
            if (queryCount < 5) queryResults += ",";
            break;
        }

        default:
            throw new InvalidOperationException($"Unknown op {op}");
    }
}

// Count components.
var components = 0;
for (var i = 0; i < n; i++)
{
    var r = i;
    while (parent[r] != r) r = parent[r];
    if (r == i) components++;
}

// Largest component size.
var maxSize = 0;
for (var i = 0; i < n; i++)
{
    if (parent[i] == i && size[i] > maxSize) maxSize = size[i];
}

return $"queries={queryResults}|components={components}|maxSize={maxSize}";
