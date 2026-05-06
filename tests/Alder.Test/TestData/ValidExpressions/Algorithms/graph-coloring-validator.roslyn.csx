var nodeCount = 7;

var adj = new List<int>[nodeCount];
for (var i = 0; i < nodeCount; i++)
    adj[i] = new List<int>();

var edgeSrc = new[] { 0, 0, 1, 1, 2, 3, 3, 4, 5 };
var edgeDst = new[] { 1, 2, 2, 3, 4, 4, 5, 6, 6 };
var edgeCount = edgeSrc.Length;

for (var e = 0; e < edgeCount; e++)
{
    adj[edgeSrc[e]].Add(edgeDst[e]);
    adj[edgeDst[e]].Add(edgeSrc[e]);
}

var colors = new int[nodeCount];
for (var i = 0; i < nodeCount; i++)
    colors[i] = -1;

var maxColor = 0;
for (var node = 0; node < nodeCount; node++)
{
    var usedColors = new List<int>();
    foreach (var neighbor in adj[node])
    {
        if (colors[neighbor] != -1)
            usedColors.Add(colors[neighbor]);
    }

    var color = 0;
    while (usedColors.Contains(color))
        color++;

    colors[node] = color;
    if (color > maxColor) maxColor = color;
}

var chromaticNumber = maxColor + 1;

var isValid = true;
var conflictCount = 0;
for (var e = 0; e < edgeCount; e++)
{
    if (colors[edgeSrc[e]] == colors[edgeDst[e]])
    {
        isValid = false;
        conflictCount++;
    }
}

var assignment = "";
for (var i = 0; i < nodeCount; i++)
{
    if (assignment.Length > 0) assignment += ",";
    assignment += colors[i].ToString();
}

var uncolored = 0;
for (var i = 0; i < nodeCount; i++)
{
    if (!new[] { 0, 1, 2, 3, 4, 5, 6 }.Contains(colors[i])) uncolored++;
}

var result = $"valid={isValid}|chromatic={chromaticNumber}|colors={assignment}|";
result += $"nodes={nodeCount}|edges={edgeCount}|conflicts={conflictCount}|uncolored={uncolored}";

return result;
