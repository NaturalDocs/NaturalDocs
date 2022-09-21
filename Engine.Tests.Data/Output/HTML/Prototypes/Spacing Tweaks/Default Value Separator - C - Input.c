
// Group: Simple
// ____________________________________________________________________________


// Function: SimpleA
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void SimpleA (int a, int b = 12)
	{
	}

// Function: SimpleB
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void SimpleB (int aa, int b = 12)
	{
	}

// Function: SimpleC
// Must *retain* the space before the equals because it would touch the variable.
public void SimpleC (int a, int bb = 12)
	{
	}

// Function: SimpleD
// Must *retain* the space before the equals because it would touch the variable.
public void SimpleD (int a, int bbb = 12)
	{
	}



// Group: Multiple
// ____________________________________________________________________________


// Function: MultipleA
// Must *retain* the space before the equals because it would touch the variable.
public void MultipleA (int a, int b = 12, int ccc = 12)
	{
	}

// Function: MultipleB
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void MultipleB (int aaa, int b = 12, int ccc = 12)
	{
	}



// Group: Symbols
// ____________________________________________________________________________


// Function: SymbolsA
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void SymbolsA (int a[], int b[] = 12)
	{
	}

// Function: SymbolsB
// Must *retain* the space before the equals because it would touch the variable.
public void SymbolsB (int a[], int b<T> = 12)
	{
	}

// Function: SymbolsC
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void SymbolsC (int a<T>, int b[] = 12)
	{
	}

// Function: SymbolsD
// Must *retain* the space before the equals because it would touch the variable.
public void SymbolsD (int a, int b[] = 12)
	{
	}

// Function: SymbolsE
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void SymbolsE (int a[], int b = 12)
	{
	}

// Function: SymbolsF
// Must *retain* the space before the equals because it would touch the variable.
public void SymbolsF (int aa, int b[] = 12)
	{
	}

// Function: SymbolsG
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void SymbolsG (int aaa, int b[] = 12)
	{
	}

// Function: SymbolsH
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void SymbolsH (int a[], int bb = 12)
	{
	}

// Function: SymbolsI
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void SymbolsI (int a[], int bbb = 12)
	{
	}

// Function: SymbolsJ
// Must *retain* the space before the equals because it would touch the variable.
public void SymbolsJ (int a[], int bbbb = 12)
	{
	}



// Group: Space Uniformity
// ____________________________________________________________________________
//
// Default value separators must have uniformity regardless of how the spacing appears in the source.
//


// Function: UniformityA
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void UniformityA (int a, int b=12)
	{
	}

// Function: UniformityB
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void UniformityB (int a, int b = 12)
	{
	}

// Function: UniformityC
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void UniformityC (int a, int b  =  12)
	{
	}

// Function: UniformityD
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void UniformityD (int a, int bb=12)
	{
	}

// Function: UniformityE
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void UniformityE (int a, int bb = 12)
	{
	}

// Function: UniformityF
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void UniformityF (int a, int bb  =  12)
	{
	}

// Function: UniformityG
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void UniformityG (int aa, int b=12)
	{
	}

// Function: UniformityH
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void UniformityH (int aa, int b = 12)
	{
	}

// Function: UniformityI
// Can *omit* the space before the equals because it still wouldn't touch the variable.
public void UniformityI (int aa, int b  =  12)
	{
	}
