
// Group: Bracketed Attributes
// ______________________________________________

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

// Class: AttributesAfterKeyword
class [[AttributeA]][[Namespace::AttributeB(12, "string")]] AttributesAfterKeyword { }

// Class: AttributesBeforeAndAfterKeyword
[[AttributeA]] class [[Namespace::AttributeB(12, "string")]] AttributesBeforeAndAfterKeyword { }



// Group: Attributes via __attribute__
// ______________________________________________
//
// Unlike bracket attributes, __attribute__ must appear after the class keyword unless you're also declaring
// an instance variable in the same statement.
//

// Class: DeprecatedUnderscored
class __attribute__((deprecated)) DeprecatedUnderscored { }

// Class: AlignedUnderscored
class __attribute__((aligned(8))) AlignedUnderscored { }

// Class: MultipleSeparateUnderscored
class __attribute__((deprecated))
__attribute__((aligned(8))) MultipleSeparateUnderscored { }

// Class: MultipleCombinedUnderscored
class __attribute__((deprecated, aligned(8))) MultipleCombinedUnderscored { }



// Group: Attributes via __declspec
// ______________________________________________
//
// Unlike bracket attributes, __declspec must appear after the class keyword unless you're also declaring
// an instance variable in the same statement.
//

// Class: DeprecatedDeclSpec
class __declspec(deprecated) DeprecatedDeclSpec { }

// Class: AlignDeclSpec
class __declspec(align(8)) AlignDeclSpec { }

// Class: MultipleSeparateDeclSpec
class __declspec(deprecated)
__declspec(align(8)) MultipleSeparateDeclSpec { }

// Class: MultipleCombinedDeclSpec
class __declspec(deprecated align(8)) MultipleCombinedDeclSpec { }



// Group: AlignAs
// ______________________________________________

// Class: AlignAsBeforeKeyword
alignas(8) class AlignAsBeforeKeyword { }

// Class: AlignAsAfterKeyword
class alignas(8) AlignAsAfterKeyword { }



// Group: Final
// ______________________________________________

// Class: FinalAfterName
class FinalAfterName final { }



// Group: Mixed Attributes
// ______________________________________________

// Class: MixedAttributes
[[AttributeA]] class alignas(8) __attribute__((deprecated)) MixedAttributes final { }
