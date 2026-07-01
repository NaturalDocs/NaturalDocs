
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

// Function: AttributesInParameters
void AttributesInParameters([[AttributeA]] int x,
										   [[Namespace::AttributeB()]] float y,
										   [[AttributeC(12), AttributeD("string", 0)]] double z);
