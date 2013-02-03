
namespace NamespaceA.NamespaceB
	{

	/* Class: ClassNameOnly
	*/
	class ClassNameOnly
		{
		}

	/* Class: NamespaceA.NamespaceB.FullName
	*/
	class FullName
		{

		/* Function: FunctionName
		*/
		void FunctionName ()
			{
			}

		/* Function: FunctionNameWithParentheses (2)
		*/
		void FunctionNameWithParentheses ()
			{
			}

		/* Function: FunctionNameWithParentheses (int, float)
			Contents of parentheses are ignored in title matching.  It doesn't matter that the parameters don't match.
		*/
		void FunctionNameWithParentheses (string x, double y)
			{
			}

		/* Function: Mismatch_Comment
			Since the names don't match this should result in two independent topics.
		*/
		void Mismatch_Code ()
			{
			}

		}

	}