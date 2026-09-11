
// Group: Bracketed Attributes
// ______________________________________________

// Union: SimpleAttributes
[[AttributeA]]
[[AttributeB()]]
[[AttributeC(12, "string")]]
union SimpleAttributes { }

// Union: AttributesWithNamespaces
[[Namespace::AttributeA]]
[[Namespace::AttributeB()]]
[[Namespace::AttributeC(12, "string")]]
union AttributesWithNamespaces { }

// Union: AttributesInLists
[[AttributeA, AttributeB(), AttributeC(12, "string")]]
union AttributesInLists { }

// Union: AttributesInListsWithNamespaces
[[Namespace::AttributeA, Namespace::AttributeB(), Namespace::AttributeC(12, "string")]]
union AttributesInListsWithNamespaces { }

// Union: AttributesWithUsing
[[using Namespace: AttributeA]]
union AttributesWithUsing { }

// Union: AttributesInListsWithUsing
[[using Namespace: AttributeA, AttributeB(), AttributeC(12, "string")]]
union AttributesInListsWithUsing { }

// Union: AttributesAfterKeyword
union [[AttributeA]][[Namespace::AttributeB(12, "string")]] AttributesAfterKeyword { }

// Union: AttributesBeforeAndAfterKeyword
[[AttributeA]] union [[Namespace::AttributeB(12, "string")]] AttributesBeforeAndAfterKeyword { }



// Group: Attributes via __attribute__
// ______________________________________________
//
// Unlike bracket attributes, __attribute__ must appear after the union keyword unless you're also declaring
// an instance variable in the same statement.
//

// Union: DeprecatedUnderscored
union __attribute__((deprecated)) DeprecatedUnderscored { }

// Union: AlignedUnderscored
union __attribute__((aligned(8))) AlignedUnderscored { }

// Union: MultipleSeparateUnderscored
union __attribute__((deprecated))
__attribute__((aligned(8))) MultipleSeparateUnderscored { }

// Union: MultipleCombinedUnderscored
union __attribute__((deprecated, aligned(8))) MultipleCombinedUnderscored { }



// Group: Attributes via __declspec
// ______________________________________________
//
// Unlike bracket attributes, __declspec must appear after the union keyword unless you're also declaring
// an instance variable in the same statement.
//

// Union: DeprecatedDeclSpec
union __declspec(deprecated) DeprecatedDeclSpec { }

// Union: AlignDeclSpec
union __declspec(align(8)) AlignDeclSpec { }

// Union: MultipleSeparateDeclSpec
union __declspec(deprecated)
__declspec(align(8)) MultipleSeparateDeclSpec { }

// Union: MultipleCombinedDeclSpec
union __declspec(deprecated align(8)) MultipleCombinedDeclSpec { }



// Group: AlignAs
// ______________________________________________

// Union: AlignAsBeforeKeyword
alignas(8) union AlignAsBeforeKeyword { }

// Union: AlignAsAfterKeyword
union alignas(8) AlignAsAfterKeyword { }



// Group: Final
// ______________________________________________

// Union: FinalAfterName
union FinalAfterName final { }



// Group: Mixed Attributes
// ______________________________________________

// Union: MixedAttributes
[[AttributeA]] union alignas(8) __attribute__((deprecated)) MixedAttributes final { }
