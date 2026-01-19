
enum NoComments
	{
	A,
	B,
	C,
	}

enum NoCommentsWithValues
	{
	A = 1,
	B = 2,
	C = 3,
	}

enum InlineCommentAfterComma
	{
	A,
	B,
	C, /// Inline description of C
	}

enum InlineCommentAfterCommaWithValues
	{
	A = 1,
	B = 2,
	C = 3, /// Inline description of C
	}

enum InlineCommentAfterCommaAndBrace
	{
	A,
	B,
	C, } /// Inline description of C

enum InlineCommentAfterCommaAndBraceWithValues
	{
	A = 1,
	B = 2,
	C = 3, } /// Inline description of C
