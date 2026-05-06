// C# 12 collection expression with spread element
int[] other = [3, 4];
int[] a = [1, 2, ..other, 5];
return a[0] + a[1] + a[2] + a[3] + a[4];
