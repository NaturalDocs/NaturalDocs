
# Proc: EmptyParams1
proc EmptyParams1 {} { }

# Proc: EmptyParams2
proc EmptyParams2 { } { }



# Proc: SingleParam_Braces
proc SingleParam_Braces {a} { }

# Proc: SingleParam_NoBraces
proc SingleParam_NoBraces a { }

# Proc: SingleParam_BracesAndDefaultValue1
proc SingleParam_BracesAndDefaultValue1 { {a 12} } { }

# Proc: SingleParam_BracesAndDefaultValue2
proc SingleParam_BracesAndDefaultValue2 { {a "abc}def"} } { }

# Proc: SingleParam_BracesAndDefaultValue3
proc SingleParam_BracesAndDefaultValue3 { {a [1 2 3]} } { }

# Proc: SingleParam_BracesAndDefaultValue4
proc SingleParam_BracesAndDefaultValue4 { {a {1 2 3}} } { }

# Proc: SingleParam_BracesAndDefaultValue5
proc SingleParam_BracesAndDefaultValue5 { {a {}} } { }

# Proc: SingleParam_BracesAndDefaultValue6
proc SingleParam_BracesAndDefaultValue6 { {a { }} } { }

# Proc: SingleParam_Args_Braces
proc SingleParam_Args_Braces {args} { }

# Proc: SingleParam_Args_NoBraces
proc SingleParam_Args_NoBraces args { }



# Proc: MultiParam1
proc MultiParam1 { a b c } { }

# Proc: MultiParam2
proc MultiParam2 { {a 12} b c } { }

# Proc: MultiParam3
proc MultiParam3 { a {b 12} c } { }

# Proc: MultiParam4
proc MultiParam4 { a b {c 12} } { }

# Proc: MultiParam5
proc MultiParam5 { {a 12} b {c 15} } { }

# Proc: MultiParam6
proc MultiParam6 { {a 12} {b "abc}def"} {c [1 2 3]} } { }

# Proc: MultiParam7
proc MultiParam7 { a {b 12} c args} { }



