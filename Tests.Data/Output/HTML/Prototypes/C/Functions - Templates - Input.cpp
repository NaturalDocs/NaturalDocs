
// Group: Template Parameters
// ______________________________________________

// Function: ClassTemplateParameter
// No difference between using "class" and "typename".
template<class T> void ClassTemplateParameter() { }

// Function: TypeNameTemplateParameter
// No difference between using "class" and "typename".
template<typename T> void TypeNameTemplateParameter() { }

// Function: UnnamedTemplateParameters
template<class, class> void UnnamedTemplateParameters() { }

// Function: TemplateParameterWithDefault
template<typename T = void> void TemplateParameterWithDefault() { }

// Function: TemplateParameterPack
template<class... Ts> void TemplateParameterPack() { }

// Function: TemplateWithSpecificTypes
template<int A, int B> void TemplateWithSpecificTypes() { }



// Group: Template Constraints
// ______________________________________________

// Function: SimpleConstraint
template<typename T> requires std::totally_ordered<T> void SimpleConstraint() { }

// Function: MultipleConstraintsAnd
template <typename T> requires std::ranges::range<T> && std::is_integral_v<T> void MultipleConstraintsAnd() { }

// Function: MultipleConstraintsOr
template <typename T> requires std::ranges::range<T> || std::is_integral_v<T> void MultipleConstraintsOr() { }

// Function: MultipleConstraintsParentheses
template<typename T> requires (EqualityComparable<T> || Same<T, void>)
void MultipleConstraintsParentheses() { }

// Function: InlineConstraintExpression
// Inline expressions require parentheses.  Only named constraints and double requires can omit them.
// Deliberately using a < to make sure it's not treated as the start of a block and expects a closing >.
template<typename T> requires (sizeof(T) < 8) void InlineConstraintExpression() { }

// Function: DoubleRequiresConstraintA
// Why C++ why?
template<typename T>
requires requires (T x) { sizeof(x) < 8; }
void DoubleRequiresConstraintA() { }

// Function: DoubleRequiresConstraintB
template<typename T>
requires requires(T a, T b) {
    { a + b } -> std::same_as<T>;
    a.swap(b);
	}
void DoubleRequiresConstraintB() { }



// Group: Trailing Constraints
// ______________________________________________
//
// Constraints can also appear after the function declaration and don't have to be part of a template.
// Putting tests here anyway to keep them all together.
//

// Function: SimpleTrailingConstraint
void SimpleTrailingConstraint() requires std::totally_ordered<T> { }

// Function: MultipleTrailingConstraintsAnd
void MultipleTrailingConstraintsAnd() requires std::ranges::range<T> && std::is_integral_v<T> { }

// Function: MultipleTrailingConstraintsOr
void MultipleTrailingConstraintsOr() requires std::ranges::range<T> || std::is_integral_v<T> { }

// Function: MultipleTrailingConstraintsParentheses
void MultipleTrailingConstraintsParentheses() requires (EqualityComparable<T> || Same<T, void>) { }

// Function: InlineTrailingConstraintExpression
// Inline expressions require parentheses.  Only named constraints and double requires can omit them.
// Deliberately using a < to make sure it's not treated as the start of a block and expects a closing >.
void InlineTrailingConstraintExpression() requires (sizeof(T) < 8) { }

// Function: TrailingDoubleRequiresConstraintA
// Why C++ why?
void TrailingDoubleRequiresConstraintA()
requires requires (T x) { sizeof(x) < 8; } { }

// Function: TrailingDoubleRequiresConstraintB
void TrailingDoubleRequiresConstraintB()
requires requires(T a, T b) {
    { a + b } -> std::same_as<T>;
    a.swap(b);
	} { }



// Group: Both Constraints
// ______________________________________________

// Function: BothConstraints
template <typename T> requires std::ranges::range<T>
void BothConstraints()
requires std::totally_ordered<T> { }

// Function: BothConstraintsAndOr
template <typename T> requires std::ranges::range<T> && std::is_integral_v<T>
void BothConstraints()
requires std::ranges::range<T> || std::is_integral_v<T> { }

// Function: BothConstraintsParentheses
template <typename T> requires (sizeof(T) < 8)
void InlineTrailingConstraintExpression() requires (sizeof(T) < 8) { }

// Function: BothDoubleRequiresConstraintsA
// Why C++ why?
template <typename T> requires requires (T x) { sizeof(x) < 8; }
void BothDoubleRequiresConstraintsA()
requires requires (T x) { sizeof(x) < 8; } { }

// Function: BothDoubleRequiresConstraintsB
template <typename T>
requires requires(T a, T b) {
    { a + b } -> std::same_as<T>;
    a.swap(b);
	}
void BothDoubleRequiresConstraintsB()
requires requires(T a, T b) {
    { a + b } -> std::same_as<T>;
    a.swap(b);
	} { }



// Group: Template Instantiation
// ______________________________________________

// Function: ExplicitInstantiation
template void ExplicitInstantiation<double> (double x) { }

// Function: InstantiationWithImpliedTypeA
template void InstantiationWithImpliedTypeA<> (char x) { }

// Function: InstantiationWithImpliedTypeB
template void InstantiationWithImpliedTypeB (char x) { }



// Group: Templates with Attributes
// ______________________________________________

// Function: AttributeLeadingTemplate
[[Attribute]] template<typename T> void AttributeLeadingTemplate() { }

// Function: TemplateLeadingAttribute
template<typename T> [[Attribute]] void TemplateLeadingAttribute() { }
