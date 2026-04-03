var x = await Task.FromResult(3);
return x switch { 1 => "one", 2 => "two", 3 => "three", _ => "other" };
