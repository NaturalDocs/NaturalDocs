
// Group: Bracketed Attributes
// ______________________________________________

// Function: SimpleAttributes
[[AttributeA]]
[[AttributeB()]]
[[AttributeC(12, "string")]]
void SimpleAttributes();

// Function: AttributesWithNamespaces
[[Namespace::AttributeA]]
[[Namespace::AttributeB()]]
[[Namespace::AttributeC(12, "string")]]
void AttributesWithNamespaces();

// Function: AttributesInLists
[[AttributeA, AttributeB(), AttributeC(12, "string")]]
void AttributesInLists();

// Function: AttributesInListsWithNamespaces
[[Namespace::AttributeA, Namespace::AttributeB(), Namespace::AttributeC(12, "string")]]
void AttributesInListsWithNamespaces();

// Function: AttributesWithUsing
[[using Namespace: AttributeA]]
void AttributesWithUsing();

// Function: AttributesInListsWithUsing
[[using Namespace: AttributeA, AttributeB(), AttributeC(12, "string")]]
void AttributesInListsWithUsing();

// Function: AttributeSpacing
[[AttributeA]]
[ [ AttributeB ] ]
[ [ AttributeC, AttributeD ] ]
void AttributeSpacing();



// Group: Attributes via __attribute__
// ______________________________________________
//
// The double parentheses are a requirement, not just a convention.
//

// Function: UnderscoredAttribute
__attribute__((noinline)) void UnderscoredAttribute() { }

// Function: UnderscoredAttributeWithParameter
__attribute__((section("SectionName"))) void UnderscoredAttributeWithParameter() { }

// Function: MultipleSeparateUnderscoredAttributes
__attribute__((noinline))
__attribute__((section("SectionName"))) void MultipleSeparateUnderscoredAttributes() { }

// Function: MultipleCombinedUnderscoredAttributes
__attribute__((noinline, code_seg("SectionName"))) void MultipleCombinedUnderscoredAttributes() { }

// Function: UnderscoredAttributeSpacing
__attribute__((AttributeA))
__attribute__ ((AttributeB))
__attribute__ ( ( AttributeC ) )
__attribute__ ( ( AttributeD, AttributeE ) )
void UnderscoredAttributeSpacing();



// Group: Attributes via __declspec
// ______________________________________________

// Function: DeclSpecAttribute
__declspec(noinline) void DeclSpecAttribute() { }

// Function: DeclSpecAttributeWithParameter
__declspec(code_seg("SectionName")) void DeclSpecAttributeWithParameter() { }

// Function: DeclSpecMultipleSeparateAttributes
__declspec(noinline)
__declspec(code_seg("SectionName")) void DeclSpecMultipleSeparateAttributes() { }

// Function: DeclSpecMultipleCombinedAttributes
__declspec(noinline code_seg("SectionName")) void DeclSpecMultipleCombinedAttributes() { }

// Function: DeclSpecAttributeSpacing
__declspec(AttributeA)
__declspec ( AttributeB )
__declspec ( AttributeC AttributeD )
void DeclSpecAttributeSpacing();

// Function: DeclSpecWhitespaceTraps
__declspec ( )
__declspec ( AttributeA AttributeB )
__declspec ( AttributeC
AttributeD
  AttributeE )
__declspec ( AttributeF /* comment */ AttributeG )
__declspec ( AttributeH ( "value" ) AttributeI ( "value" )
AttributeJ ( "value" )
  AttributeK ( "value" )
)
void DeclSpecWhitespaceTraps();
