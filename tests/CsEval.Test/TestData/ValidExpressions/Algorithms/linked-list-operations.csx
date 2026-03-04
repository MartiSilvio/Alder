// Simulate a linked list using parallel arrays
// values[i] holds the node value, nexts[i] holds the index of next node (-1 = end)
var values = new int[10];
var nexts = new int[10];
var nodeCount = 0;
var head = -1;

// Insert nodes at the front: 10, 20, 30, 40, 50
// After all insertions, list order is: 50 -> 40 -> 30 -> 20 -> 10
var valuesToInsert = new[] { 10, 20, 30, 40, 50 };
for (var i = 0; i < valuesToInsert.Length; i++)
{
    var idx = nodeCount;
    values[idx] = valuesToInsert[i];
    nexts[idx] = head;
    head = idx;
    nodeCount = nodeCount + 1;
}

// Traverse and count nodes
var count = 0;
var current = head;
while (current != -1)
{
    count = count + 1;
    current = nexts[current];
}

// Remove node with value 30 (find and unlink it)
var prev = -1;
current = head;
while (current != -1)
{
    if (values[current] == 30)
    {
        if (prev == -1)
            head = nexts[current];
        else
            nexts[prev] = nexts[current];
        break;
    }
    prev = current;
    current = nexts[current];
}

// Traverse again to build result string
var result = "";
current = head;
var afterRemoveCount = 0;
while (current != -1)
{
    if (afterRemoveCount > 0)
        result = result + "->";
    result = result + values[current].ToString();
    afterRemoveCount = afterRemoveCount + 1;
    current = nexts[current];
}

return "before:" + count + ",after:" + afterRemoveCount + ",list:" + result;
