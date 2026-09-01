Measures whether a positional differ can align an obfuscated assembly against its
pre-obfuscation original, which is the attack member reordering exists to defeat.

    dotnet run --project align.csproj -- \
      <project>/Library/Obfuz/<target>/OriginalAssemblies/Assembly-CSharp.dll \
      <project>/Library/Obfuz/<target>/ObfuscatedAssemblies/Assembly-CSharp.dll \
      <project>/Assets/Obfuz/SymbolObfus/symbol-mapping.xml

Reports two different things. The whole-dump figure is the acceptance criterion. The
within-type figure is what a structural attack that re-identifies the declaring type
first would get instead, and reordering does not address it.
