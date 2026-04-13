// Known limitation: `ref` locals and ref assignment are not supported by the parser.
// §9.5: ref local must be initialized from an lvalue, not a literal
ref int r = ref 5;
return r;
