var step = 0;
step = await Task.FromResult(1);
if (step == 1) goto phase2;
return "skipped";
phase2:
step = await Task.FromResult(2);
if (step == 2) goto phase3;
return "skipped";
phase3:
return await Task.FromResult(step + 10);
