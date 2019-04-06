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
Imports System.Collections.Generic

' Namespace changed from "ZXing.Common.ReedSolomon" to "STH1123.ReedSolomon" by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
Namespace STH1123.ReedSolomon

    ''' <summary> <p>Implements Reed-Solomon decoding, as the name implies.</p>
    ''' 
    ''' <p>The algorithm will not be explained here, but the following references were helpful
    ''' in creating this implementation:</p>
    ''' 
    ''' <ul>
    ''' <li>Bruce Maggs.
    ''' <a href="http://www.cs.cmu.edu/afs/cs.cmu.edu/project/pscico-guyb/realworld/www/rs_decode.ps">
    ''' "Decoding Reed-Solomon Codes"</a> (see discussion of Forney's Formula)</li>
    ''' <li>J.I. Hall. <a href="www.mth.msu.edu/~jhall/classes/codenotes/GRS.pdf">
    ''' "Chapter 5. Generalized Reed-Solomon Codes"</a>
    ''' (see discussion of Euclidean algorithm)</li>
    ''' </ul>
    ''' 
    ''' <p>Much credit is due to William Rucklidge since portions of this code are an indirect
    ''' port of his C++ Reed-Solomon implementation.</p>
    ''' 
    ''' </summary>
    ''' <author>Sean Owen</author>
    ''' <author>William Rucklidge</author>
    ''' <author>sanfordsquires</author>
    Public NotInheritable Class ReedSolomonDecoder

        Private ReadOnly field As GenericGF

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ReedSolomonDecoder"/> class.
        ''' </summary>
        ''' <param name="field">A <see cref="GenericGF"/> that represents the Galois field to use</param>
        Public Sub New(ByVal field As GenericGF)
            Me.field = field
        End Sub

        ''' <summary>
        '''   <p>Decodes given set of received codewords, which include both data and error-correction
        ''' codewords. Really, this means it uses Reed-Solomon to detect and correct errors, in-place,
        ''' in the input.</p>
        ''' </summary>
        ''' <param name="received">data and error-correction codewords</param>
        ''' <param name="twoS">number of error-correction codewords available</param>
        ''' <returns>false: decoding fails</returns>
        Public Function Decode(ByVal received As Integer(), ByVal twoS As Integer) As Boolean

            ' Method added by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
            Return Decode(received, twoS, Nothing)
        End Function

        ''' <summary>
        '''   <p>Decodes given set of received codewords, which include both data and error-correction
        ''' codewords. Really, this means it uses Reed-Solomon to detect and correct errors, in-place,
        ''' in the input.</p>
        ''' </summary>
        ''' <param name="received">data and error-correction codewords</param>
        ''' <param name="twoS">number of error-correction codewords available</param>
        ''' <param name="erasurePos">array of zero-based erasure indices</param>
        ''' <returns>false: decoding fails</returns>
        Public Function Decode(ByVal received As Integer(), ByVal twoS As Integer, ByVal erasurePos As Integer()) As Boolean

            ' Method modified by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
            ' to add support for erasure and errata correction
            ' most code ported to C# from the python code at http://en.wikiversity.org/wiki/Reed–Solomon_codes_for_coders
            ' C# code ported to VB.NET by using the code converter at http://codeconverter.icsharpcode.net

            If received.Length >= field.Size Then
                Throw New ArgumentException("Message is too long for this field", "received")
            End If

            If twoS <= 0 Then
                Throw New ArgumentException("No error correction bytes provided", "twoS")
            End If

            Dim dataBytes = received.Length - twoS

            If dataBytes <= 0 Then
                Throw New ArgumentException("No data bytes provided", "twoS")
            End If

            Dim syndromeCoefficients = New Integer(twoS - 1) {}
            Dim noError = True

            If erasurePos Is Nothing Then
                erasurePos = New Integer() {}
            Else
                For i = 0 To erasurePos.Length - 1
                    received(erasurePos(i)) = 0
                Next
            End If

            If erasurePos.Length > twoS Then
                Return False
            End If

            Dim poly = New GenericGFPoly(field, received, False)

            For i = 0 To twoS - 1
                Dim eval = poly.evaluateAt(field.exp(i + field.GeneratorBase))
                syndromeCoefficients(syndromeCoefficients.Length - 1 - i) = eval

                If eval <> 0 Then
                    noError = False
                End If
            Next

            If noError Then
                Return True
            End If

            Dim syndrome = New GenericGFPoly(field, syndromeCoefficients, False)

            Dim forneySyndrome = calculateForneySyndromes(syndrome, erasurePos, received.Length)

            Dim sigma = runBerlekampMasseyAlgorithm(forneySyndrome, erasurePos.Length)

            If sigma Is Nothing Then
                Return False
            End If

            Dim errorLocations = findErrorLocations(sigma)

            If errorLocations Is Nothing Then
                Return False
            End If

            ' Prepare errors
            Dim errorPositions As Integer() = New Integer(errorLocations.Length - 1) {}

            For i As Integer = 0 To errorLocations.Length - 1
                errorPositions(i) = field.log(errorLocations(i))
            Next

            ' Prepare erasures
            Dim erasurePositions As Integer() = New Integer(erasurePos.Length - 1) {}

            For i As Integer = 0 To erasurePos.Length - 1
                erasurePositions(i) = received.Length - 1 - erasurePos(i)
            Next

            ' Combine errors and erasures
            Dim errataPositions As Integer() = New Integer(errorPositions.Length + erasurePositions.Length - 1) {}

            Array.Copy(errorPositions, 0, errataPositions, 0, errorPositions.Length)
            Array.Copy(erasurePositions, 0, errataPositions, errorPositions.Length, erasurePositions.Length)

            Dim errataLocator = findErrataLocator(errataPositions)
            Dim omega = findErrorEvaluator(syndrome, errataLocator)

            If omega Is Nothing Then
                Return False
            End If

            Dim errata As Integer() = New Integer(errataPositions.Length - 1) {}

            For i As Integer = 0 To errataPositions.Length - 1
                errata(i) = field.exp(errataPositions(i))
            Next

            Dim errorMagnitudes = findErrorMagnitudes(omega, errata)

            If errorMagnitudes Is Nothing Then
                Return False
            End If

            For i = 0 To errata.Length - 1
                Dim position = received.Length - 1 - field.log(errata(i))

                If position < 0 Then

                    ' Throw New ReedSolomonException("Bad error location")
                    Return False
                End If

                received(position) = GenericGF.addOrSubtract(received(position), errorMagnitudes(i))
            Next

            Dim checkPoly = New GenericGFPoly(field, received, False)
            Dim isError = False

            For i = 0 To twoS - 1
                Dim eval = checkPoly.evaluateAt(field.exp(i + field.GeneratorBase))

                If eval <> 0 Then
                    isError = True
                End If
            Next

            If isError Then
                Return False
            End If

            Return True
        End Function

        ' Method added by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
        Friend Function calculateForneySyndromes(ByVal syndromes As GenericGFPoly, ByVal positions As Integer(), ByVal messageLength As Integer) As GenericGFPoly
            Dim positionsReversed As Integer() = New Integer(positions.Length - 1) {}

            For i As Integer = 0 To positions.Length - 1
                positionsReversed(i) = messageLength - 1 - positions(i)
            Next

            Dim forneySyndromesLength As Integer = syndromes.Coefficients.Length
            Dim syndromeCoefficients As Integer() = New Integer(syndromes.Coefficients.Length - 1) {}
            Array.Copy(syndromes.Coefficients, 0, syndromeCoefficients, 0, syndromes.Coefficients.Length)
            Dim forneySyndromes As New GenericGFPoly(field, syndromeCoefficients, False)

            For i As Integer = 0 To positions.Length - 1
                Dim x As Integer = field.exp(positionsReversed(i))

                For j As Integer = 0 To forneySyndromes.Coefficients.Length - 2
                    forneySyndromes.Coefficients(forneySyndromesLength - j - 1) = GenericGF.addOrSubtract(field.multiply(forneySyndromes.getCoefficient(j), x), forneySyndromes.getCoefficient(j + 1))
                Next
            Next

            Return forneySyndromes
        End Function

        ' Method added by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
        ' this method replaces original method "runEuclideanAlgorithm"
        Friend Function runBerlekampMasseyAlgorithm(ByVal syndrome As GenericGFPoly, ByVal erasureCount As Integer) As GenericGFPoly
            Dim sigma As New GenericGFPoly(field, New Integer() {1}, False)
            Dim old As New GenericGFPoly(field, New Integer() {1}, False)

            For i As Integer = 0 To (syndrome.Coefficients.Length - erasureCount) - 1
                Dim delta As Integer = syndrome.getCoefficient(i)

                For j As Integer = 1 To sigma.Coefficients.Length - 1
                    delta = delta Xor field.multiply(sigma.getCoefficient(j), syndrome.getCoefficient(i - j))
                Next

                Dim oldList As List(Of Integer) = New List(Of Integer)(old.Coefficients)
                oldList.Add(0)
                old = New GenericGFPoly(field, oldList.ToArray(), False)

                If delta <> 0 Then
                    If old.Coefficients.Length > sigma.Coefficients.Length Then
                        Dim new_loc As GenericGFPoly = old.multiply(delta)
                        old = sigma.multiply(field.inverse(delta))
                        sigma = new_loc
                    End If

                    sigma = sigma.addOrSubtract(old.multiply(delta))
                End If
            Next

            Dim sigmaList As List(Of Integer) = New List(Of Integer)(sigma.Coefficients)

            While Convert.ToBoolean(sigmaList.Count) AndAlso sigmaList(0) = 0
                sigmaList.RemoveAt(0)
            End While

            sigma = New GenericGFPoly(field, sigmaList.ToArray(), False)
            Return sigma
        End Function

        ' Method added by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
        Private Function findErrataLocator(ByVal errorPositions As Integer()) As GenericGFPoly
            Dim errataLocator As New GenericGFPoly(field, New Integer() {1}, False)

            For Each i As Integer In errorPositions
                errataLocator = errataLocator.multiply(New GenericGFPoly(field, New Integer() {1}, False).addOrSubtract(New GenericGFPoly(field, New Integer() {field.exp(i), 0}, False)))
            Next

            Return errataLocator
        End Function

        ' Method added by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
        Private Function findErrorEvaluator(ByVal syndrome As GenericGFPoly, ByVal errataLocations As GenericGFPoly) As GenericGFPoly
            Dim product As Integer() = syndrome.multiply(errataLocations).Coefficients
            Dim target As Integer() = New Integer(errataLocations.Coefficients.Length - 2) {}
            Array.Copy(product, product.Length - errataLocations.Coefficients.Length + 1, target, 0, target.Length)

            If target.Length = 0 Then
                Return Nothing
            End If

            Dim omega As New GenericGFPoly(field, target, False)
            Return omega
        End Function

        Private Function findErrorLocations(ByVal errorLocator As GenericGFPoly) As Integer()

            ' This is a direct application of Chien's search
            Dim numErrors As Integer = errorLocator.Degree

            If numErrors = 1 Then

                ' shortcut
                Return New Integer() {errorLocator.getCoefficient(1)}
            End If

            Dim result As Integer() = New Integer(numErrors - 1) {}
            Dim e As Integer = 0
            Dim i As Integer = 1

            While i < field.Size AndAlso e < numErrors

                If errorLocator.evaluateAt(i) = 0 Then
                    result(e) = field.inverse(i)
                    e += 1
                End If

                i += 1
            End While

            If e <> numErrors Then

                ' Throw New ReedSolomonException("Error locator degree does not match number of roots")
                Return Nothing
            End If

            Return result
        End Function

        ' Method modified by Sonic-The-Hedgehog-LNK1123 (github.com/Sonic-The-Hedgehog-LNK1123)
        ' added missing "If denominator = 0 Then" check
        Private Function findErrorMagnitudes(ByVal errorEvaluator As GenericGFPoly, ByVal errorLocations As Integer()) As Integer()

            ' This is directly applying Forney's Formula
            Dim s As Integer = errorLocations.Length
            Dim result As Integer() = New Integer(s - 1) {}

            For i As Integer = 0 To s - 1
                Dim xiInverse As Integer = field.inverse(errorLocations(i))
                Dim denominator As Integer = 1

                For j As Integer = 0 To s - 1
                    If i <> j Then
                        denominator = field.multiply(denominator, GenericGF.addOrSubtract(1, field.multiply(errorLocations(j), xiInverse)))
                    End If
                Next

                If denominator = 0 Then
                    Return Nothing
                End If

                result(i) = field.multiply(errorEvaluator.evaluateAt(xiInverse), field.inverse(denominator))

                If field.GeneratorBase <> 0 Then
                    result(i) = field.multiply(result(i), xiInverse)
                End If
            Next

            Return result
        End Function
    End Class
End Namespace
