
/* Class: ClassName
*/
class ClassName
	{

	/* Topic: FunctionA
		This uses a documentation topic type instead of a code topic type.  It shouldn't merge.
	*/
	void FunctionA ()
		{
		}

	/* Group: FunctionB
		This uses a group topic type instead of a code topic type.  It shouldn't merge.
	*/
	void FunctionB ()
		{
		}

	/* File: FunctionC
		This uses a file topic type instead of a code topic type.  It shouldn't merge.
	*/
	void FunctionC ()
		{
		}

	/* Functions: FunctionD
		This uses a list topic type.  It shouldn't merge.
	*/
	void FunctionD ()
		{
		}

	/* Variable: FunctionE
		This uses a code topic type, so it should merge even though it's a different type than the code
		says it is.
	*/
	void FunctionE ()
		{
		}

	}