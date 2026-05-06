// Three-element named tuple through Select→Where→Select pipeline
var data = new[] { "alpha", "beta", "gamma", "delta" };
return data
    .Select(s => (word: s, len: s.Length, upper: s.ToUpper()))
    .Where(t => t.len > 4)
    .Select(t => t.upper)
    .First();
