
class Class
	{
	/* Functions: ListOfFunctions_InOrder
		FunctionA - Description
		FunctionB - Description
		FunctionC - Description
	*/
	void FunctionA ()
		{
		}

	void FunctionB ()
		{
		}

	void FunctionC ()
		{
		}

	/* Functions: ListOfFunctions_OutOfOrder
		FunctionD - Description
		FunctionE - Description
		FunctionF - Description
		FunctionG - Description
	*/
	void FunctionF ()
		{
		}

	void FunctionD ()
		{
		}

	void FunctionG ()
		{
		}

	void FunctionE ()
		{
		}

	/* Functions: ListOfFunctions_NotAllMatches
		FunctionH - Description
		FunctionI - Not in code
		FunctionK - Description
	*/
	void FunctionH ()
		{
		}

	void FunctionJ ()
		{
		}

	void FunctionK ()
		{
		}
	}


/* Classes: ListOfClasses
	If you're documenting something with child elements, like classes, in a list topic, its members
	should not be documented, even if manually included.

	ClassA - Description
	ClassB - Description
	ClassD - Description
*/
class ClassA
	{
	int variableA;
	int variableB;
	}

class ClassB
	{
	int variableA;
	int variableB;
	}

class ClassC_NotInList
	{
	int variableA;
	int variableB;
	}

class ClassD
	{
	// Topic: Manual Member A

	// var: variableA
	int variableA;

	// Topic: Manual Member B

	/** Headerless Comment
	*/
	int variableB;

	// Topic: Manual Member C
	}
