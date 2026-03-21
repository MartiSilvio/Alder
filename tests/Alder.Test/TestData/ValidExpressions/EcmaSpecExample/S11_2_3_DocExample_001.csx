{
  int visitorCount = 3;
  switch (visitorCount)
  {
    case 1: return 12.0m;
    case 2: return 20.0m;
    case 3: return 27.0m;
    case 4: return 32.0m;
    case 0: return 0.0m;
    default: throw new ArgumentException("unexpected");
  }
}
