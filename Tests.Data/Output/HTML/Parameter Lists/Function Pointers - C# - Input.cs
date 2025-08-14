class TestClass
	{

	/* Function: FunctionA
		Parameters:
		x - Should have type
		delegate - Should not have type
		unmanaged - Should not have type
		Cdecl - Should not have type
	*/
	public void FunctionA (delegate* unmanaged[Cdecl] <int, float> x)
		{  }

	}