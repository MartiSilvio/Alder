int[] arr = new[] { 1, 2, 3, 4 };
int sum = 0;
Array.ForEach(arr, x => sum += x);
return sum;
