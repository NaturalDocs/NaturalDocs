
/* Enum: CommentStylesA
 */
enum CommentStylesA
	{
	A, // Standard comment, should not be included
	B, /// Documentation comment, should be included
	C  /** Documentation comment, should be included */
	}

/* Enum: CommentStylesB
 */
enum CommentStylesB
	{
	A, // Multi-line standard comment,
		// should not be included
	B, /// Multi-line documentation comment,
		// should be included
	C, /// Multi-line documentation comment,
		/// should be included
	D  /** Multi-line documentation comment,
			  should be included */
	}

