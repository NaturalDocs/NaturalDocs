
/** 
 * Serial fields are ignored.  However, comments that have them should still be recognized
 * as Javadoc, so *bold* and _underline_ should not work.  Also, blocks spanning multiple
 * lines should not have their content included.
 *
 * @serial Description
 * @serialField x Description
 *    spanning multiple lines
 * @serialData Description <i>with formatting</i>
 */
void Test1 () { }

/** 
 * @serial Description
 * @return Description after serial
 */
void Test2 () { }

/** 
 * @return Description before serial
 * @serial Description
 */

void Test3 () { }
/** 
 * @serial Description
 *   spanning multiple lines
 * @return Description after multiline serial
 */
void Test4 () { }

/** 
 * @return Description before multiline serial
 * @serial Description
 *    spanning multiple lines
 */
void Test5 () { }
