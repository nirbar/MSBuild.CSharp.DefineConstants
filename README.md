Helper MSBuild target to generate a C# code file with constants.

Example project file:
~~~~~~
  <ItemGroup>
    <PackageReference Include="MSBuild.CSharp.DefineConstants" />
  </ItemGroup>
  <PropertyGroup>
    <CSharpDefineConstantsNamespace>MyNamespace</CSharpDefineConstantsNamespace>
    <CSharpDefineConstantsClassName>MyConstants</CSharpDefineConstantsClassName>
    <CSharpDefineConstantsTargetPath>$(IntermediateOutputPath)MyConstants.cs</CSharpDefineConstantsTargetPath>

    <MyStringArray>val1;val2;val3;;</MyStringArray>
  </PropertyGroup>
  <ItemGroup>
    <CSharpDefineConstants Include="MyStr=some string">
      <Type>String</Type>
    </CSharpDefineConstants>
    <CSharpDefineConstants Include="MyBoolTrue=TrUe">
      <Type>Bool</Type>
    </CSharpDefineConstants>
    <CSharpDefineConstants Include="MyBoolFalse=false">
      <Type>Bool</Type>
    </CSharpDefineConstants>
    <CSharpDefineConstants Include="MyBool0=0">
      <Type>Bool</Type>
    </CSharpDefineConstants>
    <CSharpDefineConstants Include="MyBool8=8">
      <Type>Bool</Type>
    </CSharpDefineConstants>
    <CSharpDefineConstants Include="MyInt14534543=14534543">
      <Type>Int</Type>
    </CSharpDefineConstants>
    <CSharpDefineConstants Include="MyInt_14534543=-14534543">
      <Type>Int</Type>
    </CSharpDefineConstants>
    <CSharpDefineConstants Include="MyStringArray=$(MyStringArray.Replace(';', ';'))">
      <Type>StringArray</Type>
    </CSharpDefineConstants>
  </ItemGroup>
~~~~~~
