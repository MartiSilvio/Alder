int original = 0x12345678;
byte[] bytes = BitConverter.GetBytes(original);
int restored = BitConverter.ToInt32(bytes, 0);
return restored == original;
