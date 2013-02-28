
// Class: OneParent
class OneParent : Parent { }

// Class: ThreeParents
class ThreeParents : Parent1, Parent2, Parent3 { }

// Class: ParentsWithQualifiers
class ParentsWithQualifiers : X.Y.Parent1, A.B.Parent2 { }

// Class: ParentTemplate
class ParentTemplate : List<int> { }

// Class: TemplateWithQualifiedParentTemplates
class TemplateWithQualifiedParentTemplates<X,Y> : System.Collections.Generic.IEnumerable<X>, Parent2<X,Y> { }
