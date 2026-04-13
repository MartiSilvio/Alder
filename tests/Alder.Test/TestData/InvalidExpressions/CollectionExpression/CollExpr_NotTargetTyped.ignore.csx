// Known limitation / deliberate Extended-mode deviation: Alder in Extended mode infers an array
// element type from a bare `[1, 2, 3]` literal even when the declaration uses `var`, so the
// CS9176 rejection is intentionally relaxed. Standard mode still throws.
// Collection expression has no target type — CS9176
var x = [1, 2, 3];
return x;
