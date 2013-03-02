
class OneParent : Parent { }

class ThreeParents : Parent1, Parent2, Parent3 { }

class ParentsWithQualifiers : X.Y.Parent1, A.B.Parent2 { }

class ParentTemplate : List<int> { }

class TemplateWithQualifiedParentTemplates<X,Y> : System.Collections.Generic.IEnumerable<X>, Parent2<X,Y> { }
