Invariants that can only be checked against a real obfuscated build, because they depend on
the project's own rename rules (scene/prefab bindings, link.xml, custom policies) rather than
anything in the package.

    dotnet run --project check.csproj -- \
      <project>/Library/Obfuz/<target>/OriginalAssemblies/Assembly-CSharp.dll \
      <project>/Library/Obfuz/<target>/ObfuscatedAssemblies/Assembly-CSharp.dll

A method whose NAME survived obfuscation was pinned by the rename policy, which means
something outside the IL resolves it by name — a UnityEvent in scene YAML, SendMessage,
StartCoroutine, link.xml. Those call sites pass a fixed argument list, so such a method must
never be padded. Catching this needs a real build: the package's own self-check cannot see the
project's rule files or custom rename policies.

This exists because ParamPad originally built its rename policy from its own (empty by
default) settings instead of the symbol obfuscation ones, which padded 1641 name-bound
methods and silently broke every UnityEvent handler in the game. A startup-only smoke test
does not catch it — those handlers only fire when a player clicks something.
