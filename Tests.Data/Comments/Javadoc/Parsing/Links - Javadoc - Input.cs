
/** 
 * {@link Class.Class#MemberWithoutDescription}
 */
void Test1 () { }

/**
 * {@link Class.Class#Member With Description}
 */
void Test2 () { }

/** 
 * {@link ClassOnly}
 */
void Test3 () { }

/**
 * {@link ClassOnly With Description}
 */
void Test4 () { }

/** 
 * {@link #MemberOnly}
 */
void Test5 () { }

/**
 * {@link #MemberOnly With Description}
 */
void Test6 () { }

/** 
 * {@link #FunctionWithSpaces(x, y) Spaces in parentheses shouldn't start the description}
 */
void Test7 () { }

/**
 * {@link #FunctionWithSpacesNoDescription(x, y)}
 */
void Test8 () { }

/** 
 * {@link TemplateWithSpaces<x, y> Javadoc doesn't support this but we will}
 */
void Test9 () { }

/**
 * {@link TemplateWithSpacesNoDescription<x, y>}
 */
void Test10 () { }

/** 
 * {@link operator< However that means we can't get tripped up by this}
 */
void Test11 () { }

/**
 * {@link operator< <b>or this</b>}
 */
void Test12 () { }
