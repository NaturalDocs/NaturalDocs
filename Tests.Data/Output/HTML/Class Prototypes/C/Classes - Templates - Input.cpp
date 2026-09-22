
// Group: Simple Templates
// ______________________________________________

// Class: SimpleTemplate
template<typename T>
class SimpleTemplate { }

// Class: ExportTemplate
// Removed in C++ 11 but we'll support it anyway
export template<typename T>
class ExportTemplate { }



// Group: Templates with Constraints
// ______________________________________________

// Class: SimpleConstraint
template<typename T> requires Incrementable<T>
class SimpleConstraint { }

// Class: MultipleConstraintsAnd
template<typename T> requires Integral<T> && std::is_signed<T>::value
class MultipleConstraintsAnd { }

// Class: MultipleConstraintsOr
template<typename T> requires EqualityComparable<T> || Same<T, void>
class MultipleConstraintsOr { }

// Class: MultipleConstraintsParentheses
template<typename T> requires (EqualityComparable<T> || Same<T, void>)
class MultipleConstraintsParentheses { }

// Class: InlineConstraintExpression
// Inline expressions require parentheses.  Only named constraints and double requires can omit them.
// Deliberately using a < to make sure it's not treated as the start of a block and expects a closing >.
template<typename T> requires (sizeof(T) < 8)
class InlineConstraintExpression { }

// Class: DoubleRequiresConstraintA
// Why C++ why?
template<typename T>
requires requires (T x) { sizeof(x) < 8; }
class DoubleRequiresConstraintA { }

// Class: DoubleRequiresConstraintB
template<typename T>
requires requires(T a, T b) {
    { a + b } -> std::same_as<T>;
    a.swap(b);
	}
class DoubleRequiresConstraintB { }



// Group: Templates With Attributes
// ______________________________________________

// Class: AttributeBeforeTemplate
[[Attribute]]
template<typename T>
class AttributeBeforeTemplate { }

// Class: AttributeAfterTemplateBeforeClass
template<typename T>
[[Attribute]]
class AttributeAfterTemplateBeforeClass { }

// Class: AttributeAfterClass
template<typename T>
class [[Attribute]] AttributeAfterClass { }

// Class: AttributesInAllPositions
[[AttributeA]]
template<typename T>
[[AttributeB]]
class [[AttributeC]] AttributesInAllPositions { }



// Group: Combinations
// ______________________________________________

// Class: TemplateWithEverything
__attribute__((AttributeA)) template<typename T> requires (EqualityComparable<T> || Same<T, void>)
class [[AttributeB]] TemplateWithEverything { }
