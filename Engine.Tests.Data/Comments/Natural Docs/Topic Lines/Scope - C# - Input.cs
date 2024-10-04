
// Topic: TopicA
// Should be global

// Class: ClassB
// Should start a scope
class ClassB
	{

	// Topic: TopicB
	// Should appear in ClassB

	// Function: FunctionB
	// Should appear in ClassB
	public void FunctionB ()
		{
		}

	// Variable: VariableB
	// Should appear in ClassB
	private int VariableB;


	// Class: ClassB.ChildClassC
	// Should appear in ClassB
	class ChildClassC
		{

		// Topic: TopicC
		// Should appear in ClassB.ChildClassC

		// Function: FunctionC
		// Should appear in ClassB.ChildClassC
		public void FunctionC ()
			{
			}

		// Variable: VariableC
		// Should appear in ClassB.ChildClassC
		private int VariableC;
		}


	// Class: ChildClassD
	// Should appear in ClassB
	class ChildClassD
		{

		// Topic: TopicD
		// Should appear in ClassB.ChildClassD

		// Function: FunctionD
		// Should appear in ClassB.ChildClassD
		public void FunctionD ()
			{
			}

		// Variable: VariableD
		// Should appear in ClassB.ChildClassD
		private int VariableD;
		}


	class ChildClassE
		{

		// Topic: TopicE
		// Should appear in ClassB.ChildClassE

		public void FunctionE ()
			{
			}

		private int VariableE;
		}


	// Topic: TopicF
	// Should appear in ClassB
	}


// Topic: TopicG
// Should be global

