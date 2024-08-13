
// Namespaces without bodies apply to the entire file
// Introduced in C# 10, used in Avalonia
// Cannot have more than one per file or nested namespaces
namespace Namespace1;

class ClassInNamespace1
	{
	class ChildClassInNamespace1
		{
		}
	}

interface InterfaceInNamespace1
	{
	}

enum EnumInNamespace1
	{
	}

struct StructInNamespace1
	{
	}

delegate void DelegateInNamespace1 ();
