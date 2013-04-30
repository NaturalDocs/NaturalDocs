
/**
 * Link to <a href="mailto:mail@address.com">an e-mail address</a>.
 * @return x
 */
void Test1 () { }

/**
 * Link to <a href="mailto:ma+il@some.address.com?subject=Test Subject" class="CSSClass">an e-mail address</a>
 * with extra properties in the link.
 * @return x
 */
void Test2 () { }

/**
 * Link to <a href="http://www.website.com">an URL</a>.
 * @return x
 */
void Test3 () { }

/**
 * Link to <a href="ftp://ftp.website.com">a FTP URL</a>.
 * @return x
 */
void Test4 () { }

/**
 * Link to <a href="https://www.website.com/folder/link.cgi?a=b&b=%1F#anchor" target=_top>a complicated URL</a>
 * with extra properties.
 * @return x
 */
void Test5 () { }

/**
 * Defining <a name="anchor">an anchor</a>.  Link should be ignored.
 * @return x
 */
void Test6 () { }

/**
 * Link to <a href="javascript:doSomething()">a JavaScript function</a> via URL.
 * Link should be ignored.
 * @return x
 */
void Test7 () { }

/**
 * Link to <a href="#" onClick="doSomething()">a JavaScript function</a> via onClick.
 * Link should be ignored.
 * @return x
 */
void Test8 () { }

/**
 * Link to <a href="index.html">a relative path</a>.  Since this is probably intended for a file in the
 * Javadoc output structure it would be invalid.  Link should be ignored.
 * @return x
 */
void Test9 () { }

/**
 * Link to <a href="{@docRoot}/index.html">a docRoot path</a>.  Since this is probably intended for
 * a file in the Javadoc output structure it would be invalid.  Link should be ignored.
 * @return x
 */
void Test10 () { }

/**
 * Link to <a href="http://www.website.com"><b>an URL with <i>tags</i> in the label</b></a>.
 * @return x
 */
void Test11 () { }

/**
 * Link to <a href="http://www.website.com">an URL with &gt; entities & in the &#169; label</a>.
 * @return x
 */
void Test12 () { }
