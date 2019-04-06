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
Imports System.Text

' Namespace changed from "ZXing.Common.ReedSolomon" to "STH1123.ReedSolomon" by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
Namespace STH1123.ReedSolomon

    ''' <summary>
    ''' <p>Represents a polynomial whose coefficients are elements of a GF.
    ''' Instances of this class are immutable.</p>
    ''' <p>Much credit is due to William Rucklidge since portions of this code are an indirect
    ''' port of his C++ Reed-Solomon implementation.</p>
    ''' </summary>
    ''' <author>Sean Owen</author>
    Friend NotInheritable Class GenericGFPoly

        Private ReadOnly field As GenericGF
        Private ReadOnly m_coefficients As Integer()
        Private ReadOnly encoding As Boolean

        ''' <summary>
        ''' Initializes a new instance of the <see cref="GenericGFPoly"/> class.
        ''' </summary>
        ''' <param name="field">the <see cref="GenericGF"/> instance representing the field to use
        ''' to perform computations</param>
        ''' <param name="coefficients">coefficients as ints representing elements of GF(size), arranged
        ''' from most significant (highest-power term) coefficient to least significant</param>
        ''' <param name="encode">use true for encoding, false for decoding</param>
        ''' <exception cref="ArgumentException">if argument is null or empty,
        ''' or if leading coefficient is 0 and this is not a
        ''' constant polynomial (that is, it is not the monomial "0")</exception>
        Friend Sub New(ByVal field As GenericGF, ByVal coefficients As Integer(), ByVal encode As Boolean)

            ' Constructor modified by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
            ' do not drop zeros while performing decoding, because this rarely causes problems
            If encode Then

                If coefficients.Length = 0 Then
                    Throw New ArgumentException("No coefficients provided", "coefficients")
                End If

                Me.field = field
                Me.encoding = encode
                Dim coefficientsLength As Integer = coefficients.Length

                If coefficientsLength > 1 AndAlso coefficients(0) = 0 Then

                    ' Leading term must be non-zero for anything except the constant polynomial "0"
                    Dim firstNonZero As Integer = 1

                    While firstNonZero < coefficientsLength AndAlso coefficients(firstNonZero) = 0
                        firstNonZero += 1
                    End While

                    If firstNonZero = coefficientsLength Then
                        Me.m_coefficients = New Integer() {0}
                    Else
                        Me.m_coefficients = New Integer(coefficientsLength - firstNonZero - 1) {}
                        Array.Copy(coefficients, firstNonZero, Me.m_coefficients, 0, Me.m_coefficients.Length)
                    End If
                Else
                    Me.m_coefficients = coefficients
                End If
            Else

                If coefficients.Length = 0 Then
                    Throw New ArgumentException("No coefficients provided", "coefficients")
                End If

                Me.field = field
                Me.encoding = encode
                Me.m_coefficients = coefficients
            End If
        End Sub

        Friend ReadOnly Property Coefficients As Integer()
            Get
                Return m_coefficients
            End Get
        End Property

        ''' <summary>
        ''' degree of this polynomial
        ''' </summary>
        Friend ReadOnly Property Degree As Integer
            Get
                Return m_coefficients.Length - 1
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this <see cref="GenericGFPoly"/> is zero.
        ''' </summary>
        ''' <value>true iff this polynomial is the monomial "0"</value>
        Friend ReadOnly Property isZero As Boolean

            ' Property modified by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
            Get
                For i As Integer = 0 To m_coefficients.Length - 1
                    If m_coefficients(i) <> 0 Then
                        Return False
                    End If
                Next

                Return True
            End Get
        End Property

        ''' <summary>
        ''' Builds the monomial.
        ''' </summary>
        ''' <param name="degree">The degree.</param>
        ''' <param name="coefficient">The coefficient.</param>
        ''' <returns>the monomial representing coefficient * x^degree</returns>
        Friend Function buildMonomial(ByVal degree As Integer, ByVal coefficient As Integer) As GenericGFPoly

            ' Method added by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
            ' replaces method originally defined in GenericGF.vb
            If degree < 0 Then
                Throw New ArgumentException("Degree must be positive", "degree")
            End If

            If coefficient = 0 Then
                Return New GenericGFPoly(field, New Integer() {0}, encoding)
            End If

            Dim coefficients As Integer() = New Integer(degree) {}
            coefficients(0) = coefficient
            Return New GenericGFPoly(field, coefficients, encoding)
        End Function

        ''' <summary>
        ''' coefficient of x^degree term in this polynomial
        ''' </summary>
        ''' <param name="degree">The degree.</param>
        ''' <returns>coefficient of x^degree term in this polynomial</returns>
        Friend Function getCoefficient(ByVal degree As Integer) As Integer
            Return m_coefficients(m_coefficients.Length - 1 - degree)
        End Function

        ''' <summary>
        ''' evaluation of this polynomial at a given point
        ''' </summary>
        ''' <param name="a">A.</param>
        ''' <returns>evaluation of this polynomial at a given point</returns>
        Friend Function evaluateAt(ByVal a As Integer) As Integer
            Dim result As Integer = 0

            If a = 0 Then

                ' Just return the x^0 coefficient
                Return getCoefficient(0)
            End If

            Dim size As Integer = m_coefficients.Length

            If a = 1 Then

                ' Just the sum of the coefficients
                For Each coefficient In m_coefficients
                    result = GenericGF.addOrSubtract(result, coefficient)
                Next

                Return result
            End If

            result = m_coefficients(0)

            For i As Integer = 1 To size - 1
                result = GenericGF.addOrSubtract(field.multiply(a, result), m_coefficients(i))
            Next

            Return result
        End Function

        Friend Function addOrSubtract(ByVal other As GenericGFPoly) As GenericGFPoly
            If Not field.Equals(other.field) Then
                Throw New ArgumentException("GenericGFPolys do not have same GenericGF field")
            End If

            If isZero Then
                Return other
            End If

            If other.isZero Then
                Return Me
            End If

            Dim smallerCoefficients As Integer() = Me.m_coefficients
            Dim largerCoefficients As Integer() = other.m_coefficients

            If smallerCoefficients.Length > largerCoefficients.Length Then
                Dim temp As Integer() = smallerCoefficients
                smallerCoefficients = largerCoefficients
                largerCoefficients = temp
            End If

            Dim sumDiff As Integer() = New Integer(largerCoefficients.Length - 1) {}
            Dim lengthDiff As Integer = largerCoefficients.Length - smallerCoefficients.Length
            ' Copy high-order terms only found in higher-degree polynomial's coefficients
            Array.Copy(largerCoefficients, 0, sumDiff, 0, lengthDiff)

            For i As Integer = lengthDiff To largerCoefficients.Length - 1
                sumDiff(i) = GenericGF.addOrSubtract(smallerCoefficients(i - lengthDiff), largerCoefficients(i))
            Next

            Return New GenericGFPoly(field, sumDiff, encoding)
        End Function

        Friend Function multiply(ByVal other As GenericGFPoly) As GenericGFPoly
            If Not field.Equals(other.field) Then
                Throw New ArgumentException("GenericGFPolys do not have same GenericGF field")
            End If

            If isZero OrElse other.isZero Then
                Return New GenericGFPoly(field, New Integer() {0}, encoding)
            End If

            Dim aCoefficients As Integer() = Me.m_coefficients
            Dim aLength As Integer = aCoefficients.Length
            Dim bCoefficients As Integer() = other.m_coefficients
            Dim bLength As Integer = bCoefficients.Length
            Dim product As Integer() = New Integer(aLength + bLength - 2) {}

            For i As Integer = 0 To aLength - 1
                Dim aCoeff As Integer = aCoefficients(i)

                For j As Integer = 0 To bLength - 1
                    product(i + j) = GenericGF.addOrSubtract(product(i + j), field.multiply(aCoeff, bCoefficients(j)))
                Next
            Next

            Return New GenericGFPoly(field, product, encoding)
        End Function

        Friend Function multiply(ByVal scalar As Integer) As GenericGFPoly
            If scalar = 0 Then
                Return New GenericGFPoly(field, New Integer() {0}, encoding)
            End If

            If scalar = 1 Then
                Return Me
            End If

            Dim size As Integer = m_coefficients.Length
            Dim product As Integer() = New Integer(size - 1) {}

            For i As Integer = 0 To size - 1
                product(i) = field.multiply(m_coefficients(i), scalar)
            Next

            Return New GenericGFPoly(field, product, encoding)
        End Function

        Friend Function multiplyByMonomial(ByVal degree As Integer, ByVal coefficient As Integer) As GenericGFPoly
            If degree < 0 Then
                Throw New ArgumentException("Degree must be positive", "degree")
            End If

            If coefficient = 0 Then
                Return New GenericGFPoly(field, New Integer() {0}, encoding)
            End If

            Dim size As Integer = m_coefficients.Length
            Dim product As Integer() = New Integer(size + degree - 1) {}

            For i As Integer = 0 To size - 1
                product(i) = field.multiply(m_coefficients(i), coefficient)
            Next

            Return New GenericGFPoly(field, product, encoding)
        End Function

        Friend Function divide(ByVal other As GenericGFPoly) As GenericGFPoly()
            If Not field.Equals(other.field) Then
                Throw New ArgumentException("GenericGFPolys do not have same GenericGF field")
            End If

            If other.isZero Then
                Throw New ArgumentException("Divide by 0", "other")
            End If

            Dim quotient As New GenericGFPoly(field, New Integer() {0}, encoding)
            Dim remainder As GenericGFPoly = Me
            Dim denominatorLeadingTerm As Integer = other.getCoefficient(other.Degree)
            Dim inverseDenominatorLeadingTerm As Integer = field.inverse(denominatorLeadingTerm)

            While remainder.Degree >= other.Degree AndAlso Not remainder.isZero
                Dim degreeDifference As Integer = remainder.Degree - other.Degree
                Dim scale As Integer = field.multiply(remainder.getCoefficient(remainder.Degree), inverseDenominatorLeadingTerm)
                Dim term As GenericGFPoly = other.multiplyByMonomial(degreeDifference, scale)
                Dim iterationQuotient As GenericGFPoly = buildMonomial(degreeDifference, scale)
                quotient = quotient.addOrSubtract(iterationQuotient)
                remainder = remainder.addOrSubtract(term)
            End While

            Return New GenericGFPoly() {quotient, remainder}
        End Function

        Public Overrides Function ToString() As String
            Dim result As New StringBuilder(8 * Degree)

            For degreeIterator As Integer = Degree To 0 Step -1
                Dim coefficient As Integer = getCoefficient(degreeIterator)

                If coefficient <> 0 Then
                    If coefficient < 0 Then
                        result.Append(" - ")
                        coefficient = -coefficient
                    Else
                        If result.Length > 0 Then
                            result.Append(" + ")
                        End If
                    End If

                    If degreeIterator = 0 OrElse coefficient <> 1 Then
                        Dim alphaPower As Integer = field.log(coefficient)

                        If alphaPower = 0 Then
                            result.Append("1"c)
                        ElseIf alphaPower = 1 Then
                            result.Append("a"c)
                        Else
                            result.Append("a^")
                            result.Append(alphaPower)
                        End If
                    End If

                    If degreeIterator <> 0 Then
                        If degreeIterator = 1 Then
                            result.Append("x"c)
                        Else
                            result.Append("x^")
                            result.Append(degreeIterator)
                        End If
                    End If
                End If
            Next

            Return result.ToString()
        End Function
    End Class
End Namespace
