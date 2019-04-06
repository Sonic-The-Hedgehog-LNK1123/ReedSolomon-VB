'
' Copyright 2007 ZXing authors
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

' Namespace changed from "ZXing.Common.ReedSolomon" to "STH1123.ReedSolomon" by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
Namespace STH1123.ReedSolomon

    ''' <summary>
    '''   <p>This class contains utility methods for performing mathematical operations over
    ''' the Galois Fields. Operations use a given primitive polynomial in calculations.</p>
    '''   <p>Throughout this package, elements of the GF are represented as an <see cref="Int32"/>
    ''' for convenience and speed (but at the cost of memory).
    '''   </p>
    ''' </summary>
    ''' <author>Sean Owen</author>
    Public NotInheritable Class GenericGF

        ''' <summary>
        ''' x^12 + x^6 + x^5 + x^3 + 1
        ''' </summary>
        Public Shared AZTEC_DATA_12 As New GenericGF(&H1069, 4096, 1, 2) ' x^12 + x^6 + x^5 + x^3 + 1

        ''' <summary>
        ''' x^10 + x^3 + 1
        ''' </summary>
        Public Shared AZTEC_DATA_10 As New GenericGF(&H409, 1024, 1, 2) ' x^10 + x^3 + 1

        ''' <summary>
        ''' x^6 + x + 1
        ''' </summary>
        Public Shared AZTEC_DATA_6 As New GenericGF(&H43, 64, 1, 2) ' x^6 + x + 1

        ''' <summary>
        ''' x^4 + x + 1
        ''' </summary>
        Public Shared AZTEC_PARAM As New GenericGF(&H13, 16, 1, 2) ' x^4 + x + 1

        ''' <summary>
        ''' x^8 + x^4 + x^3 + x^2 + 1
        ''' </summary>
        Public Shared QR_CODE_FIELD_256 As New GenericGF(&H11D, 256, 0, 2) ' x^8 + x^4 + x^3 + x^2 + 1

        ''' <summary>
        ''' x^8 + x^5 + x^3 + x^2 + 1
        ''' </summary>
        Public Shared DATA_MATRIX_FIELD_256 As New GenericGF(&H12D, 256, 1, 2) ' x^8 + x^5 + x^3 + x^2 + 1

        ''' <summary>
        ''' x^8 + x^5 + x^3 + x^2 + 1
        ''' </summary>
        Public Shared AZTEC_DATA_8 As GenericGF = DATA_MATRIX_FIELD_256

        ''' <summary>
        ''' x^6 + x + 1
        ''' </summary>
        Public Shared MAXICODE_FIELD_64 As GenericGF = AZTEC_DATA_6

        Private expTable As Integer()
        Private logTable As Integer()
        Private ReadOnly m_size As Integer
        Private ReadOnly m_primitive As Integer
        Private ReadOnly m_generatorBase As Integer
        Private ReadOnly m_alpha As Integer

        ''' <summary>
        ''' Create a representation of GF(size) using the given primitive polynomial.
        ''' </summary>
        ''' <param name="primitive">irreducible polynomial whose coefficients are represented by
        ''' the bits of an <see cref="Int32"/>, where the least-significant bit represents the constant
        ''' coefficient</param>
        ''' <param name="size">the size of the field</param>
        ''' <param name="genBase">the factor b in the generator polynomial can be 0- or 1-based
        ''' (g(x) = (x+a^b)(x+a^(b+1))...(x+a^(b+2t-1))).
        ''' In most cases it should be 1, but for QR code it is 0.</param>
        Public Sub New(ByVal primitive As Integer, ByVal size As Integer, ByVal genBase As Integer)
            Me.New(primitive, size, genBase, 2)

            ' Constructor added by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
            ' calls overloaded constructor only
        End Sub

        ''' <summary>
        ''' Create a representation of GF(size) using the given primitive polynomial.
        ''' </summary>
        ''' <param name="primitive">irreducible polynomial whose coefficients are represented by
        ''' the bits of an <see cref="Int32"/>, where the least-significant bit represents the constant
        ''' coefficient</param>
        ''' <param name="size">the size of the field</param>
        ''' <param name="genBase">the factor b in the generator polynomial can be 0- or 1-based
        ''' (g(x) = (x+a^b)(x+a^(b+1))...(x+a^(b+2t-1))).
        ''' In most cases it should be 1, but for QR code it is 0.</param>
        ''' <param name="alpha">the generator alpha</param>
        Public Sub New(ByVal primitive As Integer, ByVal size As Integer, ByVal genBase As Integer, ByVal alpha As Integer)

            ' Constructor modified by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
            ' to add support for alpha powers other than 2
            Me.m_primitive = primitive
            Me.m_size = size
            Me.m_generatorBase = genBase
            Me.m_alpha = alpha

            expTable = New Integer(size - 1) {}
            logTable = New Integer(size - 1) {}
            Dim x As Integer = 1

            If alpha = 2 Then
                For i As Integer = 0 To size - 1
                    expTable(i) = x
                    x <<= 1 ' x = x * 2  the generator alpha is 2

                    If x >= size Then
                        x = x Xor primitive
                        x = x And size - 1
                    End If
                Next
            Else

                For i As Integer = 0 To size - 1
                    expTable(i) = x
                    x = multiplyNoLUT(x, alpha, primitive, size)
                Next
            End If

            For i As Integer = 0 To size - 2
                logTable(expTable(i)) = i
            Next
            ' logTable(0) = 0 but this should never be used
        End Sub

        ' Method added by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
        Friend Shared Function multiplyNoLUT(ByVal x As Integer, ByVal y As Integer, ByVal primitive As Integer, ByVal size As Integer) As Integer
            Dim r As Integer = 0

            While y > 0
                If Convert.ToBoolean(y And 1) Then
                    r = r Xor x
                End If

                y >>= 1
                x <<= 1

                If x >= size Then
                    x = x Xor primitive
                    x = x And size - 1
                End If
            End While

            Return r
        End Function

        ''' <summary>
        ''' Implements both addition and subtraction -- they are the same in GF(size).
        ''' </summary>
        ''' <returns>sum/difference of a and b</returns>
        Friend Shared Function addOrSubtract(ByVal a As Integer, ByVal b As Integer) As Integer
            Return a Xor b
        End Function

        ''' <summary>
        ''' Exps the specified a.
        ''' </summary>
        ''' <returns>alpha to the power of a in GF(size)</returns>
        Friend Function exp(ByVal a As Integer) As Integer
            Return expTable(a)
        End Function

        ''' <summary>
        ''' Logs the specified a.
        ''' </summary>
        ''' <param name="a">A.</param>
        ''' <returns>base alpha log of a in GF(size)</returns>
        Friend Function log(ByVal a As Integer) As Integer
            If a = 0 Then
                Throw New ArithmeticException("log(0) is undefined")
            End If

            Return logTable(a)
        End Function

        ''' <summary>
        ''' Inverses the specified a.
        ''' </summary>
        ''' <returns>multiplicative inverse of a</returns>
        Friend Function inverse(ByVal a As Integer) As Integer
            If a = 0 Then
                Throw New ArithmeticException("inverse(0) is undefined")
            End If

            Return expTable(Size - logTable(a) - 1)
        End Function

        ''' <summary>
        ''' Multiplies the specified a with b.
        ''' </summary>
        ''' <param name="a">A.</param>
        ''' <param name="b">The b.</param>
        ''' <returns>product of a and b in GF(size)</returns>
        Friend Function multiply(ByVal a As Integer, ByVal b As Integer) As Integer
            If a = 0 OrElse b = 0 Then
                Return 0
            End If

            Return expTable((logTable(a) + logTable(b)) Mod (Size - 1))
        End Function

        ''' <summary>
        ''' Gets the primitive polynomial as an <see cref="Int32"/>.
        ''' </summary>
        Public ReadOnly Property Primitive As Integer

            ' Property added by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
            Get
                Return m_primitive
            End Get
        End Property

        ''' <summary>
        ''' Gets the size.
        ''' </summary>
        Public ReadOnly Property Size As Integer
            Get
                Return m_size
            End Get
        End Property

        ''' <summary>
        ''' Gets the generator base.
        ''' </summary>
        Public ReadOnly Property GeneratorBase As Integer
            Get
                Return m_generatorBase
            End Get
        End Property

        ''' <summary>
        ''' Gets the generator alpha.
        ''' </summary>
        Public ReadOnly Property Alpha As Integer

            ' Property added by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
            Get
                Return m_alpha
            End Get
        End Property

        ''' <summary>
        ''' Returns a <see cref="System.String"/> that represents this instance.
        ''' </summary>
        ''' <returns>
        ''' A <see cref="System.String"/> that represents this instance.
        ''' </returns>
        Public Overrides Function ToString() As String
            Return "GF(0x" & m_primitive.ToString("X") & ","c & m_size & ","c & m_alpha & ")"c
        End Function
    End Class
End Namespace
