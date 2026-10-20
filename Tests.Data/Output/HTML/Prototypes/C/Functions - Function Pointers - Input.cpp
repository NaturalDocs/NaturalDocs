
// Group: Function Pointers
// ______________________________________________

// Function: GlobalFunctionPointerAsParameter
void GlobalFunctionPointerAsParameter( void(*paramA)(int, int), float paramB) { }

// Function: MemberFunctionPointerAsParameter
void MemberFunctionPointerAsParameter( void(ParentClass::*paramA)(int, int), float paramB) { }

// Function: GlobalFunctionPointerAsReturnValue
void (*GlobalFunctionPointerAsReturnValue(int paramA, float paramB))(int, int) { }

// Function: MemberFunctionPointerAsReturnValue
void (ParentClass::*MemberFunctionPointerAsReturnValue(int paramA, float paramB))(int, int) { }

// Function: GlobalFunctionPointerAsReturnValueWithAuto
auto GlobalFunctionPointerAsReturnValueWithAuto(int paramA, float paramB) -> void(*)(int, int) { }

// Function: MemberFunctionPointerAsReturnValueWithAuto
auto MemberFunctionPointerAsReturnValueWithAuto(int paramA, float paramB) -> void(ClassName::*)(int, int) { }



// Group: Function References
// ______________________________________________
//
// Function references use the same syntax as function pointers only substituting & for *.  However, they cannot be
// used with member functions, only global ones.
//

// Function: GlobalFunctionReferenceAsParameter
void GlobalFunctionReferenceAsParameter( void(&paramA)(int, int), float paramB) { }

// Function: GlobalFunctionReferenceAsReturnValue
void (&GlobalFunctionReferenceAsReturnValue(int paramA, float paramB))(int, int) { }

// Function: GlobalFunctionReferenceAsReturnValueWithAuto
auto GlobalFunctionReferenceAsReturnValueWithAuto(int paramA, float paramB) -> void(&)(int, int) { }



// Group: Function Pointers with Modifiers
// ______________________________________________

// Function: GlobalFunctionPointerAsParameterWithModifiers
void GlobalFunctionPointerAsParameterWithModifiers( void(*paramA)(int, int) const noexcept(true), float paramB) { }

// Function: MemberFunctionPointerAsParameterWithModifiers
void MemberFunctionPointerAsParameterWithModifiers( void(ParentClass::*paramA)(int, int) const && noexcept(true), float paramB) { }

// Function: GlobalFunctionPointerAsReturnValueWithModifiers
void (*GlobalFunctionPointerAsReturnValueWithModifiers(int paramA, float paramB) const noexcept(true))(int, int) { }

// Function: MemberFunctionPointerAsReturnValueWithModifiers
void (ParentClass::*MemberFunctionPointerAsReturnValueWithModifiers(int paramA, float paramB) const & noexcept(true))(int, int) { }

// Function: GlobalFunctionPointerAsReturnValueWithAutoAndModifiers
auto GlobalFunctionPointerAsReturnValueWithAutoAndModifiers(int paramA, float paramB) -> void(*)(int, int) const noexcept(true) { }

// Function: MemberFunctionPointerAsReturnValueWithAutoAndModifiers
auto MemberFunctionPointerAsReturnValueWithAutoAndModifiers(int paramA, float paramB) -> void(ClassName::*)(int, int) const noexcept(true) { }



// Group: Attributes, Arrays, and Templates
// ______________________________________________

// Function: GlobalFunctionPointerAsReturnValueWithAttributes
[[deprecated]] void (*GlobalFunctionPointerAsReturnValueWithAttributes(int paramA, float paramB))(int, int) { }

// Function: MemberFunctionPointerAsReturnValueWithAttributes
__declspec(noinline code_seg("SectionName")) void (ParentClass::*MemberFunctionPointerAsReturnValueWithAttributes(int paramA, float paramB))(int, int) { }

// Function: GlobalFunctionPointerArrayAsParameter
void GlobalFunctionPointerArrayAsParameter( void(*paramA[])(int, int), float paramB) { }

// Function: MemberFunctionPointerArrayAsParameter
void MemberFunctionPointerArrayAsParameter( void(ParentClass::*paramA[6][])(int, int), float paramB) { }

// Function: TemplatedMemberFunctionPointerAsParameter
void TemplatedMemberFunctionPointerAsParameter( void(ParentClass<int>::*paramA)(int, int), float paramB) { }

// Function: TemplatedMemberFunctionPointerAsReturnValue
void (ParentClass<int>::*TemplatedMemberFunctionPointerAsReturnValue(int paramA, float paramB))(int, int) { }
