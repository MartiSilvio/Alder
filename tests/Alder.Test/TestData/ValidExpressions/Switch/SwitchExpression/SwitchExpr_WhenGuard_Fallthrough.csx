{ object x = 50; return x switch { int n when n > 100 => "big", int n => "small", _ => "not int" }; }
