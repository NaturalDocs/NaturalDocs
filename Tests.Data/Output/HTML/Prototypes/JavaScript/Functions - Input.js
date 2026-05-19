
// Group: Declaration Types
// ______________________________________________

// Function: StandaloneDeclaration
function StandaloneDeclaration ()
	{  }

class TestClass
	{
	// Function: ClassMethodDeclaration
	ClassMethodDeclaration ()
		{  }
	}

// Function: ThisAssignmentDeclaration
this.ThisAssignmentDeclaration = function ()
	{  }

// Function: NamedObjectAssignmentDeclaration
NamedObject.NamedObjectAssignmentDeclaration = function ()
	{  }

// Function: PrototypeAssignmentDeclaration
NamedClass.prototype.PrototypeAssignmentDeclaration = function ()
	{  }


// Group: Parameters
// ______________________________________________

// Function: BasicParameters
function BasicParameters (a, b = 12)
	{  }

// Function: RestParameter
function RestParameter (a, b, ...c)
	{  }


// Group: Special Function Types
// ______________________________________________

class TestClass
	{
	// Function: constructor
	constructor ()
		{  }

	// Function: StaticMethod
	static StaticMethod ()
		{  }
	}

// Function: StandaloneGeneratorFunction
function *StandaloneGeneratorFunction ()
	{  }

class TestClass
	{
	// Function: ClassGeneratorMethod
	*ClassGeneratorMethod ()
		{  }
	}

// Function: StandaloneAsyncFunction
async function StandaloneAsyncFunction ()
	{  }

class TestClass
	{
	// Function: ClassAsyncMethod
	async ClassAsyncMethod ()
		{  }
	}

// Function: StandaloneAsyncGeneratorFunction
async function *StandaloneAsyncGeneratorFunction ()
	{  }

class TestClass
	{
	// Function: ClassAsyncGeneratorMethod
	async *ClassAsyncGeneratorMethod ()
		{  }
	}
