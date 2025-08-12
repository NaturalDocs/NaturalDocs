
/** 
 * {@linkPlain Class.Class#MemberWithoutDescription}
 */
void Test1 () { }

/**
 * {@linkPlain Class.Class#Member With Description}
 */
void Test2 () { }

/** 
 * {@linkPlain ClassOnly}
 */
void Test3 () { }

/**
 * {@linkPlain ClassOnly With Description}
 */
void Test4 () { }

/** 
 * {@linkPlain #MemberOnly}
 */
void Test5 () { }

/**
 * {@linkPlain #MemberOnly With Description}
 */
void Test6 () { }

/** 
 * {@linkPlain #FunctionWithSpaces(x, y) Spaces in parentheses shouldn't start the description}
 */
void Test7 () { }

/**
 * {@linkPlain #FunctionWithSpacesNoDescription(x, y)}
 */
void Test8 () { }

/** 
 * {@linkPlain TemplateWithSpaces<x, y> Javadoc doesn't support this but we will}
 */
void Test9 () { }

/**
 * {@linkPlain TemplateWithSpacesNoDescription<x, y>}
 */
void Test10 () { }

/** 
 * {@linkPlain operator< However that means we can't get tripped up by this}
 */
void Test11 () { }

/**
 * {@linkPlain operator< <b>or this</b>}
 */
void Test12 () { }
