class TestClass
	{

	// Functions

	public (string, int) FunctionA ((string, int) x)
		{  }

	public (string a, int b) FunctionB ((string a, int b) x)
		{  }

	public (string, (int, float)) FunctionC ((string, (int, float)) x)
		{  }

	public (string a, (int b, float c)) FunctionD ((string a, (int b, float c)) x)
		{  }

	public (string[,,] a, (Namespace.MyClass<int> b, float? c)) FunctionE ((string[,,] a, (Namespace.MyClass<int> b, float? c)) x)
		{  }


	// Variables

	public (string, int) varA;

	public (string a, int b) varB;

	public (string, (int, float)) varC;

	public (string a, (int b, float c)) varD;

	public (string[,,] a, (Namespace.MyClass<int> b, float? c)) varE;


	// Properties

	public (string, int) PropertyA
		{  get  }

	public (string a, int b) PropertyB
		{  get; set  }

	public (string, (int, float)) PropertyC
		{  get { }  }

	public (string a, (int b, float c)) PropertyD
		{  get { } set { }  }

	public (string[,,] a, (Namespace.MyClass<int> b, float? c)) PropertyE
		{  get  }


	// Spacing

	public void TypeSpacing ( ( ( int , int ) , float , ( string , string ) ) x1,
										  ((int,int),float,(string,string)) x2,
										  ( ( int a , int b ) , float c , ( string d , string e ) ) x3,
										  ((int a,int b),float c,(string d,string e)) x4,
										  ( string[,,] a, ( Namespace.MyClass<int> b , float? c ) ) x5,
										  (string[,,] a,(Namespace.MyClass<int> b,float? c)) x6)
		{  }

	}