{
  var a = new int[100][];
  for (int i = 0; i < 100; i++)
  {
    a[i] = new int[5];
  }
  return a[0].Length + a[99].Length;
}
