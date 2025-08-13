
public class Template<T>
	{
	}

public struct TemplateWithInheritance<X,Y> : System.Collections.List<Y>, Interface
	{
	}

public interface TemplateWithConditions<in X, out Y, out Z> 
	where X: class, Interface, new ()
	where Y: System.Collections.IEnumerable<Y>
    where Z: class?, unmanaged, notnull
	{
	}

public class TemplateWithEmbeddedTemplates<X,Y,Z> : Base<KeyValuePair<X,Y>, int>
	where Z: IEnumerable<KeyValuePair<X,Y>>
	{
	}