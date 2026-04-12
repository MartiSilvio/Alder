// §13.9.5: iteration variable is readonly and cannot be assigned
foreach (int x in new int[] { 1, 2, 3 })
    x = 0;
return 0;
