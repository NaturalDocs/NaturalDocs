
// Class: Plain
class Plain { }

// Class: WithModifiers
public static class WithModifiers { }

// Class: AsTemplate
class AsTemplate<T> { }

// Class: WithMetadata
[Something]
class WithMetadata { }

// Class: WithEverything
[Something("value", 2)]
[SomethingElse]
public static class WithEverything<X, Y> { }
