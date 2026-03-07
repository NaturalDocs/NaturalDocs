
// Group: Full comments only
// ____________________________________________________________________________


/* Event: FullCommentOnlyA
 * Full comment only A
 */
public event Delegate FullCommentOnlyA;

/** Full comment only B
 */
public event Delegate FullCommentOnlyB;

/// Full comment only C
public event Delegate FullCommentOnlyC;



// Group: Inline comments only
// ____________________________________________________________________________


public event Delegate InlineCommentOnlyA;  /// Inline comment only A

public event Delegate InlineCommentOnlyB;  /** Inline comment only B */

public event Delegate InlineCommentOnlyC; /// Inline comment only C
															 // with multiple lines

public event Delegate InlineCommentOnlyD; /// Inline comment only D
															 /// with multiple lines

public event Delegate InlineCommentOnlyE; /** Inline comment only E
																   with multiple lines */

public event Delegate InlineCommentOnlyF; /** Inline comment only F
														 	  *   with multiple lines
															  */

[Attribute]
public event Delegate
	InlineCommentOnlyG; /// Inline comment only G



// Group: Both comments
// ____________________________________________________________________________


/* Event: BothCommentsA
	Description from full comment.
*/
public event Delegate BothCommentsA;  /// Description from inline comment



// Group: Compound Declarations
// ____________________________________________________________________________


public event Delegate CompoundDeclarationA, CompoundDeclarationB; /// Description after both

public event Delegate CompoundDeclarationC, /// Description of C
							   CompoundDeclarationD; /// Description of D



// Group: Traps
// ____________________________________________________________________________


public event Delegate /// This comment should be ignored
	InnerCommentsA;

public event /** These comments */ Delegate /** should be */ InnerCommentsB /** ignored */;

public event /** Ignore */ Delegate InnerCommentsC /** ignore */; /** Acceptable comment */
