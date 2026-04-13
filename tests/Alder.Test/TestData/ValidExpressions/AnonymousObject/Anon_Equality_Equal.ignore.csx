// Known limitation: Alder's anonymous type is backed by an IDictionary<string,object?> rather
// than a generated type, so value-equality via .Equals returns reference equality instead of
// comparing property values member-wise.
// §12.8.16.7: two anonymous objects with equal members compare equal via Equals
var a = new { X = 1, Y = 2 };
var b = new { X = 1, Y = 2 };
return a.Equals(b);
