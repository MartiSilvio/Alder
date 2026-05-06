{ object x = -3; return x switch { int n when n > 0 => "positive", int n when n < 0 => "negative", _ => "zero" }; }
