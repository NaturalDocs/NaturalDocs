
[AttributeA]
[AttributeB()]
[AttributeC ()]
public void SimpleAttributes()
	{  }

/// If the parameters aren't named, format them on a single line.
[AttributeA(12, "string")]
public void AttributeParametersA()
	{  }

/// If the parameters are named, format them like function parameters.
[AttributeA(x = 12, y = "string")]
[AttributeB(x: 12, y: "string")]
public void AttributeParametersB()
	{  }

/// Mixing naming styles will look a little weird, but we support it.
[AttributeA(x = 12, y: "string")]
[AttributeB(x: 12, y = "string")]
public void AttributeParametersC()
	{  }

/// If a named parameter follows an unnamed one, still format them on a single line.
[AttributeA(12, "string")]
[AttributeB(12, y = "string")]
[AttributeC(12, y: "string")]
public void AttributeParametersD()
	{  }

[AttributeA(Enum.FlagA | Enum.FlagB)]
[AttributeB(Property = Enum.FlagA | Enum.FlagB)]
[AttributeC(Property: Enum.FlagA | Enum.FlagB)]
public void ComplexValues()
	{  }

/// Method and return attributes should appear in the prototype.  Assembly and module ones should not.
[assembly:GlobalAttribute_ShouldBeExcluded]
[module: GlobalAttribute_ShouldBeExcluded(12)]
[method:AttributeA]
[return: AttributeB("string")]
public void AttributeTargets()
	{  }

/// Inline parameter attributes should format on a single line even if they have named properties.
public void ParameterAttributesA([AttributeA] int a,
											  [AttributeB()] int b,
											  [AttributeC(12, "string")] int c = 10,
											  [param:AttributeD(x = 12, y = "string")] int d = 20)
	{  }

/// Multiple consecutive parameter attributes can be broken into separate lines, though their properties should not be.
public void ParameterAttributesB([AttributeA][AttributeB()] int a,
											  [AttributeC(12, "string")][param:AttributeD(x = 12, y = "string")] int b = 10)
	{  }
