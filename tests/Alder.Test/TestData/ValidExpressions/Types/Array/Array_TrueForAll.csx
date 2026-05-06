int[] arr = new[] { 2, 4, 6, 8 };
return Array.TrueForAll(arr, x => x % 2 == 0);
