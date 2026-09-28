
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



// Group: Cast Operators
// ______________________________________________

// Operator: int
operator int() { }

// Operator: unsigned int
explicit operator unsigned int() { }

// Operator: ClassName
operator ClassName() { }

// Operator: QualifiedClassName
explicit operator NamespaceName::QualifiedClassName() { }

// Operator: ClassName&
operator ClassName&() { }

// Operator: QualifiedClassName**
explicit operator NamespaceName::QualifiedClassName**() { }

// Operator: Global_Class_Name
operator ::Global_Class_Name() { }

// Operator: Qualified_Template_Name
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
