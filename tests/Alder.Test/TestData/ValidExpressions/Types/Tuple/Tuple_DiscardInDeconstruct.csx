// §12.21.2: discard `_` in deconstruction ignores first element
var (_, b) = (99, 42);
return b;
