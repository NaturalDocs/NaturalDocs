
# Function: BareParameters
function BareParameters ($a, $b, $c)
	{
	}

# Function: ParameterTypes
function ParameterTypes ([string] $a,
										[int] $b,
										[switch] $c,
										[DateTime] $d,
										[PSCredential] $e,
										[object[]] $f,
										[System.IO.FileInfo] $g)
	{
	}

# Function: ParameterModifiers
function ParameterModifiers ([Parameter(Mandatory=$true)] $a,
											[Parameter(Position=2)] $b,
											[Parameter(Mandatory=$true, Position=3)] $c,
											[ValidateRange(0,10)] $d,
											[ValidateNotNull] $e,
											[Alias("f")] $f,
											[Parameter(Mandatory=$true, Position=7)][ValidateNotNull][Alias('g',"ggg")][ValidateRange(0,10)] $g)
	{
	}

# Function: DetermineTypeA
function DetermineTypeA ([Parameter(Mandatory=$true)][Alias("a")][ValidateNotNull][string] $a,
										[string][Parameter(Mandatory=$true)][Alias("b")][ValidateNotNull] $b,
										[Parameter(Mandatory=$true)][string][Alias("c")][ValidateNotNull] $c)
	{
	}

# Function: DetermineTypeB
function DetermineTypeB ([Parameter(Mandatory=$true)][Alias("a")][ValidateNotNull][DateTime] $a,
										[DateTime][Parameter(Mandatory=$true)][Alias("b")][ValidateNotNull] $b,
										[Parameter(Mandatory=$true)][DateTime][Alias("c")][ValidateNotNull] $c)
	{
	}

# Function: DetermineTypeC
function DetermineTypeC ([Parameter(Mandatory=$true)][Alias("a")][ValidateNotNull][System.IO.FileInfo[]] $a,
										[System.IO.FileInfo[]][Parameter(Mandatory=$true)][Alias("b")][ValidateNotNull] $b,
										[Parameter(Mandatory=$true)][System.IO.FileInfo[]][Alias("c")][ValidateNotNull] $c)
	{
	}
