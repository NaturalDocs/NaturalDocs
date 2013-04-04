
/// <summary>Opening paras as breaks<para>
/// Opening paras as breaks<para>
/// Opening paras as breaks</summary>
void Test1 () { }

/// <summary>Standalone paras as breaks<para />
/// Standalone paras as breaks<para />
/// Standalone paras as breaks</summary>
void Test2 () { }

/// <summary>Summary with<example>unclosed example.</summary>
/// <remark>Remark with<example>unclosed example.</remark>
void Test3 () { }

/// <summary><code>
/// Code with opening <tag>
/// </code></summary>
void Test4 () { }

/// <summary>Unrecognized <blah>tags</blah></summary>
void Test5 () { }

/// <summary>Unopened </example>closing </c>tags</summary>
void Test6 () { }

/// <param name="Param1">Param 1</param>
/// <exception cref="Exception1">Exception 1</exception>
/// <permission cref="Permission1">Permission 1</permission>
/// <param name="Param2">Out of order Param 2</param>
/// <exception cref="Exception2">Out of order Exception 2</exception>
/// <permission cref="Permission2">Out of order Permission 2</permission>
/// <summary>Summary at end of topic</summary>
void Test7 () { }