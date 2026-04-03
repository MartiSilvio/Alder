var data = new[] { 1, 2, 3 };
var results = data.Select(x => x * 2).ToArray();
var sum = await Task.FromResult(results.Sum());
return sum;
