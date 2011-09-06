
# Function: FunctionName
#
#	Fake Perl syntax but that's not important.
#	
#	Parameters:
#		$x - Should match with the symbol
#		@y - Should match with the symbol
#		%z - Should match with the symbol
#
#	Parameters:
#		x - Should match without the symbol
#		y - Should match without the symbol
#		z - Should match without the symbol
#
sub FunctionName (int $x, array @y, table %z)
	{
	}
	
# Function: FunctionName2
#
#	Parameters:
#		x - Should not show the type if it's just a symbol
#		y - Should not show the type if it's just a symbol
#		z - Should not show the type if it's just a symbol
#
sub FunctionName2 ($x, @y, %z)
	{
	}