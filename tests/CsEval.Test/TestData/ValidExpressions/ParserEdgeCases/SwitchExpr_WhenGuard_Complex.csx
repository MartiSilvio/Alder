{ object x = 42; return x switch { int n when n > 0 => "pos", int n => "non-pos", _ => "other" }; }
