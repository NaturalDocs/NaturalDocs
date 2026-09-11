
// Group: Bracketed Attributes
// ______________________________________________

// Struct: SimpleAttributes
[[AttributeA]]
[[AttributeB()]]
[[AttributeC(12, "string")]]
struct SimpleAttributes { }

// Struct: AttributesWithNamespaces
[[Namespace::AttributeA]]
[[Namespace::AttributeB()]]
[[Namespace::AttributeC(12, "string")]]
struct AttributesWithNamespaces { }

// Struct: AttributesInLists
[[AttributeA, AttributeB(), AttributeC(12, "string")]]
struct AttributesInLists { }

// Struct: AttributesInListsWithNamespaces
[[Namespace::AttributeA, Namespace::AttributeB(), Namespace::AttributeC(12, "string")]]
struct AttributesInListsWithNamespaces { }

// Struct: AttributesWithUsing
[[using Namespace: AttributeA]]
struct AttributesWithUsing { }

// Struct: AttributesInListsWithUsing
[[using Namespace: AttributeA, AttributeB(), AttributeC(12, "string")]]
struct AttributesInListsWithUsing { }

// Struct: AttributesAfterKeyword
struct [[AttributeA]][[Namespace::AttributeB(12, "string")]] AttributesAfterKeyword { }

// Struct: AttributesBeforeAndAfterKeyword
[[AttributeA]] struct [[Namespace::AttributeB(12, "string")]] AttributesBeforeAndAfterKeyword { }



// Group: Attributes via __attribute__
// ______________________________________________
//
// Unlike bracket attributes, __attribute__ must appear after the struct keyword unless you're also declaring
// an instance variable in the same statement.
//

// Struct: DeprecatedUnderscored
struct __attribute__((deprecated)) DeprecatedUnderscored { }

// Struct: AlignedUnderscored
struct __attribute__((aligned(8))) AlignedUnderscored { }

// Struct: MultipleSeparateUnderscored
struct __attribute__((deprecated))
__attribute__((aligned(8))) MultipleSeparateUnderscored { }

// Struct: MultipleCombinedUnderscored
struct __attribute__((deprecated, aligned(8))) MultipleCombinedUnderscored { }



// Group: Attributes via __declspec
// ______________________________________________
//
// Unlike bracket attributes, __declspec must appear after the struct keyword unless you're also declaring
// an instance variable in the same statement.
//

// Struct: DeprecatedDeclSpec
struct __declspec(deprecated) DeprecatedDeclSpec { }

// Struct: AlignDeclSpec
struct __declspec(align(8)) AlignDeclSpec { }

// Struct: MultipleSeparateDeclSpec
struct __declspec(deprecated)
__declspec(align(8)) MultipleSeparateDeclSpec { }

// Struct: MultipleCombinedDeclSpec
struct __declspec(deprecated align(8)) MultipleCombinedDeclSpec { }



// Group: AlignAs
// ______________________________________________

// Struct: AlignAsBeforeKeyword
alignas(8) struct AlignAsBeforeKeyword { }

// Struct: AlignAsAfterKeyword
struct alignas(8) AlignAsAfterKeyword { }



// Group: Final
// ______________________________________________

// Struct: FinalAfterName
struct FinalAfterName final { }



// Group: Mixed Attributes
// ______________________________________________

// Struct: MixedAttributes
[[AttributeA]] struct alignas(8) __attribute__((deprecated)) MixedAttributes final { }
