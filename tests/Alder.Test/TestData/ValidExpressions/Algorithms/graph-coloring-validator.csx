var nodeCount = 7;

var adj = new List<int>[nodeCount];
foreach (var i in 0..<nodeCount)
    adj[i] = new List<int>();

var edgeSrc = [0, 0, 1, 1, 2, 3, 3, 4, 5];
var edgeDst = [1, 2, 2, 3, 4, 4, 5, 6, 6];
var edgeCount = edgeSrc.Length;

foreach (var e in 0..<edgeCount)
{
    adj[edgeSrc[e]].Add(edgeDst[e]);
    adj[edgeDst[e]].Add(edgeSrc[e]);
}

var colors = new int[nodeCount];
foreach (var i in 0..<nodeCount)
    colors[i] = -1;

var maxColor = 0;
foreach (var node in 0..<nodeCount)
{
    var usedColors = new List<int>();
    foreach (var neighbor in adj[node])
    {
        if (colors[neighbor] != -1)
            usedColors.Add(colors[neighbor]);
    }

    var color = 0;
    while (color in usedColors)
        color++;

    colors[node] = color;
    if (color > maxColor) maxColor = color;
}

var chromaticNumber = maxColor + 1;

var isValid = true;
var conflictCount = 0;
foreach (var e in 0..<edgeCount)
{
    if (colors[edgeSrc[e]] == colors[edgeDst[e]])
    {
        isValid = false;
        conflictCount++;
    }
}

var assignment = "";
foreach (var i in 0..<nodeCount)
{
    if (assignment.Length > 0) assignment += ",";
    assignment += colors[i].ToString();
}

var uncolored = 0;
foreach (var i in 0..<nodeCount)
{
    if (!(0 <= colors[i] < nodeCount)) uncolored++;
}

var result = $"valid={isValid}|chromatic={chromaticNumber}|colors={assignment}|";
result += $"nodes={nodeCount}|edges={edgeCount}|conflicts={conflictCount}|uncolored={uncolored}";

return result;
