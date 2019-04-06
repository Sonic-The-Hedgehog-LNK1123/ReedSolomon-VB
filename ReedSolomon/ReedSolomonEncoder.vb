'
' Copyright 2008 ZXing authors
'
' Licensed under the Apache License, Version 2.0 (the "License");
' you may not use this file except in compliance with the License.
' You may obtain a copy of the License at
'
'      http://www.apache.org/licenses/LICENSE-2.0
'
' Unless required by applicable law or agreed to in writing, software
' distributed under the License is distributed on an "AS IS" BASIS,
' WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
' See the License for the specific language governing permissions and
' limitations under the License.
'


' Code ported from C# to VB.NET by using the code converter at http://codeconverter.icsharpcode.net
' Conversion errors fixed by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)

Imports System
Imports System.Collections.Generic

' Namespace changed from "ZXing.Common.ReedSolomon" to "STH1123.ReedSolomon" by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
Namespace STH1123.ReedSolomon

    ''' <summary>
    ''' Implements Reed-Solomon encoding, as the name implies.
    ''' </summary>
    ''' <author>Sean Owen</author>
    ''' <author>William Rucklidge</author>
    Public NotInheritable Class ReedSolomonEncoder

        Private ReadOnly field As GenericGF
        Private ReadOnly cachedGenerators As IList(Of GenericGFPoly)

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ReedSolomonEncoder"/> class.
        ''' </summary>
        ''' <param name="field">A <see cref="GenericGF"/> that represents the Galois field to use</param>
        Public Sub New(ByVal field As GenericGF)
            Me.field = field
            Me.cachedGenerators = New List(Of GenericGFPoly)()
            cachedGenerators.Add(New GenericGFPoly(field, New Integer() {1}, True))
        End Sub

        Private Function buildGenerator(ByVal degree As Integer) As GenericGFPoly
            If degree >= cachedGenerators.Count Then
                Dim lastGenerator = cachedGenerators(cachedGenerators.Count - 1)

                For d As Integer = cachedGenerators.Count To degree
                    Dim nextGenerator = lastGenerator.multiply(New GenericGFPoly(field, New Integer() {1, field.exp(d - 1 + field.GeneratorBase)}, True))
                    cachedGenerators.Add(nextGenerator)
                    lastGenerator = nextGenerator
                Next
            End If

            Return cachedGenerators(degree)
        End Function

        ''' <summary>
        ''' Encodes given set of data codewords with Reed-Solomon.
        ''' </summary>
        ''' <param name="toEncode">data codewords and padding, the amount of padding should match
        ''' the number of error-correction codewords to generate. After encoding, the padding is
        ''' replaced with the error-correction codewords</param>
        ''' <param name="ecBytes">number of error-correction codewords to generate</param>
        Public Sub Encode(ByVal toEncode As Integer(), ByVal ecBytes As Integer)

            ' Method modified by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
            ' added check for messages that are too long for the used Galois field

            If toEncode.Length >= field.Size Then
                Throw New ArgumentException("Message is too long for this field", "toEncode")
            End If

            If ecBytes <= 0 Then
                Throw New ArgumentException("No error correction bytes provided", "ecBytes")
            End If

            Dim dataBytes = toEncode.Length - ecBytes

            If dataBytes <= 0 Then
                Throw New ArgumentException("No data bytes provided", "ecBytes")
            End If

            Dim generator = buildGenerator(ecBytes)
            Dim infoCoefficients = New Integer(dataBytes - 1) {}
            Array.Copy(toEncode, 0, infoCoefficients, 0, dataBytes)
            Dim info = New GenericGFPoly(field, infoCoefficients, True)
            info = info.multiplyByMonomial(ecBytes, 1)
            Dim remainder = info.divide(generator)(1)
            Dim coefficients = remainder.Coefficients
            Dim numZeroCoefficients = ecBytes - coefficients.Length

            For i = 0 To numZeroCoefficients - 1
                toEncode(dataBytes + i) = 0
            Next

            Array.Copy(coefficients, 0, toEncode, dataBytes + numZeroCoefficients, coefficients.Length)
        End Sub
    End Class
End Namespace
