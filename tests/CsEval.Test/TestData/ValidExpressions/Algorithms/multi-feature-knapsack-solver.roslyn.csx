var weights = new[] { 3, 4, 2, 5, 1, 6, 3, 2 };
var values = new[] { 4, 5, 3, 8, 2, 7, 4, 3 };
var capacity = 15;
var n = weights.Length;
var cols = capacity + 1;

var dp = new int[(n + 1) * cols];

for (var i = 1; i <= n; i++)
{
    for (var w = 0; w <= capacity; w++)
    {
        var prev = dp[(i - 1) * cols + w];
        dp[i * cols + w] = prev;
        var wi = weights[i - 1];
        if (wi <= w)
        {
            var withItem = dp[(i - 1) * cols + (w - wi)] + values[i - 1];
            if (withItem > prev)
                dp[i * cols + w] = withItem;
        }
    }
}

var maxValue = dp[n * cols + capacity];

var selected = new System.Collections.Generic.List<int>();
var remaining = capacity;
for (var i = n; i > 0; i--)
{
    if (dp[i * cols + remaining] != dp[(i - 1) * cols + remaining])
    {
        selected.Add(i - 1);
        remaining -= weights[i - 1];
    }
}
selected.Reverse();

var totalWeight = 0;
foreach (var idx in selected)
    totalWeight += weights[idx];

var valid = totalWeight >= 0 && totalWeight <= capacity;

var densities = new System.Collections.Generic.List<double>();
foreach (var idx in selected)
    densities.Add(Math.Pow(values[idx], 2) / (double)weights[idx]);

var totalDensity = 0.0;
foreach (var d in densities)
    totalDensity += d;
var avgDensity = Math.Round(totalDensity / selected.Count, 2);

var cmp = maxValue.CompareTo(20);
var rating = cmp > 0 ? "excellent" : (cmp == 0 ? "good" : "low");

var items = "";
foreach (var idx in selected)
{
    if (items.Length > 0) items += ",";
    items += idx.ToString();
}

var utilization = Math.Round((double)totalWeight / capacity * 100, 1);

var result = $"maxValue={maxValue}|items={items}|weight={totalWeight}|";
result += $"capacity={capacity}|util={utilization}%|";
result += $"rating={rating}|density={avgDensity}|valid={valid}";

return result;
