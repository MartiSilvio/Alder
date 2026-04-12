// §10.2.3: no implicit conversion from char to short (char is 0..65535, short is -32768..32767)
char c = 'A';
short s = c;
return s;
