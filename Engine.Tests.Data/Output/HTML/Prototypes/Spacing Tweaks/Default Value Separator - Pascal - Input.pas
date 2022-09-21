
// Group: Simple
// ____________________________________________________________________________


// Function: SimpleA
// Can *omit* the space before the equals because it still wouldn't touch the variable.
Function SimpleA (a: integer, b: float := 12): integer;
	Begin
	End

// Function: SimpleB
// Must *retain* the space before the equals because it would touch the variable.
Function SimpleB (a: float, b: integer := 12): integer;
	Begin
	End

// Function: SimpleC
// Must *retain* the space before the equals because it would touch the variable.
Function SimpleC (a: float*, b: integer:= 12): integer;
	Begin
	End

// Function: SimpleD
// Can *omit* the space before the equals because it still wouldn't touch the variable.
Function SimpleD (a: float[], b: integer := 12): integer;
	Begin
	End



// Group: Multiple
// ____________________________________________________________________________


// Function: MultipleA
// Must *retain* the space before the equals because it would touch the variable.
Function MultipleA (a: float, b: float := 12, c: integer := 12): integer;
	Begin
	End

// Function: MultipleB
// Can *omit* the space before the equals because it still wouldn't touch the variable.
Function MultipleB (a: integer, b: float := 12, c: integer := 12): integer;
	Begin
	End



// Group: Space Uniformity
// ____________________________________________________________________________
//
// Default value separators must have uniformity regardless of how the spacing appears in the source.
//


// Function: UniformityA
// Can *omit* the space before the equals because it still wouldn't touch the variable.
Function UniformityA (a: float, b: float:=12): integer;
	Begin
	End

// Function: UniformityB
// Can *omit* the space before the equals because it still wouldn't touch the variable.
Function UniformityB (a: float, b: float := 12): integer;
	Begin
	End

// Function: UniformityC
// Can *omit* the space before the equals because it still wouldn't touch the variable.
Function UniformityC (a: float, b: float  :=  12): integer;
	Begin
	End

// Function: UniformityD
// Must *retain* the space before the equals because it would touch the variable.
Function UniformityD (a: float, b: integer:=12): integer;
	Begin
	End

// Function: UniformityE
// Must *retain* the space before the equals because it would touch the variable.
Function UniformityE (a: float, b: integer := 12): integer;
	Begin
	End

// Function: UniformityF
// Must *retain* the space before the equals because it would touch the variable.
Function UniformityF (a: float, b: integer  :=  12): integer;
	Begin
	End

// Function: UniformityG
// Can *omit* the space before the equals because it still wouldn't touch the variable.
Function UniformityG (a: integer, b: float:=12): integer;
	Begin
	End

// Function: UniformityH
// Can *omit* the space before the equals because it still wouldn't touch the variable.
Function UniformityH (a: integer, b: float := 12): integer;
	Begin
	End

// Function: UniformityI
// Can *omit* the space before the equals because it still wouldn't touch the variable.
Function UniformityI (a: integer, b: float  :=  12): integer;
	Begin
	End
