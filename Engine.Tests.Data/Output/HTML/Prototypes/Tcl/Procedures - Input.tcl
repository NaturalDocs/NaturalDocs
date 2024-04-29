
# Group: Empty Parameters
# _____________________________________________________________________________

# Proc: EmptyParams1
proc EmptyParams1 {} { }

# Proc: EmptyParams2
proc EmptyParams2 { } { }



# Group: Single Parameters
# _____________________________________________________________________________

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



# Group: Multiple Parameters
# _____________________________________________________________________________

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



# Group: Parameter Alignment
# _____________________________________________________________________________

# Proc: ParamAlignment1
proc ParamAlignment1 {a {b 1} {c "abcdefgh"} } { }

# Proc: ParamAlignment2
proc ParamAlignment2 {a {b "abcdefgh"} {c 1} } { }

# Proc: ParamAlignment3
proc ParamAlignment3 {a {bbbbbbb 12} {c 12} } { }

# Proc: ParamAlignment4
proc ParamAlignment4 {a {b 12} {ccccccc 12} } { }

# Proc: ParamAlignment5
proc ParamAlignment5 {a {bbbbbbb "abcdefgh"} {c 1} } { }

# Proc: ParamAlignment6
proc ParamAlignment6 {a {b "abcdefgh"} {ccccccc 1} } { }

# Proc: ParamAlignment7
proc ParamAlignment7 {a {bbbbbbb 1} {c "abcdefgh"} } { }

# Proc: ParamAlignment8
proc ParamAlignment8 {a {b 1} {ccccccc "abcdefgh"} } { }



# Group: Parameter Spacing
# _____________________________________________________________________________

# Proc: SingleParamSpacing1
proc SingleParamSpacing1 { a } { }

# Proc: SingleParamSpacing2
proc SingleParamSpacing2 {a} { }

# Proc: SingleParamSpacing3
proc SingleParamSpacing3 {a } { }

# Proc: SingleParamSpacing4
proc SingleParamSpacing4 { a} { }


# Proc: MultiParamSpacing1
proc MultiParamSpacing1 { a b c } { }

# Proc: MultiParamSpacing2
proc MultiParamSpacing2 {a b c} { }

# Proc: MultiParamSpacing3
proc MultiParamSpacing3 {a b c } { }

# Proc: MultiParamSpacing4
proc MultiParamSpacing4 { a b c} { }


# Proc: BracedParamSpacing1
proc BracedParamSpacing1 {a {b 12} c} { }

# Proc: BracedParamSpacing2
proc BracedParamSpacing2 { a { b 12 } c } { }

# Proc: BracedParamSpacing3
proc BracedParamSpacing3 { a {b 12} c } { }

# Proc: BracedParamSpacing4
proc BracedParamSpacing4 {a { b 12 } c} { }

# Proc: BracedParamSpacing5
proc BracedParamSpacing5 { a { b 12} c} { }

# Proc: BracedParamSpacing6
proc BracedParamSpacing6 {a {b 12 } c } { }

# Proc: BracedParamSpacing7
proc BracedParamSpacing7 { a {b 12 } c} { }

# Proc: BracedParamSpacing8
proc BracedParamSpacing8 {a { b 12} c } { }


# Proc: SpacingTweaks1
proc SpacingTweaks1 {aaa b c } { }

# Proc: SpacingTweaks2
proc SpacingTweaks2 {a bbb c } { }

# Proc: SpacingTweaks3
proc SpacingTweaks3 {a b ccc } { }

# Proc: SpacingTweaks4
proc SpacingTweaks4 {{a 12} b c } { }

# Proc: SpacingTweaks5
proc SpacingTweaks5 {a {b 12} c } { }

# Proc: SpacingTweaks6
proc SpacingTweaks6 {a b {c 12} } { }

