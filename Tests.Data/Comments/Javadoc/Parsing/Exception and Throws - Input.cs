
/** 
 * @exception x Description
 */
void Test1 () { }

/** 
 * @exception x Description
 * @exception y Description
 */
void Test2 () { }

/** 
 * @exception x Description with <i>formatting</i>.
 * @exception y Description with paragraphs.
 *   <p>
 *   and spanning multiple lines.
 */
void Test3 () { }

/** 
 * @throws x Description
 */
void Test4 () { }

/** 
 * @throws x Description
 * @throws y Description
 */
void Test5 () { }

/** 
 * @throws x Description with <i>formatting</i>.
 * @throws y Description with paragraphs.
 *   <p>
 *   and spanning multiple lines.
 */
void Test6 () { }

/**
 * @exception x Exception and throws are kept separate even though they're synonyms in Javadoc
 * @throws y so that the documentation heading uses the keyword the user prefers.
 */
void Test7 () { }
