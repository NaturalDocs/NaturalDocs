
/** 
 * Text without p tags.
 * @return x
 */
void Test1 () { }

/** 
 * <p>Paragraph 1</p>
 * <p>Paragraph 2</p>
 * @return x
 */
void Test2 () { }

/** 
 * Text before paragraph.
 * <p>Paragraph.</p>
 * Text after paragraph.
 * @return x
 */
void Test3 () { }

/** 
 * Text before p tag.
 * <p>
 * Text after unclosed p tag.
 * <p>
 * Text after another unclosed p tag.
 *
 * @return x
 */
void Test4 () { }

/** 
 * Text before p tag.
 * <p/>
 * Text after standalone p tag.
 * <p />
 * Text after another standalone p tag.
 *
 * @return x
 */
void Test5 () { }
