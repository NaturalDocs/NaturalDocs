
/// <summary>An XML comment.  You can tell because *this* is not bold and _this_ 
/// is not underline.</summary>
void Test1 () { }

/** <summary>XML can use Javadoc comment styles.  Not *bold* not _underline_.</summary>
 */
void Test2 () { }

/**
 * <summary>XML can use Javadoc comment styles 2.  
 * Not *bold* not _underline_.</summary>
 */
void Test3 () { }

///
///
///          <summary>Lots of whitespace before the opening tag is tolerated.  Not *bold*
/// not _underline_.</summary>
void Test4 () { }

/// Not starting with a tag is not permitted though.  <summary>This should be parsed as Natural
/// Docs, meaning the tags show up literally.</summary>
void Test5 () { }

