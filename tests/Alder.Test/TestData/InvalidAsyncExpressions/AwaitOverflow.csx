var x = await Task.FromResult(int.MaxValue);
return checked(x + 1);
