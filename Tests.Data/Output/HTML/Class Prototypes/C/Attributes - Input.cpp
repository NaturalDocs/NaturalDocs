
// Class: SimpleAttributes
[[AttributeA]]
[[AttributeB()]]
[[AttributeC(12, "string")]]
class SimpleAttributes { }


// Class: AttributesWithNamespaces
[[Namespace::AttributeA]]
[[Namespace::AttributeB()]]
[[Namespace::AttributeC(12, "string")]]
class AttributesWithNamespaces { }


// Class: AttributesInLists
[[AttributeA, AttributeB(), AttributeC(12, "string")]]
class AttributesInLists { }

// Class: AttributesInListsWithNamespaces
[[Namespace::AttributeA, Namespace::AttributeB(), Namespace::AttributeC(12, "string")]]
class AttributesInListsWithNamespaces { }

// Class: AttributesWithUsing
[[using Namespace: AttributeA]]
class AttributesWithUsing { }

// Class: AttributesInListsWithUsing
[[using Namespace: AttributeA, AttributeB(), AttributeC(12, "string")]]
class AttributesInListsWithUsing { }
