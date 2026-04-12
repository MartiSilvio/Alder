// §12.9.3: unary minus — negating int.MinValue wraps in unchecked context
return unchecked(-int.MinValue) == int.MinValue;
