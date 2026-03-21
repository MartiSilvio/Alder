{
  int i = 0;
  int Next() { i = i + 1; return i; }
  return Next() * 10 + Next();
}
