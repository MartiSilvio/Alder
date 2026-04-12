// §12.8.19: unchecked negation of int.MinValue wraps to itself
return unchecked(-int.MinValue) == int.MinValue;
