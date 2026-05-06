// §10.3.2 / §12.8.19: the unchecked result is unspecified when the source is out of range.
_ = unchecked((int)double.MaxValue);
return true;
