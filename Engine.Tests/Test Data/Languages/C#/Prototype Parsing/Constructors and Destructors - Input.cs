
public TestClass ()
	{ }

private TestClass (int x) : base (x)
	{ }

private TestClass (int x, int y) : this (x)
	{ }

~TestClass ()
	{ }

extern static TestClass (float x);

extern ~TestClass ();
