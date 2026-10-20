
// Group: Function Pointers
// ______________________________________________

// Function: GlobalFunctionPointerAsParameter
// Params:
// paramA - Test
// paramB - test
void GlobalFunctionPointerAsParameter( void(*paramA)(int, int), float paramB) { }

// Function: MemberFunctionPointerAsParameter
// Params:
// paramA - Test
// paramB - test
void MemberFunctionPointerAsParameter( void(ParentClass::*paramA)(int, int), float paramB) { }

// Function: GlobalFunctionPointerAsReturnValue
// Params:
// paramA - Test
// paramB - test
void (*GlobalFunctionPointerAsReturnValue(int paramA, float paramB))(int, int) { }

// Function: MemberFunctionPointerAsReturnValue
// Params:
// paramA - Test
// paramB - test
void (ParentClass::*MemberFunctionPointerAsReturnValue(int paramA, float paramB))(int, int) { }

// Function: GlobalFunctionPointerAsReturnValueWithAuto
// Params:
// paramA - Test
// paramB - test
auto GlobalFunctionPointerAsReturnValueWithAuto(int paramA, float paramB) -> void(*)(int, int) { }

// Function: MemberFunctionPointerAsReturnValueWithAuto
// Params:
// paramA - Test
// paramB - test
auto MemberFunctionPointerAsReturnValueWithAuto(int paramA, float paramB) -> void(ClassName::*)(int, int) { }



// Group: Function References
// ______________________________________________
//
// Function references use the same syntax as function pointers only substituting & for *.  However, they cannot be
// used with member functions, only global ones.
//

// Function: GlobalFunctionReferenceAsParameter
// Params:
// paramA - Test
// paramB - test
void GlobalFunctionReferenceAsParameter( void(&paramA)(int, int), float paramB) { }

// Function: GlobalFunctionReferenceAsReturnValue
// Params:
// paramA - Test
// paramB - test
void (&GlobalFunctionReferenceAsReturnValue(int paramA, float paramB))(int, int) { }

// Function: GlobalFunctionReferenceAsReturnValueWithAuto
// Params:
// paramA - Test
// paramB - test
auto GlobalFunctionReferenceAsReturnValueWithAuto(int paramA, float paramB) -> void(&)(int, int) { }



// Group: Function Pointers with Modifiers
// ______________________________________________

// Function: GlobalFunctionPointerAsParameterWithModifiers
// Params:
// paramA - Test
// paramB - test
void GlobalFunctionPointerAsParameterWithModifiers( void(*paramA)(int, int) const noexcept(true), float paramB) { }

// Function: MemberFunctionPointerAsParameterWithModifiers
// Params:
// paramA - Test
// paramB - test
void MemberFunctionPointerAsParameterWithModifiers( void(ParentClass::*paramA)(int, int) const && noexcept(true), float paramB) { }

// Function: GlobalFunctionPointerAsReturnValueWithModifiers
// Params:
// paramA - Test
// paramB - test
void (*GlobalFunctionPointerAsReturnValueWithModifiers(int paramA, float paramB) const noexcept(true))(int, int) { }

// Function: MemberFunctionPointerAsReturnValueWithModifiers
// Params:
// paramA - Test
// paramB - test
void (ParentClass::*MemberFunctionPointerAsReturnValueWithModifiers(int paramA, float paramB) const & noexcept(true))(int, int) { }

// Function: GlobalFunctionPointerAsReturnValueWithAutoAndModifiers
// Params:
// paramA - Test
// paramB - test
auto GlobalFunctionPointerAsReturnValueWithAutoAndModifiers(int paramA, float paramB) -> void(*)(int, int) const noexcept(true) { }

// Function: MemberFunctionPointerAsReturnValueWithAutoAndModifiers
// Params:
// paramA - Test
// paramB - test
auto MemberFunctionPointerAsReturnValueWithAutoAndModifiers(int paramA, float paramB) -> void(ClassName::*)(int, int) const noexcept(true) { }



// Group: Attributes, Arrays, and Templates
// ______________________________________________

// Function: GlobalFunctionPointerAsReturnValueWithAttributes
// Params:
// paramA - Test
// paramB - test
[[deprecated]] void (*GlobalFunctionPointerAsReturnValueWithAttributes(int paramA, float paramB))(int, int) { }

// Function: MemberFunctionPointerAsReturnValueWithAttributes
// Params:
// paramA - Test
// paramB - test
__declspec(noinline code_seg("SectionName")) void (ParentClass::*MemberFunctionPointerAsReturnValueWithAttributes(int paramA, float paramB))(int, int) { }

// Function: GlobalFunctionPointerArrayAsParameter
// Params:
// paramA - Test
// paramB - test
void GlobalFunctionPointerArrayAsParameter( void(*paramA[])(int, int), float paramB) { }

// Function: MemberFunctionPointerArrayAsParameter
// Params:
// paramA - Test
// paramB - test
void MemberFunctionPointerArrayAsParameter( void(ParentClass::*paramA[6][])(int, int), float paramB) { }

// Function: TemplatedMemberFunctionPointerAsParameter
// Params:
// paramA - Test
// paramB - test
void TemplatedMemberFunctionPointerAsParameter( void(ParentClass<int>::*paramA)(int, int), float paramB) { }

// Function: TemplatedMemberFunctionPointerAsReturnValue
// Params:
// paramA - Test
// paramB - test
void (ParentClass<int>::*TemplatedMemberFunctionPointerAsReturnValue(int paramA, float paramB))(int, int) { }
