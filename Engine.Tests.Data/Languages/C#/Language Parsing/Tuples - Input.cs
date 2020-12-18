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

	}