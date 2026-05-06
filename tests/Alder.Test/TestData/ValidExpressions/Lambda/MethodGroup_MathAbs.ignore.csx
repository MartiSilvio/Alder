// Known limitation: method group conversion to a delegate parameter is not yet supported —
// `Select(Math.Abs)` has to be written as `Select(x => Math.Abs(x))`.
new[] {-3, -1, 2, 4}.Select(Math.Abs).ToList()