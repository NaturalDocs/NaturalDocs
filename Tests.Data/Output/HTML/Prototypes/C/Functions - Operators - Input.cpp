
// Group: Single Character Operators
// ______________________________________________

// Operator: operator+
int operator+ (int a) { }

// Operator: operator/
int operator/ (int a) { }

// Operator: operator!
int operator! (int a) { }

// Operator: operator,
int operator, (int a) { }

// Operator: operator<
// Make sure it doesn't break by thinking this is an unclosed template signature.
int operator< (int a) { }



// Group: Double Character Operators
// ______________________________________________

// Operator: operator+=
int operator+= (int a) { }

// Operator: operator<<
// Make sure it doesn't break by thinking this is an unclosed template signature.
int operator<< (int a) { }

// Operator: operator<=
// Make sure it doesn't break by thinking this is an unclosed template signature.
int operator<= (int a) { }

// Operator: operator&&
int operator&& (int a) { }

// Operator: operator->
int operator-> (int a) { }



// Group: Triple Character Operators
// ______________________________________________

// Operator: operator<<=
// Make sure it doesn't break by thinking this is an unclosed template signature.
int operator<<= (int a) { }

// Operator: operator<=>
int operator<=> (int a) { }

// Operator: operator->*
int operator->* (int a) { }



// Group: Type Conversion Operators
// ______________________________________________

// Operator: operator int
operator int() { }

// Operator: operator unsigned int*
explicit operator unsigned int*() { }

// Operator: operator ClassName
operator ClassName() { }

// Operator: operator QualifiedClassNameA
explicit operator NamespaceName::QualifiedClassNameA() { }

// Operator: operator NamespaceName::QualifiedClassNameB
explicit operator NamespaceName::QualifiedClassNameB() { }

// Operator: operator ClassName&
operator ClassName&() { }

// Operator: operator QualifiedClassName**
explicit operator NamespaceName::QualifiedClassName**() { }

// Operator: operator Global_Class_Name
operator ::Global_Class_Name() { }

// Operator: operator NamespaceName::Qualified_Template_Name<int>*
explicit operator Namespace_Name::Qualified_Template_Name<int>*() { }



// Group: Other Operators
// ______________________________________________

// Operator: operator[]
int operator[] (int a) { }

// Operator: operator()
int operator() (int a) { }

// Operator: operator new
int operator new (int a) { }

// Operator: operator new[]
int operator new[] (int a) { }

// Operator: operator delete
int operator delete (int a) { }

// Operator: operator delete[]
int operator delete[] (int a) { }

// Operator: operator co_await
int operator co_await (int a) { }



// Group: Spacing
// ______________________________________________
//
// Spaces are allowed between "operator" and the symbol, but not between the symbols themselves like
// between the brackets in "operator []".  They also can't be between new/delete and their brackets.
//

// Operator: operator +
int operator + (int a) { }

// Operator: operator ()
int operator ()(int a) { }

// Operator: operator <<=
int operator <<= (int a) { }
