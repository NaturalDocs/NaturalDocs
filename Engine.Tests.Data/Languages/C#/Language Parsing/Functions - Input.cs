
class TestClass
	{
	public void FunctionA ()
		{  }

	abstract int FunctionB (int a);

	[Attribute]
	protected internal IList<T> FunctionC<T> () where T: System.Collections.IEnumerable
		{  }

	static int FunctionD (this string a, int b, params object[] c)
		{  }

	internal partial void Interface.Interface.FunctionE<T> () where T: new();

	unsafe void* FunctionF (int* x)
		{  }

	public async int FunctionG ()
		{  }

	public int FunctionH (bool x) => x ? 1 : 0;

	public static RgbColor FunctionI (RgbColor color) =>
		new RgbColor(
			color.Red ^ 0xFF,
			color.Green ^ 0xFF,
			color.Blue ^ 0xFF
			);
	}