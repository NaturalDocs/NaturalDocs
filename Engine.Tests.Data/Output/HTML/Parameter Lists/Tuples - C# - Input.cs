class TestClass
	{

	/* Function: FunctionA
		Parameters:
		x - Should have type
	*/
	public (string, int) FunctionA ((string, int) x)
		{  }

	/* Function: FunctionB
		Parameters:
		x - Should have type with names
		a - Should not have type
		b - Should not have type
	*/
	public (string a, int b) FunctionB ((string a, int b) x)
		{  }

	/* Function: FunctionC
		Parameters:
		x - Should have type
	*/
	public (string, (int, float)) FunctionC ((string, (int, float)) x)
		{  }

	/* Function: FunctionD
		Parameters:
		x - Should have type with names
		a - Should not have type
		b - Should not have type
		c - Should not have type
	*/
	public (string a, (int b, float c)) FunctionD ((string a, (int b, float c)) x)
		{  }

	/* Function: FunctionE
		Parameters:
		x - Should have type with names
		a - Should not have type
		b - Should not have type
		c - Should not have type
	*/
	public (string[,,] a, (Namespace.MyClass<int> b, float? c)) FunctionE ((string[,,] a, (Namespace.MyClass<int> b, float? c)) x)
		{  }

	/* Function: TypeSpacing
		Parameters:
		x1 - Should have evenly-spaced type
		x2 - Should have evenly-spaced type
		x3 - Should have evenly-spaced type with names
		x4 - Should have evenly-spaced type with names
		x5 - Should have evenly-spaced type with names
		x6 - Should have evenly-spaced type with names
		a - Should not have type
		b - Should not have type
		c - Should not have type
	*/
	public void TypeSpacing ( ( ( int , int ) , float , ( string , string ) ) x1,
										  ((int,int),float,(string,string)) x2,
										  ( ( int a , int b ) , float c , ( string d , string e ) ) x3,
										  ((int a,int b),float c,(string d,string e)) x4,
										  ( string[,,] a, ( Namespace.MyClass<int> b , float? c ) ) x5,
										  (string[,,] a,(Namespace.MyClass<int> b,float? c)) x6)
		{  }

	}