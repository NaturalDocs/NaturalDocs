
// Constructor: ConstructorWithParentCalls
// Parent calls shouldn't be included in the prototype.
ConstructorWithParentCalls(int param) : BaseConstructor(param, null), internalVariable(param) {  }

// Constructor: ConstructorWithParentCallsAndAttributes
// Parent calls shouldn't be included in the prototype.
[[Attribute]] ClassName::ConstructorWithParentCallsAndAttributes (int param) noexcept(true) : BaseConstructor(param, null), internalVariable(param) {  }

// Destructor: ~SimpleDestructor
~SimpleDestructor() { }

// Destructor: ~VirtualDestructor
virtual ~VirtualDestructor() { }
