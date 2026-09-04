
// Group: Function Bodies
// ______________________________________________

// Function: NoBody
bool NoBody(int param);

// Function: StandardBody
bool StandardBody(int param) { return true; }

// Constructor: StandardBodyConstructor
StandardBodyConstructor(int param) : BaseConstructor(param, null), internalVariable(param) {  }

// Function: TryBody
bool TryBody(int param)
	try
		{  return true;  }
	catch
		{  return false;  }

// Constructor: TryBodyConstructor
TryBodyConstructor(int param) : BaseConstructor(param, null), internalVariable(param)
	try
		{  }
	catch
		{  }

// Function: ZeroBody
// Defines a pure virtual function.
virtual bool ZeroBody(int param) = 0;

// Function: DefaultBody
// Explicitly defines a function with a defaulted implementation.  Can only be used in certain circumstances,
// in this example a copy constructor.
DefaultBody::DefaultBody(DefaultBody& toCopy) = default;

// Function: DeleteBody
// Prevents compilation if the function is used.
bool DeleteBody(int param) = delete;

// Function: DeleteBodyWithMessage
bool DeleteBodyWithMessage(int param) = delete("You cannot use this function.");

