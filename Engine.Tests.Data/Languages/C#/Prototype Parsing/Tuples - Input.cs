class TestClass
	{

	// Functions

	public (string, int) FunctionA ((string, int) x)
		{  }

	public (string a, int b) FunctionB ((string a, int b) x)
		{  }

	public (string, (int, float)) FunctionC ((string, (int, float)) x)
		{  }

	public (string a, (int m, float n) b) FunctionD ((string a, (int m, float n) b) x)
		{  }

	public (string[,,] a, (Namespace.MyClass<int> m, float? n) b) FunctionE ((string[,,] a, (Namespace.MyClass<int> m, float? n) b) x)
		{  }


	// Variables

	public (string, int) varA;

	public (string a, int b) varB;

	public (string, (int, float)) varC;

	public (string a, (int m, float n) b) varD;

	public (string[,,] a, (Namespace.MyClass<int> m, float? n) b) varE;


	// Properties

	public (string, int) PropertyA
		{  get  }

	public (string a, int b) PropertyB
		{  get; set  }

	public (string, (int, float)) PropertyC
		{  get { }  }

	public (string a, (int m, float n) b) PropertyD
		{  get { } set { }  }

	public (string[,,] a, (Namespace.MyClass<int> m, float? n) b) PropertyE
		{  get  }


	// Spacing

	public void TypeSpacing ( ( ( int , int ) , float , ( string , string ) ) x1,
										  ((int,int),float,(string,string)) x2,
										  ( ( int m , int n ) a , float b , ( string i , string j ) c ) x3,
										  ((int m,int n)a,float b,(string i,string j)c) x4,
										  ( string[,,] a, ( Namespace.MyClass<int> m , float? n ) b ) x5,
										  (string[,,] a,(Namespace.MyClass<int> m,float? n)b) x6)
		{  }

	}