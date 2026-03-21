// 0/1 Knapsack problem using dynamic programming with item reconstruction
// Exercises: 2D DP via flat array, new int[n], ternary operators, backtracking

var numItems = 12;
var capacity = 50;

var weights = new[] { 10, 20, 30, 5, 15, 25, 8, 12, 18, 7, 22, 3 };
var values  = new[] { 60, 100, 120, 30, 90, 150, 48, 72, 108, 42, 130, 20 };

// DP table: (numItems + 1) x (capacity + 1)
var dp = new int[((numItems + 1) * (capacity + 1))];

// Fill DP table
for (var i = 1; i <= numItems; i++)
{
    var w = weights[i - 1];
    var v = values[i - 1];
    for (var c = 0; c <= capacity; c++)
    {
        var without = dp[(i - 1) * (capacity + 1) + c];
        var with_ = c >= w ? dp[(i - 1) * (capacity + 1) + (c - w)] + v : 0;
        dp[i * (capacity + 1) + c] = with_ > without ? with_ : without;
    }
}

var maxValue = dp[numItems * (capacity + 1) + capacity];

// Backtrack to find selected items
var selected = new bool[numItems];
var remainCap = capacity;
var selectedCount = 0;
var totalWeight = 0;

for (var i = numItems; i > 0; i--)
{
    if (dp[i * (capacity + 1) + remainCap] != dp[(i - 1) * (capacity + 1) + remainCap])
    {
        selected[i - 1] = true;
        selectedCount++;
        totalWeight = totalWeight + weights[i - 1];
        remainCap = remainCap - weights[i - 1];
    }
}

// Build selected items string
var selectedStr = "";
for (var i = 0; i < numItems; i++)
{
    if (selected[i])
    {
        if (selectedStr.Length > 0) selectedStr += ",";
        selectedStr += i.ToString();
    }
}

// Verify
var verifyValue = 0;
for (var i = 0; i < numItems; i++)
{
    if (selected[i]) verifyValue = verifyValue + values[i];
}
var verified = verifyValue == maxValue;

var utilization = totalWeight * 100 / capacity;

var result = $"maxValue={maxValue}|items={selectedCount}|weight={totalWeight}/{capacity}|";
result += $"util={utilization}%|verified={verified}|selected=[{selectedStr}]";

return result;
