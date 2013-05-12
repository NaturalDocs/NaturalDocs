class HasGroups
	{
	// Group: Manual Group

	void FunctionA () { }
	void FunctionB () { }
	int VariableA;
	int VariableB;
	}

class HasLateGroups
	{
	void FunctionA () { }
	void FunctionB () { }
	
	// Group: Manual Group

	int VariableA;
	int VariableB;
	}

class DoesntHaveGroups
	{
	void FunctionA () { }
	void FunctionB () { }
	int VariableA;
	int VariableB;
	}

class OuterClassHasGroups
	{
	// Group: Manual Group

	void FunctionA () { }
	void FunctionB () { }
	int VariableA;
	int VariableB;

	class InnerClassDoesntHaveGroups
		{
		void FunctionA () { }
		void FunctionB () { }
		int VariableA;
		int VariableB;
		}

	void FunctionC () { }
	void FunctionD () { }
	int VariableC;
	int VariableD;
	}

class OuterClassDoesntHaveGroups
	{
	void FunctionA () { }
	void FunctionB () { }
	int VariableA;
	int VariableB;

	class InnerClassHasGroups
		{
		// Group: Manual Group

		void FunctionA () { }
		void FunctionB () { }
		int VariableA;
		int VariableB;
		}

	void FunctionC () { }
	void FunctionD () { }
	int VariableC;
	int VariableD;
	}