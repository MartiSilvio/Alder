// §20.5: Comparison<T> delegate instantiation used with Array.Sort
int[] arr = new[] { 3, 1, 4, 1, 5, 9, 2, 6 };
Comparison<int> cmp = (a, b) => a - b;
Array.Sort(arr, cmp);
return arr[0] + arr[arr.Length - 1];
