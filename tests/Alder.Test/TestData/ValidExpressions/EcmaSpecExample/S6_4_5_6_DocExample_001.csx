{
  string a = "Happy birthday, Joel";
  string b = @"Happy birthday, Joel";
  string c = "hello \t world";
  string d = @"hello \t world";
  string e = "Joe said \"Hello\" to me";
  string f = @"Joe said ""Hello"" to me";
  string g = "\\\\server\\share\\file.txt";
  string h = @"\\server\share\file.txt";
  string i = "one\r\ntwo\r\nthree";
  string j = @"one
three";
  return a.Length + b.Length + c.Length + d.Length + e.Length + f.Length + g.Length + h.Length + i.Length + j.Length;
}
