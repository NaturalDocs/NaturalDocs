
/** 
 * @see "Plain Text"
 */
void Test1 () { }

/** 
 * @see <a href="http://www.website.com">HTML Link</a>
 */
void Test2 () { }

/** 
 * @see Class.Class#MemberWithoutDescription
 */
void Test3 () { }

/**
 * @see Class.Class#Member With Description
 */
void Test4 () { }

/** 
 * @see ClassOnly
 */
void Test5 () { }

/**
 * @see ClassOnly With Description
 */
void Test6 () { }

/** 
 * @see #MemberOnly
 */
void Test7 () { }

/**
 * @see #MemberOnly With Description
 */
void Test8 () { }

/** 
 * @see #FunctionWithSpaces(x, y) Spaces in parentheses shouldn't start the description
 */
void Test9 () { }

/**
 * @see #FunctionWithSpacesNoDescription(x, y)
 */
void Test10 () { }

/** 
 * @see TemplateWithSpaces<x, y> Javadoc doesn't support this but we will
 */
void Test11 () { }

/**
 * @see TemplateWithSpacesNoDescription<x, y>
 */
void Test12 () { }

/** 
 * @see operator< However that means we can't get tripped up by this
 */
void Test13 () { }

/**
 * @see operator< <b>or this</b>
 */
void Test14 () { }

/**
 * @see "Plain text"
 * @see <a href="http://www.website.com">HTML link</a>
 * @see Class.Class.Member Description
 */
void Test15 () { }
