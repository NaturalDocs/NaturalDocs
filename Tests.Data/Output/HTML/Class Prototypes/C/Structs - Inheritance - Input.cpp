
// Group: Simple Inheritance
// ______________________________________________

// Struct: SimpleStructInheritance
struct SimpleStructInheritance : BaseStruct { }

// Struct: StructInheritanceWithPublic
struct StructInheritanceWithPublic : public BaseStruct { }

// Struct: StructInheritanceWithPrivate
struct StructInheritanceWithPrivate : private BaseStruct { }



// Group: Multiple Inheritance
// ______________________________________________

// Struct: MultipleStructInheritance
struct MultipleStructInheritance : BaseStruct1, BaseStruct2 { }

// Struct: MultipleStructInheritanceWithAccessA
struct MultipleStructInheritanceWithAccessA : public BaseStruct1, private BaseStruct2, protected BaseStruct3 { }

// Struct: MultipleStructInheritanceWithAccessB
struct MultipleStructInheritanceWithAccessB : public BaseStruct1, BaseStruct2 { }

// Struct: MultipleStructInheritanceWithAccessC
struct MultipleStructInheritanceWithAccessC : BaseStruct1, private BaseStruct2 { }



// Group: Virtual Inheritance
// ______________________________________________

// Struct: VirtualStructInheritance
struct VirtualStructInheritance : virtual BaseStruct { }

// Struct: VirtualMultipleStructInheritanceWithAccess
// The access modifier and "virtual" can be in any order
struct VirtualMultipleStructInheritanceWithAccess : virtual public BaseStruct1, private virtual BaseStruct2 { }



// Group: Namespaces
// ______________________________________________

// Struct: Namespace::NamespaceOnStruct
struct Namespace::NamespaceOnStruct : BaseStruct { }

// Struct: NamespaceOnBaseStruct
struct NamespaceOnBaseStruct : private Namespace::BaseStruct { }

// Struct: NamespaceA::NamespaceOnBothStructs
struct NamespaceA::NamespaceOnBothStructs : protected NamespaceB::BaseStruct { }



// Group: Attributes
// ______________________________________________

// Struct: AttributeOnStruct
struct [[Attribute]] AttributeOnStruct : BaseStruct { }

// Struct: AttributeOnBaseStruct
struct AttributeOnBaseStruct : [[Attribute]] public BaseStruct { }

// Struct: AttributeOnBothStructs
struct [[AttributeA]] AttributeOnBothStructs : [[AttributeB]] private BaseStruct { }

// Struct: UnderscoreAttributesOnBothStructs
struct __attribute__((AttributeA)) UnderscoreAttributesOnBothStructs final : __attribute__((AttributeB)) protected virtual Namespace::BaseStruct { }
