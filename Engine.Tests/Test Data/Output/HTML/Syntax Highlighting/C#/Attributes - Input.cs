
/*
	Topic: Attributes

		(code)

		[assembly: GlobalAttribute]
		[module: GlobalAttribute2("x")]
		[UntargetedAttribute]  // Comment
		[UntargetedAttribute2("x", false)]
		class TestClass
			{

			[Flags]
			enum x { a, b, c };

			}

		(end)


	Topic: Attributes vs. Arrays

		(code)

		[Test, Category("x")]
		void Function ([param: something][something else] string[][] x)
			{
			x = new string[12];
			x[variable] = "";
			}

		(end)

*/
