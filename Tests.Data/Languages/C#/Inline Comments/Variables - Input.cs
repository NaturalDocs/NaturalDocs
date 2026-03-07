
// Group: Full comments only
// ____________________________________________________________________________


/* Variable: FullCommentOnlyA
 * Full comment only A
 */
public int FullCommentOnlyA;

/** Full comment only B
 */
public int FullCommentOnlyB;

/// Full comment only C
public int FullCommentOnlyC;



// Group: Inline comments only
// ____________________________________________________________________________


public int InlineCommentOnlyA;  /// Inline comment only A

public int InlineCommentOnlyB;  /** Inline comment only B */

public int InlineCommentOnlyC; /// Inline comment only C
												// with multiple lines

public int InlineCommentOnlyD; /// Inline comment only D
												/// with multiple lines

public int InlineCommentOnlyE; /** Inline comment only E
													  with multiple lines */

public int InlineCommentOnlyF; /** Inline comment only F
												 *   with multiple lines
												 */


public int InlineCommentOnlyG = 10;  /// Inline comment only G

public int
	InlineCommentOnlyH; /// Inline comment only H

[Attribute]
public int InlineCommentOnlyI
	= 11;  /** Inline comment only I */



// Group: Both comments
// ____________________________________________________________________________


/* Variable: BothCommentsA
	Description from full comment.
*/
public int BothCommentsA;  /// Description from inline comment



// Group: In structs
// ____________________________________________________________________________


public struct StructA
	{
	public int InlineCommentA; /// Inline comment A
	public int InlineCommentB; /** Inline comment B */
	}


// Struct: StructB
public struct StructB
	{
	public int InlineCommentA; /// Inline comment A
	public int InlineCommentB; /** Inline comment B */
	}


/** Description of struct C */
public struct StructC
	{
	public int InlineCommentA; /// Inline comment A
	public int InlineCommentB; /** Inline comment B */
	}



// Group: Compound Declarations
// ____________________________________________________________________________


public int CompoundDeclarationA, CompoundDeclarationB; /// Description after both

public int CompoundDeclarationC, /// Description of C
			  CompoundDeclarationD; /// Description of D

public int CompoundDeclarationE = 12, /// Description of E
			  CompoundDeclarationF
					= 14; /// Description of F



// Group: Traps
// ____________________________________________________________________________


public int InnerCommentsA /// This comment should be ignored
	= 12;

public /** These comments */ int /** should be */ InnerCommentsB /** ignored */;

public /** Ignore */ int InnerCommentsC /** ignore */ = 12; /** Acceptable comment */
