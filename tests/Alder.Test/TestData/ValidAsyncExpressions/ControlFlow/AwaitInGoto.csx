var x = 0;
x = await Task.FromResult(1);
goto done;
x = 99;
done:
return x;
