
# Topic: Escape Characters
#
# Strings are *not* escaped with backslashes.  They are escaped with backticks.
#
# --- Code
#
#    Do-Something -Param1 C:\A\B\C -Param2 "C:\A\B\C\" -Param3 $true
#
#    Do-Something -Param1 "A`"B" -Param2 'A`'B' -Param3 $false
#
#    Do-Something -Param1 "A``" -Param2 $null
#
# ---
