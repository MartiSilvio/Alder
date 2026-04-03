var x = await Task.FromResult((object)3.14);
return x is double d ? d * 2 : 0.0;
