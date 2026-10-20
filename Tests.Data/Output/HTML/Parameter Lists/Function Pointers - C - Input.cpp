
// Group: Function Pointer Parameters
// ______________________________________________

/* Function: GlobalFunctionPointerParameter

	Parameters:
		paramA - Description
		paramB - Description
*/
void GlobalFunctionPointerParameter( void(*paramA)(int, int), float paramB) { }


/* Function: MemberFunctionPointerParameter

	Parameters:
		paramA - Description
		paramB - Description
*/
void MemberFunctionPointerParameter( void(ParentClass::*paramA)(int, int), float paramB) { }


/* Function: GlobalFunctionReferenceParameter

	Parameters:
		paramA - Description
		paramB - Description
*/
void GlobalFunctionReferenceParameter( void(&paramA)(int, int), float paramB) { }


/* Function: GlobalFunctionPointerArrayParameter

	Parameters:
		paramA - Description
		paramB - Description
*/
void GlobalFunctionPointerArrayParameter( void(*paramA[])(int, int), float paramB) { }


/* Function: MemberFunctionPointerArrayParameter

	Parameters:
		paramA - Description
		paramB - Description
*/
void MemberFunctionPointerArrayParameter( void(ParentClass::*paramA[])(int, int), float paramB) { }


/* Function: TemplatedMemberFunctionPointerParameter

	Parameters:
		paramA - Description
		paramB - Description
*/
void TemplatedMemberFunctionPointerParameter( void(ParentClass<int>::*paramA)(int, int), float paramB) { }


/* Function: GlobalFunctionPointerAsParameterWithModifiers

	Parameters:
		paramA - Description
		paramB - Description
*/
void GlobalFunctionPointerAsParameterWithModifiers( void(*paramA)(int, int) const noexcept, float paramB) { }


/* Function: MemberFunctionPointerAsParameterWithModifiers

	Parameters:
		paramA - Description
		paramB - Description
*/
void MemberFunctionPointerAsParameterWithModifiers( void(ParentClass::*paramA)(int, int) const && noexcept(true), float paramB) { }



// Group: Regular Parameters with Function Pointer Return Values
// ______________________________________________
//
// To make sure the parser doesn't get confused by the syntax.
//

/* Function: RegularParametersWithReturnedGlobalFunctionPointer

	Parameters:
		paramA - Description
		paramB - Description
*/
void (*RegularParametersWithReturnedGlobalFunctionPointer(int paramA, float paramB))(int, int) { }


/* Function: RegularParametersWithReturnedMemberFunctionPointer

	Parameters:
		paramA - Description
		paramB - Description
*/
void (ParentClass::*RegularParametersWithReturnedMemberFunctionPointer(int paramA, float paramB))(int, int) { }


/* Function: RegularParametersWithReturnedGlobalFunctionPointerWithAuto

	Parameters:
		paramA - Description
		paramB - Description
*/
auto RegularParametersWithReturnedGlobalFunctionPointerWithAuto(int paramA, float paramB) -> void(*)(int, int) { }


/* Function: RegularParametersWithReturnedMemberFunctionPointerWithAuto

	Parameters:
		paramA - Description
		paramB - Description
*/
auto RegularParametersWithReturnedMemberFunctionPointerWithAuto(int paramA, float paramB) -> void(ClassName::*)(int, int) { }
