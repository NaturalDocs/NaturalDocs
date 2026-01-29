
enum NoComments
	{  A, B, C  }


/* Enum: NoValueCommentsA
 */
enum NoValueCommentsA
	{  A, B, C  }

/* Enum: NoValueCommentsB
 */
enum NoValueCommentsB
	{  A = 10, B = (5 + 6), C  }

/* Enum: NoValueCommentsC
 */
[Flags]
enum NoValueCommentsC : byte
	{  A = 0x01, B, C  }


enum NoParentCommentsA
	{
	A, /// Inline description of A
	B, /// Inline description of B
	C  /** Inline description of C */
	}

[Flags]
enum NoParentCommentsB : byte
	{
	A = 10, /// Inline description of A
	B = (5 + 6),
	C  /** Inline description of C */
	}
