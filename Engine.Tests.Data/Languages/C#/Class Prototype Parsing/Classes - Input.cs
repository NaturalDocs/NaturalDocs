
// Class: Plain
class Plain { }

// Class: WithModifiers
public static interface WithModifiers { }

// Class: Template
class Template<T> { }

// Struct: Metadata
[Something]
struct Metadata { }

// Interface: Everything
[Something("value", 2)]
[SomethingElse]
public static interface Everything<X, Y> { }
