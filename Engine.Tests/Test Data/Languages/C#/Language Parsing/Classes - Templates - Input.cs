
public class Template<T>
	{
	}

public struct TemplateWithInheritance<X,Y> : System.Collections.List<Y>, Interface
	{
	}

public interface TemplateWithConditions<in X, out Y> 
	where X: class, Interface, new ()
	where Y: System.Collections.IEnumerable<Y>
	{
	}

public class TemplateWithEmbeddedTemplates<X,Y,Z> : Base<KeyValuePair<X,Y>, int>
	where Z: IEnumerable<KeyValuePair<X,Y>>
	{
	}