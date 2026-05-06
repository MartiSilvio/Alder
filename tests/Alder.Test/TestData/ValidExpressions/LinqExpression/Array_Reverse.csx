// §12.8.9.2: static method invocation — Array.Reverse
int[] arr = new int[] { 1, 2, 3 };
Array.Reverse(arr);
return arr[0] == 3 && arr[2] == 1;
