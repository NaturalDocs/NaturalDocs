
// Group: Properties and Attributes
// ______________________________________________
//
// More elaborate tests of attribute forms are in the function and class tests.
//

// var: typeProperties
unsigned int* typeProperties;

// var: typePropertiesWithParentheses
alignas(8) unsigned int* typePropertiesWithParentheses;

// var: bracketedAttributes
[[AttributeA]] [[AttributeB(12, "string")]] NamespaceA::TypeA bracketedAttributes;

// var: underscoredAttributes
__attribute__((AttributeA)) __attribute__((AttributeB(12, "string"))) void* underscoredAttributes = 0;

// const: constant
const unsigned int* constant = 0;

// var: array
volatile int array[256];

// var: templatedType
Dictionary<int, char*> templatedType;

// var: nestedTemplatedType
std::unique_ptr<Dictionary<int, char*>> nestedTemplatedType = 0;



// Group: Function Pointers
// ______________________________________________

// var: globalFunctionPointer
void(*globalFunctionPointer)(int, int);

// var: memberFunctionPointer
void(ParentClass::*memberFunctionPointer)(int, int) = null;

// var: globalFunctionReference
void (&globalFunctionReference)(int, int);

// var: globalFunctionPointerWithModifiers
void (*globalFunctionPointerWithModifiers)(int, int) volatile noexcept = null;

// var: memberFunctionPointerWithModifiers
void (ParentClass::*memberFunctionPointerWithModifiers)(int, int) volatile && noexcept(true) = null;
