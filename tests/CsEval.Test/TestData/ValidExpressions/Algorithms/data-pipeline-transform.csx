var data = (1..20).ToList();

Func<List<int>, List<int>> filterOdd = xs => {
    var r = new List<int>();
    foreach (var x in xs) { if (x % 2 != 0) r.Add(x); }
    return r;
};

Func<List<int>, List<int>> squareAll = xs => {
    var r = new List<int>();
    foreach (var x in xs) r.Add((int)(x ** 2));
    return r;
};

Func<List<int>, List<int>> sortDesc = xs => {
    var sorted = new List<int>(xs);
    sorted.Sort();
    sorted.Reverse();
    return sorted;
};

var transformed = data |> filterOdd |> squareAll |> sortDesc;

var sum = 0;
var count = transformed.Count;
var min = transformed[0];
var max = transformed[0];
foreach (var v in transformed)
{
    sum += v;
    if (v < min) min = v;
    if (v > max) max = v;
}

var items = "";
foreach (var v in transformed)
{
    if (items.Length > 0) items += ",";
    items += v.ToString();
}

return $"items={items}|count={count}|sum={sum}|min={min}|max={max}";
