
/// <summary>No para</summary>
void Test1 () { }

/// <summary><para>Para</para></summary>
void Test2 () { }

/// <summary><para>Para 1</para><para>Para 2</para></summary>
void Test3 () { }

/// <summary>Before para<para>Para</para></summary>
void Test4 () { }

/// <summary><para>Para</para>After para</summary>
void Test5 () { }

/// <summary>Before para<para>Para 1</para>Between paras<para>Para 2</para>After para</summary>
void Test6 () { }

/// <summary>
///    <para>Para 1 on separate line</para>
///    <para>Para 2 on separate line</para>
/// </summary>
void Test7 () { }

/// <summary><para>Para on first line.
/// Continued on second line.</para></summary>
void Test8 () { }

/// <summary><para>Para on first line.
///     Continued on second line with extra indent.</para></summary>
void Test9 () { }

/// <summary><para>Para on two lines but
///     broken mid-sentence.</para></summary>
void Test10 () { }

/// <summary><para>
///    Para content on standalone line with extra indent.
/// </para></summary>
void Test11 () { }

/// <summary><para>  Whitespace  </para>  <para>  Whitespace everywhere  </para></summary>
void Test12 () { }
