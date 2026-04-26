
// Class: AnnotatedClass
@ClassAnnotation class AnnotatedClass
	{
	// Function: AnnotatedFunction
    @FunctionAnnotation fun AnnotatedFunction (@ParameterAnnotation parameter: Int): Int
		{  }
	}

// Class: MultipleParameterizedAnnotatedClass
@ClassAnnotation1
@ClassAnnotation2("string")
@ClassAnnotation3(value1, "value2", 3)
class MultipleParameterizedAnnotatedClass
	{
	// Function: MultipleParameterizedAnnotatedFunction
	@FunctionAnnotation1
	@FunctionAnnotation2("string")
	@FunctionAnnotation3(value1, "value2", 3)
    fun MultipleParameterizedAnnotatedFunction (parameter: Int): Int
		{  }

	// Function: ScopedAnnotations
	fun ScopedAnnotations (@field:Annotation1 val x,
										 @get:Annotation2 val y,
										 @param:Annotation3 val z)
		{  }

	// Variable: MultiScopedAnnotation
	@set:[Annotation1 Annotation2]
	var MultiScopedAnnotation: VariableType
	}