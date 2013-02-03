
/* Class: Namespace.ClassFromOutsideNamespace
*/
namespace Namespace
	{
	class ClassFromOutsideNamespace
		{
		}
	}

/* Class: ClassName
	This topic should attach to the class even though there's a topic between them.

	Topic: InterveningCommentOnlyTopic
*/
class ClassName
	{

	/* Function: FunctionA
		These topics should attach to the functions because they're in the proper order.
	
		Function: FunctionB

		Function: FunctionC
	*/

	void FunctionA ()
		{
		}

	void FunctionB ()
		{
		}

	void FunctionC ()
		{
		}

	/* Function: FunctionD
		
		Function: FunctionF
		This should attach to the function even though there's an undocumented function in between
		them because they're still in the proper order.
	*/

	void FunctionD ()
		{
		}

	void FunctionE ()
		{
		}

	void FunctionF ()
		{
		}

	}