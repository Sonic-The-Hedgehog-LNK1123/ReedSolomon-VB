# Universal Reed-Solomon Codec in VB.NET
A VB.NET implementation of the Reed-Solomon algorithm, supporting error, erasure and errata correction.

Uses code from the Reed-Solomon component of the [ZXing.Net project](https://github.com/micjahn/ZXing.Net/tree/master/Source/lib/common/reedsolomon) and code I've ported to C#, from the python code at [Wikiversity](https://en.wikiversity.org/wiki/Reed%E2%80%93Solomon_codes_for_coders), I then ported the C# code to VB.NET by using the code converter at [icsharpcode.net](https://codeconverter.icsharpcode.net/)

# NOTE

This code is provided for static linking into VB.NET programs only, for use in other languages, please use the [C# implementation](https://github.com/Sonic-The-Hedgehog-LNK1123/ReedSolomon), binary releases of the C# implementation are available [here](https://github.com/Sonic-The-Hedgehog-LNK1123/ReedSolomon/releases).

# Code examples

Create a representation of a Galois field:

```vbnet
Dim field As New GenericGF(285, 256, 0)
```

## Reed-Solomon encoding:

Create an instance of the `ReedSolomonEncoder` class, specifying the Galois field to use:

```vbnet
Dim rse As New ReedSolomonEncoder(field)
```

To encode the string `"Hello World"` with 9 ecc symbols, 9 null-values must be appended to store the ecc symbols:

```vbnet
Dim data As Integer() = New Integer() {&H48, &H65, &H6C, &H6C, &H6F, &H20, &H57, &H6F, &H72, &H6C, &H64, &H00, &H00, &H00, &H00, &H00, &H00, &H00, &H00, &H00}
```

Call the `Encode()` method of the `ReedSolomonEncoder` class to encode data with Reed-Solomon:

```vbnet
rse.Encode(data, 9)
```

The `data` variable now contains:

```
&H48, &H65, &H6C, &H6C, &H6F, &H20, &H57, &H6F, &H72, &H6C, &H64, &H40, &H86, &H08, &HD5, &H2C, &HAE, &HB5, &H8F, &H83
```

## Reed-Solomon decoding:

Previous `data` variable with some errors:

```vbnet
data = New Integer() {&H00, &H02, &H02, &H02, &H02, &H02, &H57, &H6F, &H72, &H6C, &H64, &H40, &H86, &H8, &HD5, &H2C, &HAE, &HB5, &H8F, &H83}
```

Providing the locations of some erasures:

```vbnet
Dim erasures As Integer() = New Integer() {0, 1, 2}
```

Create an instance of the `ReedSolomonDecoder` class, specifying the Galois field to use:

```vbnet
Dim rsd As New ReedSolomonDecoder(field)
```

Call the `Decode()` method of the `ReedSolomonDecoder` class to decode (correct) data with Reed-Solomon:

```vbnet
If rsd.Decode(data, 9, erasures) Then

    ' Data corrected.

Else

    ' Too many errors/erasures to correct.

End If
```

The `data` variable now contains:

```
&H48, &H65, &H6C, &H6C, &H6F, &H20, &H57, &H6F, &H72, &H6C, &H64, &H40, &H86, &H08, &HD5, &H2C, &HAE, &HB5, &H8F, &H83
```

# License

This project uses source files which are under Apache License 2.0, thus this repository is also under this license.
