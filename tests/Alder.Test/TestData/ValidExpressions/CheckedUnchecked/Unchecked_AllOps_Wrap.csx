int max = int.MaxValue;
int min = int.MinValue;
int addWrap = unchecked(max + 1);
int subWrap = unchecked(min - 1);
int mulWrap = unchecked(max * 2);
int negWrap = unchecked(-min);
byte castWrap = unchecked((byte)256);
addWrap == int.MinValue && subWrap == int.MaxValue && negWrap == int.MinValue && castWrap == 0
