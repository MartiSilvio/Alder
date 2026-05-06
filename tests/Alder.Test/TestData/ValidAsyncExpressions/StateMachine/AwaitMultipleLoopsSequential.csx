var phase1 = 0;
for (var i = 0; i < 5; i++)
    phase1 += await Task.FromResult(i);

var phase2 = 1;
for (var i = 1; i <= 5; i++)
    phase2 *= await Task.FromResult(i);

var phase3 = new List<int>();
foreach (var x in new[] { phase1, phase2 })
    phase3.Add(await Task.FromResult(x));

return phase3[0] + phase3[1];
