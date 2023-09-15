
# Topic: Keywords with Symbols
#
# Some keywords can start with symbols, like $true and -eq.
#
# --- Code
#
#    if (Do-Something -eq $true -and Do-SomethingElse -ne $null)
#       { ... }
#
# ---

# Topic: Traps
#
# --- Code
#
#    true $true $$true true_ $true_
#
#    eq -eq --eq eq_ -eq_ $eq $-eq
#
# ---
