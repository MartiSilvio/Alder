// §12.8.9.2: static method invocation — Array.Sort
int[] arr = new int[] { 3, 1, 4, 1, 5 };
Array.Sort(arr);
return arr[0] == 1 && arr[4] == 5;
