
// Group: Full comments only
// ____________________________________________________________________________


/* Constant: FullCommentOnlyA
 * Full comment only A
 */
public const int FullCommentOnlyA = 1;

/** Full comment only B
 */
public const int FullCommentOnlyB = 2;

/// Full comment only C
public const int FullCommentOnlyC = 3;



// Group: Inline comments only
// ____________________________________________________________________________


public const int InlineCommentOnlyA = 1;  /// Inline comment only A

public const int InlineCommentOnlyB = 2;  /** Inline comment only B */

public const int InlineCommentOnlyC = 3; /// Inline comment only C
												// with multiple lines

public const int InlineCommentOnlyD = 4; /// Inline comment only D
												/// with multiple lines

public const int InlineCommentOnlyE = 5; /** Inline comment only E
													  with multiple lines */

public const int InlineCommentOnlyF = 6; /** Inline comment only F
												 *   with multiple lines
												 */

public const int
	InlineCommentOnlyG = 7; /// Inline comment only G

[Attribute]
public const int InlineCommentOnlyH
	= 8;  /** Inline comment only H */



// Group: Both comments
// ____________________________________________________________________________


/* Constant: BothCommentsA
	Description from full comment.
*/
public const int BothCommentsA = 1;  /// Description from inline comment



// Group: Compound Declarations
// ____________________________________________________________________________


public const int CompoundDeclarationA = 1, CompoundDeclarationB = 2; /// Description after both

public const int CompoundDeclarationC = 3, /// Description of C
			  CompoundDeclarationD = 4; /// Description of D

public const int CompoundDeclarationE = 5, /// Description of E
			  CompoundDeclarationF
					= 6; /// Description of F



// Group: Traps
// ____________________________________________________________________________


public const int InnerCommentsA /// This comment should be ignored
	= 1;

public const /** These comments */ int /** should be */ InnerCommentsB /** ignored */ = 2;

public const /** Ignore */ int InnerCommentsC /** ignore */ = 3; /** Acceptable comment */
