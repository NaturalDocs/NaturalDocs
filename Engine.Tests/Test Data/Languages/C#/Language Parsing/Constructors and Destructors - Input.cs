
class TestClass
	{
	public TestClass ()
		{ }

	private TestClass (int x) : base (x)
		{ }

	private TestClass (int x, int y) : this (x)
		{ }

	unsafe public TestClass (int* x, void* y)
		{ }

	~TestClass ()
		{ }

	extern static TestClass (float x);

	extern ~TestClass ();
	}
