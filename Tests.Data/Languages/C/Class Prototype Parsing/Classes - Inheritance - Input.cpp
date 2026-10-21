
// Group: Simple Inheritance
// ______________________________________________

// Class: SimpleInheritance
class SimpleInheritance : BaseClass { }

// Class: InheritanceWithPublic
class InheritanceWithPublic : public BaseClass { }

// Class: InheritanceWithPrivate
class InheritanceWithPrivate : private BaseClass { }



// Group: Multiple Inheritance
// ______________________________________________

// Class: MultipleInheritance
class MultipleInheritance : BaseClass1, BaseClass2 { }

// Class: MultipleInheritanceWithAccessA
class MultipleInheritanceWithAccessA : public BaseClass1, private BaseClass2, protected BaseClass3 { }

// Class: MultipleInheritanceWithAccessB
class MultipleInheritanceWithAccessB : public BaseClass1, BaseClass2 { }

// Class: MultipleInheritanceWithAccessC
class MultipleInheritanceWithAccessC : BaseClass1, private BaseClass2 { }



// Group: Virtual Inheritance
// ______________________________________________

// Class: VirtualInheritance
class VirtualInheritance : virtual BaseClass { }

// Class: VirtualMultipleInheritanceWithAccess
// The access modifier and "virtual" can be in any order
class VirtualMultipleInheritanceWithAccess : virtual public BaseClass1, private virtual BaseClass2 { }



// Group: Namespaces
// ______________________________________________

// Class: Namespace::NamespaceOnClass
class Namespace::NamespaceOnClass : BaseClass { }

// Class: NamespaceOnBaseClass
class NamespaceOnBaseClass : private Namespace::BaseClass { }

// Class: NamespaceA::NamespaceOnBoth
class NamespaceA::NamespaceOnBoth : protected NamespaceB::BaseClass { }



// Group: Attributes
// ______________________________________________

// Class: AttributeOnClass
class [[Attribute]] AttributeOnClass : BaseClass { }

// Class: AttributeOnBase
class AttributeOnBase : [[Attribute]] public BaseClass { }

// Class: AttributeOnBoth
class [[AttributeA]] AttributeOnBoth : [[AttributeB]] private BaseClass { }

// Class: UnderscoreAttributesOnBoth
class __attribute__((AttributeA)) UnderscoreAttributesOnBoth final : __attribute__((AttributeB)) protected virtual Namespace::BaseClass { }
