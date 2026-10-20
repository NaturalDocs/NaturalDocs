
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



// Group: Whitespace
// ______________________________________________

// Function: ParameterWhitespaceA
void ParameterWhitespaceA(void(*paramA)(int,int), float paramB) { }

// Function: ParameterWhitespaceB
void ParameterWhitespaceB ( void ( * paramA ) ( int , int ) , float paramB) { }

// Function: ReturnValueWhitespaceA
void (ParentClass::*ReturnValueWhitespaceA(int paramA,float paramB))(int,int) { }

// Function: ReturnValueWhitespaceB
void ( ParentClass::*ReturnValueWhitespaceB (int paramA , float paramB ) ) ( int , int ) { }

// Function: TrailingReturnValueWhitespaceA
auto TrailingReturnValueWhitespaceA(int paramA, float paramB) -> void(*)(int,int) { }

// Function: TrailingReturnValueWhitespaceB
auto TrailingReturnValueWhitespaceB(int paramA, float paramB) -> void ( * ) ( int , int ) { }



// Group: Parameter Alignment
// ______________________________________________

// Function: ParameterAlignmentA
void ParameterAlignmentA (int (*paramA)(int, int),
									  int paramB,
									  int* paramC,
									  unsigned int (*paramD)(int, int),
									  unsigned int paramE,
									  unsigned int* paramF,
									  ClassName (*paramG)(int, int),
									  ClassName paramH,
									  ClassName* paramI,
									  PackageName::ClassName (*paramJ)(int, int),
									  PackageName::ClassName paramK,
									  PackageName::ClassName paramL) { }

// Function: ParameterAlignmentB
void ParameterAlignmentB (int (ClassName::*paramA)(int, int),
									  int paramB,
									  int* paramC,
									  unsigned int (ClassName::*paramD)(int, int),
									  unsigned int paramE,
									  unsigned int* paramF,
									  ClassName (ClassName::*paramG)(int, int),
									  ClassName paramH,
									  ClassName* paramI,
									  PackageName::ClassName (ClassName::*paramJ)(int, int),
									  PackageName::ClassName paramK,
									  PackageName::ClassName paramL) { }

// Function: ParameterAlignmentC
void ParameterAlignmentC (int (PackageName::ClassName::*paramA)(int, int),
									  int paramB,
									  int* paramC,
									  unsigned int (PackageName::ClassName::*paramD)(int, int),
									  unsigned int paramE,
									  unsigned int* paramF,
									  ClassName (PackageName::ClassName::*paramG)(int, int),
									  ClassName paramH,
									  ClassName* paramI,
									  PackageName::ClassName (PackageName::ClassName::*paramJ)(int, int),
									  PackageName::ClassName paramK,
									  PackageName::ClassName paramL) { }

