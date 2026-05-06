{ object x = 5; return x switch { int n when n > 0 => "positive", int n when n < 0 => "negative", _ => "zero" }; }
